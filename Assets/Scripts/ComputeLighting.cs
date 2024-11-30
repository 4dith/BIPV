using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeLighting : MonoBehaviour
{
    struct Triangle
    {
        public uint v0;
        public uint v1;
        public uint v2;
    }

    public ComputeShader computeShader;
    RenderTexture renderTexture;

    public int screenWidth;
    public int screenHeight;
    public int nSamples;
    public int maxDepth;

    List<Triangle> triangles = new();
    List<Vector3> vertices = new();

    Camera _camera;
    ComputeBuffer triangleBuffer, vertexBuffer;

    [HideInInspector]
    public bool meshGenerated;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        meshGenerated = false;
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
            int vertIndex = 0;

            foreach (GameObject obj in allObjects)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    int[] tris = meshFilter.mesh.triangles;
                    Vector3[] verts = meshFilter.mesh.vertices;
                    Transform objTransform = obj.transform;

                    for (int i = 0; i < verts.Length; i++)
                    {
                        vertices.Add(objTransform.TransformPoint(verts[i]));
                    }

                    for (int i = 0; i < meshFilter.mesh.triangles.Length; i += 3)
                    {
                        Triangle triangle = new()
                        {
                            v0 = (uint) (vertIndex + tris[i]), 
                            v1 = (uint) (vertIndex + tris[i + 1]), 
                            v2 = (uint) (vertIndex + tris[i + 2]),
                        };
                        triangles.Add(triangle);
                    }

                    vertIndex += verts.Length;
                }
            }

            Vector3[] vertArray = vertices.ToArray();
            Triangle[] triArray = triangles.ToArray();

            Debug.Log("Number of vertices = " + vertArray.Length);
            Debug.Log("Number of triangles = " + triArray.Length);

            renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            
            computeShader.SetTexture(0, "Result", renderTexture);
            computeShader.SetMatrix("CameraInverseProj", _camera.projectionMatrix.inverse);
            computeShader.SetInt("NSamples", nSamples);
            computeShader.SetInt("MaxDepth", maxDepth);

            triangleBuffer = new ComputeBuffer(triArray.Length, sizeof(uint) * 3);
            triangleBuffer.SetData(triArray);
            computeShader.SetBuffer(0, "Triangles", triangleBuffer);

            vertexBuffer = new ComputeBuffer(vertArray.Length, sizeof(float) * 3);
            vertexBuffer.SetData(vertArray);
            computeShader.SetBuffer(0, "Vertices", vertexBuffer);
        }
        
        computeShader.SetMatrix("CameraToWorld", _camera.cameraToWorldMatrix);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        Graphics.Blit(renderTexture, destination);
    }

    void OnDestroy()
    {
        // Release the buffer when done (important to avoid memory leaks)
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
        }

        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
        }
    }
}
