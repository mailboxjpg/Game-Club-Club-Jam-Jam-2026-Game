#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomCreator))]
public class RoomCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomCreator roomCreator = (RoomCreator)target;

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build Room"))
        {
            roomCreator.BuildRoom();
        }
        if (GUILayout.Button("Clear Room"))
        {
            roomCreator.ClearRoom();
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif