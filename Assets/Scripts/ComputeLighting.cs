using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeLighting : MonoBehaviour
{
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public uint colorIndex;
    }

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public uint colorIndex;
    }

    public ComputeShader computeShader;
    RenderTexture renderTexture;

    public Mesh defaultSphereMesh;

    public int screenWidth;
    public int screenHeight;
    public int nSamples;
    public int maxDepth;

    public string materialsPath;
    Dictionary<string, uint> colorIdDict = new Dictionary<string, uint>();

    List<Sphere> spheres = new();
    List<Vector3> colors = new();
    List<Triangle> triangles = new();

    Camera _camera;
    ComputeBuffer sphereBuffer, colorBuffer, triangleBuffer;

    [HideInInspector]
    public bool meshGenerated;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        meshGenerated = false;

        // Load all materials from the specified folder in the Resources directory
        Material[] materials = Resources.LoadAll<Material>(materialsPath);

        uint colorIndex = 0;
        foreach (Material mat in materials)
        {
            Debug.Log($"Loaded Material: {mat.name}", mat);
            colorIdDict[mat.name] = colorIndex;
            colorIndex++;
            
            Vector3 color = new Vector3(mat.color.r, mat.color.g, mat.color.b);
            colors.Add(color);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!meshGenerated)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        if (renderTexture == null)
        {
            //Iterate through all GameObjects in the scene
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    if (meshFilter.sharedMesh == defaultSphereMesh)
                    {
                        Sphere sphere = new()
                        {
                            position = obj.transform.position,
                            radius = obj.transform.localScale.x / 2.0f,
                            colorIndex = colorIdDict[obj.GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "")]
                        };
                        spheres.Add(sphere);
                    } else
                    {
                        int[] tris = meshFilter.mesh.triangles;
                        Vector3[] verts = meshFilter.mesh.vertices;
                        Transform objTransform = obj.transform;
                        
                        for (int i = 0; i < meshFilter.mesh.triangles.Length; i += 3)
                        {
                            Vector3 v0 = objTransform.TransformPoint(verts[tris[i]]);
                            Vector3 v1 = objTransform.TransformPoint(verts[tris[i + 1]]);
                            Vector3 v2 = objTransform.TransformPoint(verts[tris[i + 2]]);

                            Triangle triangle = new()
                            {
                                v0 = v0, v1 = v1, v2 = v2,
                                colorIndex = colorIdDict[obj.GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "")]
                            };
                            triangles.Add(triangle);
                        }
                    }
                }
            }

            Debug.Log(triangles.ToArray().Length);

            renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            
            computeShader.SetTexture(0, "Result", renderTexture);
            computeShader.SetMatrix("CameraInverseProj", _camera.projectionMatrix.inverse);
            computeShader.SetInt("NSamples", nSamples);
            computeShader.SetInt("MaxDepth", maxDepth);

            sphereBuffer = new ComputeBuffer(spheres.ToArray().Length, sizeof(float) * 4 + sizeof(uint));
            sphereBuffer.SetData(spheres.ToArray());
            computeShader.SetBuffer(0, "Spheres", sphereBuffer);

            colorBuffer = new ComputeBuffer(colors.ToArray().Length, sizeof(float) * 3);
            colorBuffer.SetData(colors.ToArray());
            computeShader.SetBuffer(0, "Colors", colorBuffer);

            triangleBuffer = new ComputeBuffer(triangles.ToArray().Length, sizeof(float) * 9 + sizeof(uint));
            triangleBuffer.SetData(triangles.ToArray());
            computeShader.SetBuffer(0, "Triangles", triangleBuffer);
        }
        
        computeShader.SetMatrix("CameraToWorld", _camera.cameraToWorldMatrix);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        Graphics.Blit(renderTexture, destination);
    }

    void OnDestroy()
    {
        // Release the buffer when done (important to avoid memory leaks)
        if (sphereBuffer != null)
        {
            sphereBuffer.Release();
        }

        if (colorBuffer != null)
        {
            colorBuffer.Release();
        }

        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
        }
    }
}
