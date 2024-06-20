using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Make sure your custom editor matches the type it's supposed to edit
[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector options
        DrawDefaultInspector();

        BuildingGenerator script = (BuildingGenerator)target;

        // Button to trigger building generation
        if (GUILayout.Button("Generate Buildings"))
        {
            if (!script.BuildingLayout)
            {
                script.BuildingLayout = new GameObject("Polygons Container");
            }

            string geojsonString = script.geoJsonFile.text;
            script.LoadBuildingData(geojsonString);
        }

        // Create a drag-and-drop area for new JSON files
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag and drop building data JSON file here");

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
                            script.LoadBuildingData(data);
                            break; // Handle one file at a time
                        }
                    }
                }
                break;
        }
    }
}