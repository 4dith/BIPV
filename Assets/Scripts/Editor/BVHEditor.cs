using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BVH))]
public class BVHEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BVH bvh = (BVH)target;

        if (GUILayout.Button("Calculate Bounds"))
        {
            bvh.root = bvh.CreateAndInit();
        }
    }
}
