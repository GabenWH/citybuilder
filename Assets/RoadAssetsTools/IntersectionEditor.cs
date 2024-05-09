using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Intersection))]
public class IntersectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Intersection intersection = (Intersection)target;
        if (GUILayout.Button("Connect nearby roads")){
            intersection.AttachToNearbyRoads();
        }
        if (GUILayout.Button("Add Slot"))
        {
            GameObject slotObj = new GameObject("Slot");
            slotObj.transform.SetParent(intersection.transform, false);
            IntersectionSlot slot = slotObj.AddComponent<IntersectionSlot>();
            intersection.slots.Add(slot);
        }
    }

    void OnSceneGUI()
    {
        Intersection intersection = (Intersection)target;
        if (intersection.slots.Count > 0)
        {
            foreach (IntersectionSlot slot in intersection.slots)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(slot.transform.position, slot.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(slot.transform, "Move Slot");
                    slot.transform.position = newPosition;
                }
            }
        }
    }
}
