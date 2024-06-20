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
        
    }
}
