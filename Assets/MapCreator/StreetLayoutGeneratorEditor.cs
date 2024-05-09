using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;

[CustomEditor(typeof(StreetLayoutGenerator))]
public class StreetLayoutCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector options, which now includes the GeoJSON data field
        DrawDefaultInspector();

        StreetLayoutGenerator script = (StreetLayoutGenerator)target;

        // The GeoJSON data field is now part of the StreetLayoutCreator and will be drawn by DrawDefaultInspector()
        // If you need to reference or use the geoJsonData in the editor script, access it via the target object:
        // string geoJsonData = script.geoJsonData;

        // Add a button to the inspector for the StreetLayoutCreator component
        if (GUILayout.Button("Create Streets"))
        {
            // Assuming you want to read from a file path specified in geoJsonData
            if (script.geoJsonFile != null)
            {
                string jsonString = script.geoJsonFile.text;
                script.CreateStreets(jsonString);
            }
            else
            {
                Debug.LogError("GeoJSONFile not assigned");
            }
        }
    }
}
