using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFTest))]
public class SDFTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Build Lighting"))
        {
            SDFTest t = (SDFTest)target;
            t.BuildSDFMap();
        }
    }
}
