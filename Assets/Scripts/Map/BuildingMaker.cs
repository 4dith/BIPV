using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MapReader))]
class BuildingMaker : MonoBehaviour
{
    public Material buildingMat;
    MapReader map;

    IEnumerator Start()
    {
        map = GetComponent<MapReader>();
        while (!map.IsReady)
        {
            yield return null;
        }

        foreach (var way in map.ways.FindAll((w) => { return w.IsBuilding && w.NodeIDs.Count > 3; }))
        {
            GameObject go = new GameObject();
            Vector3 localOrigin = GetCentre(way);
            go.transform.position = localOrigin - map.bounds.Centre;
            go.transform.parent = transform;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = buildingMat;

            List<Vector3> verts = new();

            float signedArea = 0f;
            Vector3 current = map.nodes[way.NodeIDs[0]] - localOrigin, next;
            for (int i = 0; i < way.NodeIDs.Count - 1; i++)
            {
                next = map.nodes[way.NodeIDs[i + 1]] - localOrigin;
                signedArea += current.x * next.z - current.z * next.x;

                current.y = way.Height;
                verts.Add(current);
                current = next;
            }
            if (signedArea < 0f) verts.Reverse();
            List<int> tris = EarClippingTriangulation.Triangulate(verts);

            // Adding bottom vertices
            int nTopVertices = verts.Count;
            for (int i = 0; i < nTopVertices; i++)
            {
                Vector3 bottomVert = verts[i] + Vector3.down * verts[i].y;
                verts.Add(bottomVert);
            }

            for (int i = 0; i < way.NodeIDs.Count - 1; i++)
            {
                tris.Add(i);
                tris.Add((i + 1) % (way.NodeIDs.Count - 1));
                tris.Add(nTopVertices + i);

                tris.Add((i + 1) % (way.NodeIDs.Count - 1));
                tris.Add(nTopVertices + (i + 1) % (way.NodeIDs.Count - 1));
                tris.Add(nTopVertices + i);
            }

            mf.mesh.vertices = verts.ToArray();
            mf.mesh.triangles = tris.ToArray();
            mf.mesh.RecalculateNormals();

            yield return null;
        }

        FindObjectOfType<ComputeLighting>().meshGenerated = true;
    }

    Vector3 GetCentre(OsmWay way)
    {
        Vector3 total = Vector3.zero;
        foreach (var id in way.NodeIDs)
        {
            total += map.nodes[id];
        }

        return total / way.NodeIDs.Count;
    }
}
