using System;
using System.Collections.Generic;
using UnityEngine;

public class ComputeLighting : MonoBehaviour
{
    struct Triangle
    {
        public uint v0;
        public uint v1;
        public uint v2;
        public uint colorIndex;
    }

    struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;
        public uint endIndex;
    }

    public BVH Bvh;
    public ComputeShader computeShader;
    RenderTexture renderTexture;

    public int screenWidth;
    public int screenHeight;
    public int nSamples;
    public int maxDepth;

    Vector3[] vertices;
    Triangle[] triangles;
    BoundingBox[] bounds;

    public Vector3[] colors;

    Camera _camera;
    ComputeBuffer triangleBuffer, vertexBuffer, boundsBuffer, colorBuffer;

    [HideInInspector]
    public bool meshGenerated;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        meshGenerated = false;
    }

    void PrepareData()
    {
        bounds = new BoundingBox[(int)Math.Pow(2, Bvh.maxDepth)];

        BVHNode root = Bvh.CreateAndInit();

        vertices = Bvh.transformedVertArray;
        triangles = new Triangle[Bvh.triArray.Length / 3];

        PopulateBounds(root, 1, bounds, triangles);
    }

    void PopulateBounds(BVHNode node, int index, BoundingBox[] bounds, Triangle[] tris)
    {
        bounds[index] = new()
        {
            Max = node.Max,
            Min = node.Min,
        };

        if (node.ChildA != null && node.ChildB != null)
        {
            PopulateBounds(node.ChildA, 2 * index, bounds, tris);
            PopulateBounds(node.ChildB, 2 * index + 1, bounds, tris);
        } else
        {
            uint startIndex = (index > bounds.Length / 2) ? bounds[index - 1].endIndex : 0;

            bounds[index].endIndex = startIndex + (uint) node.triangles.Count / 3;

            for (int i = 0; i < node.triangles.Count; i+=3)
            {
                tris[startIndex + i / 3] = new()
                {
                    v0 = (uint)node.triangles[i],
                    v1 = (uint)node.triangles[i + 1],
                    v2 = (uint)node.triangles[i + 2],
                    colorIndex = (uint) (vertices[(uint)node.triangles[i]].y < 1.0f ? 0 : 1),
                };
            }
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
            PrepareData();
            
            Debug.Log("Number of vertices = " + vertices.Length);
            Debug.Log("Number of triangles = " + triangles.Length);

            renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            
            computeShader.SetTexture(0, "Result", renderTexture);
            computeShader.SetMatrix("CameraInverseProj", _camera.projectionMatrix.inverse);
            computeShader.SetInt("NSamples", nSamples);
            computeShader.SetInt("MaxDepth", maxDepth);

            triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(uint) * 4);
            triangleBuffer.SetData(triangles);
            computeShader.SetBuffer(0, "Triangles", triangleBuffer);

            vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            vertexBuffer.SetData(vertices);
            computeShader.SetBuffer(0, "Vertices", vertexBuffer);

            boundsBuffer = new ComputeBuffer(bounds.Length, sizeof(float) * 6 + sizeof(uint));
            boundsBuffer.SetData(bounds);
            computeShader.SetBuffer(0, "Bounds", boundsBuffer);

            colorBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 3);
            colorBuffer.SetData(colors);
            computeShader.SetBuffer(0, "Colors", colorBuffer);
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

        if (boundsBuffer != null)
        {
            boundsBuffer.Release();
        }

        if (colorBuffer != null)
        {
            colorBuffer.Release();
        }
    }

    private void OnDrawGizmos()
    {
        //if (triangles != null)
        //{
        //    for (int i = 0; i < triangles.Length; i++)
        //    {
        //        Gizmos.DrawLine(vertices[triangles[i].v0], vertices[triangles[i].v1]);
        //        Gizmos.DrawLine(vertices[triangles[i].v1], vertices[triangles[i].v2]);
        //        Gizmos.DrawLine(vertices[triangles[i].v2], vertices[triangles[i].v0]);
        //    }
        //}
        if (bounds == null) return;

        foreach (BoundingBox b in bounds)
        {
            Vector3 centre = (b.Max + b.Min) * 0.5f;
            Gizmos.DrawWireCube(centre, b.Max - b.Min);
        }
    }
}
