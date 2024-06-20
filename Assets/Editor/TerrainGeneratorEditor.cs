using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainGenerator script = (TerrainGenerator)target;
        if(GUILayout.Button("Create Terrain")){
            script.GenerateTerrain();
        }
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag and drop terrain JSON file here");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        var filePath = AssetDatabase.GetAssetPath(draggedObject);
                        if (filePath.EndsWith(".json"))
                        {
                            string data = File.ReadAllText(filePath);
                            script.LoadDownloadedTerrain(data);
                            break; // Assuming you want to handle one file at a time
                        }
                    }
                }
                break;
        }
    }
}
