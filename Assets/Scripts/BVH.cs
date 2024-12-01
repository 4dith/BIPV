using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class BVH : MonoBehaviour
{
    public BVHNode root;

    public Transform lightRay;

    public int maxDepth = 1;

    [HideInInspector]
    public Vector3[] transformedVertices;

    [HideInInspector]
    public int[] tris;
    
    public BVHNode CreateAndInit()
    {
        BVHNode node = new();

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        transformedVertices = new Vector3[mesh.vertices.Length];
        transform.TransformPoints(mesh.vertices, transformedVertices);
        
        tris = mesh.triangles;

        for (int i = 0; i < mesh.triangles.Length; i+=3)
        {
            Vector3 vert1 = transformedVertices[tris[i]];
            Vector3 vert2 = transformedVertices[tris[i + 1]];
            Vector3 vert3 = transformedVertices[tris[i + 2]];

            node.FitTriangle(vert1, vert2, vert3, tris[i], tris[i + 1], tris[i + 2]);
        }

        Split(transformedVertices, node, 1);
        return node;
    }

    void Split(Vector3[] verts, BVHNode node, int depth)
    {
        if (depth >= maxDepth) return;

        List<int> tris = node.triangles;

        node.ChildA = new();
        node.ChildB = new();

        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 v1 = verts[tris[i]];
            Vector3 v2 = verts[tris[i + 1]];
            Vector3 v3 = verts[tris[i + 2]];
            
            Vector3 size = node.Size;
            int splitAxis = size.x > Math.Max(size.y, size.z) ? 0: size.y > size.z ? 1 : 2;
            
            float triCentre = (v1[splitAxis] + v2[splitAxis] + v3[splitAxis]) / 3f;
            if (triCentre <= node.Centre[splitAxis])
            {
                node.ChildA.FitTriangle(v1, v2, v3, tris[i], tris[i + 1], tris[i + 2]);
            } else
            {
                node.ChildB.FitTriangle(v1, v2, v3, tris[i], tris[i + 1], tris[i + 2]);
            }
        }

        Split(verts, node.ChildA, depth + 1);
        Split(verts, node.ChildB, depth + 1);
    }

    bool DrawBVH(BVHNode node)
    {
        float t;
        bool result = RayIntersectsAABB(lightRay.position, lightRay.forward, node.Min, node.Max, out t);

        if (result)
        {
            if (node.ChildA != null)
            {
                result = DrawBVH(node.ChildA);
                if (!result) result = DrawBVH(node.ChildB);
                Gizmos.color = result ? Color.yellow : Color.red;
            }
            else
            {
                Gizmos.color = Color.white; // Leaf node which intersects with ray
            }
        } else Gizmos.color= Color.red; // Node does not intersect with ray
        
        Gizmos.DrawWireCube(node.Centre, node.Size);
        return result;
    }

    void OnDrawGizmos()
    {
        if (root != null) DrawBVH(root);
    }

    bool RayIntersectsAABB(Vector3 origin, Vector3 direction, Vector3 boxMin, Vector3 boxMax, out float t)
    {
        t = 0;
        float tMin = (boxMin.x - origin.x) / direction.x;
        float tMax = (boxMax.x - origin.x) / direction.x;

        if (tMin > tMax) Swap(ref tMin, ref tMax);

        float tyMin = (boxMin.y - origin.y) / direction.y;
        float tyMax = (boxMax.y - origin.y) / direction.y;

        if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

        if ((tMin > tyMax) || (tyMin > tMax))
            return false;

        if (tyMin > tMin)
            tMin = tyMin;
        if (tyMax < tMax)
            tMax = tyMax;

        float tzMin = (boxMin.z - origin.z) / direction.z;
        float tzMax = (boxMax.z - origin.z) / direction.z;

        if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

        if ((tMin > tzMax) || (tzMin > tMax))
            return false;

        if (tzMin > tMin)
            tMin = tzMin;
        if (tzMax < tMax)
            tMax = tzMax;

        if (tMax < 0)
            return false;

        t = tMin > 0 ? tMin : tMax;
        return true;
    }

    void Swap(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }
}
