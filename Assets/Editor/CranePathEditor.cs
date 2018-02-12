using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CranePath))]
public class CranePathEditor : Editor
{
    private void OnSceneGUI()
    {
        var path = (CranePath)target;
        foreach (var node in path.nodes)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newTargetPosition = Handles.PositionHandle(node.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "move");
                node.position = newTargetPosition;
            }
        }
        foreach (var node in path.nodes)
            foreach (var con in node.connections)
                Handles.DrawLine(node.position, con.connected.position);


    }
}
