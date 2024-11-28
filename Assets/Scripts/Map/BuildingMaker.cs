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

            int vertIndex = verts.Count;
            for (int i = 0; i < way.NodeIDs.Count - 1; i++)
            {
                current = verts[i];
                next = verts[(i + 1) % (way.NodeIDs.Count - 1)];

                verts.Add(new(current.x, current.y, current.z));
                verts.Add(new(next.x, next.y, next.z));
                verts.Add(new(current.x, 0, current.z));
                verts.Add(new(next.x, 0, next.z));

                tris.Add(vertIndex);
                tris.Add(vertIndex + 1);
                tris.Add(vertIndex + 2);

                tris.Add(vertIndex + 1);
                tris.Add(vertIndex + 3);
                tris.Add(vertIndex + 2);

                vertIndex += 4;
            }

            mf.mesh.vertices = verts.ToArray();
            mf.mesh.triangles = tris.ToArray();
            mf.mesh.RecalculateNormals();

            yield return null;
        }
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
