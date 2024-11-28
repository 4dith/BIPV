using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeLighting : MonoBehaviour
{
    [System.Serializable]
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public uint materialIndex;
    }

    [System.Serializable]
    struct MatStruct
    {
        public Vector3 color;
        public float metallic;
        public float smoothness;
    }

    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    public Mesh defaultSphereMesh;

    public int screenWidth;
    public int screenHeight;
    public int nSamples;
    public int maxDepth;

    public string materialsPath;
    Dictionary<string, uint> matIdDict = new Dictionary<string, uint>();

    List<Sphere> spheres = new();
    List<MatStruct> matStructs = new();

    Camera _camera;
    ComputeBuffer sphereBuffer, materialBuffer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        // Load all materials from the specified folder in the Resources directory
        Material[] materials = Resources.LoadAll<Material>(materialsPath);

        uint matIndex = 0;
        foreach (Material mat in materials)
        {
            Debug.Log($"Loaded Material: {mat.name}", mat);
            matIdDict[mat.name] = matIndex;
            matIndex++;
            
            MatStruct matStruct = new MatStruct();
            matStruct.color = new Vector3(mat.color.r, mat.color.g, mat.color.b);
            matStruct.metallic = mat.GetFloat("_Metallic");
            matStruct.smoothness = mat.GetFloat("_Smoothness");
            matStructs.Add(matStruct);
        }

        //Iterate through all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh == defaultSphereMesh)
            {
                Debug.Log($"Default sphere found: {obj.name} {obj.transform.position} {obj.transform.localScale}");
                Sphere sphere = new()
                {
                    position = obj.transform.position,
                    radius = obj.transform.localScale.x / 2.0f,
                    materialIndex = matIdDict[obj.GetComponent<MeshRenderer>().material.name.Replace(" (Instance)", "")]
                };
                spheres.Add(sphere);
            }
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (renderTexture == null)
        {
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

            materialBuffer = new ComputeBuffer(matStructs.ToArray().Length, sizeof(float) * 5);
            materialBuffer.SetData(matStructs.ToArray());
            computeShader.SetBuffer(0, "Materials", materialBuffer);
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

        if (materialBuffer != null)
        {
            materialBuffer.Release();
        }
    }
}
