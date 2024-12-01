using System.Collections.Generic;
using UnityEngine;

public class BVHNode
{
    public Vector3 Min = Vector3.one * float.PositiveInfinity;
    public Vector3 Max = Vector3.one * float.NegativeInfinity;
    public Vector3 Centre => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
    
    public BVHNode ChildA;
    public BVHNode ChildB;
    public List<int> triangles = new();

    void FitVertex(Vector3 vert)
    {
        Min = Vector3.Min(Min, vert);
        Max = Vector3.Max(Max, vert);
    }

    public void FitTriangle(Vector3 vert1, Vector3 vert2, Vector3 vert3, int index1, int index2, int index3)
    {
        FitVertex(vert1);
        FitVertex(vert2);
        FitVertex(vert3);

        triangles.Add(index1);
        triangles.Add(index2);
        triangles.Add(index3);
    }
}