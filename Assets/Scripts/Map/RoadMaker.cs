using System.Collections;
using UnityEngine;


// Todo: Make a Base Class for "Makers"
[RequireComponent(typeof(MapReader))]
class RoadMaker : MonoBehaviour
{
    MapReader map;

    IEnumerator Start()
    {
        map = GetComponent<MapReader>();
        while (!map.IsReady)
        {
            yield return null;
        }
    }
}