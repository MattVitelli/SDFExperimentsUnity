using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFBuilder))]
public class SDFBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Build SDF"))
        {
            SDFBuilder t = (SDFBuilder)target;
            t.BuildSDF();
        }
    }
}
