using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Road))]
public class RoadEditor : Editor
{
    void OnSceneGUI()
    {

        Road road = (Road)target;
        if (road.controlPoints == null)
            return;

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < road.controlPoints.Length; i++)
        {
            // Transform local position to world space
            Vector3 worldPosition = road.transform.TransformPoint(road.controlPoints[i]);
            // Display the handle in world space and allow for movement
            Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);

            // Check if the handle was moved
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Control Point");
                // Convert the new world space position back to local space
                road.controlPoints[i] = road.transform.InverseTransformPoint(newWorldPosition);
                EditorUtility.SetDirty(road);
            }
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(road, "Move Road Control Point");
            EditorUtility.SetDirty(road);
            road.BuildRoad(); // Rebuild the road if it uses a dynamic mesh based on control points
        }
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Road road = (Road)target;

        if (GUILayout.Button("Build Road"))
        {
            road.BuildRoad();
        }
        if(GUILayout.Button("Build End Colliders"))
        {
            road.BuildColliders();
        }
    }
}