using UnityEditor;
using UnityEngine;
using CityBuilder.Roads;
using System.IO;

[CustomEditor(typeof(RoadNetworkLoader))]
public class RoadNetworkLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("runtime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingParent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loadOnStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingPrefabRegistry"));

        var jsonPathProp = serializedObject.FindProperty("jsonPath");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(jsonPathProp, new GUIContent("JSON Path"));
        if (GUILayout.Button("Select...", GUILayout.MaxWidth(80)))
        {
            var chosen = EditorUtility.OpenFilePanel("Select Road Network JSON", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(chosen))
            {
                jsonPathProp.stringValue = chosen;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Network"))
        {
            ((RoadNetworkLoader)target).Save();
        }
        if (GUILayout.Button("Load Network"))
        {
            ((RoadNetworkLoader)target).Load();
        }
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
        serializedObject.ApplyModifiedProperties();
    }
}

