using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RainManager))]
public class RainManagerEditor : Editor
{

    /// <summary>
    /// Creates button.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RainManager myScript = (RainManager)target;
        if (GUILayout.Button("OK"))
        {
            myScript.MakeRain();
        }
    }
}

