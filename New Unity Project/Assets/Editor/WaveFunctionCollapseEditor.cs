using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveFunctionCollapse))]
public class WaveFunctionCollapseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WaveFunctionCollapse myScript = (WaveFunctionCollapse)target;
        base.OnInspectorGUI();
        if(GUILayout.Button("Run WFC"))
        {
            myScript.Run();
        }
    }
}
