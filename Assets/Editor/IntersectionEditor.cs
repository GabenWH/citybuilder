using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Intersection))]
public class IntersectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Intersection intersection = (Intersection)target;
        if(GUILayout.Button("Connect nearby roads")){
            intersection.connectedRoads.Clear();
            for(int i = intersection.nodes.Count-1; i >= 0;i--){
                if(intersection.nodes[i].gameObject!= null){
                    DestroyImmediate(intersection.nodes[i].gameObject);
                }
            }
            intersection.nodes.Clear();
            intersection.AttachToNearbyRoads();
        }
        if (GUILayout.Button("Add Node"))
        {
            GameObject slotObj = new GameObject("Node");
            slotObj.transform.SetParent(intersection.transform, false);
            Node slot = slotObj.AddComponent<Node>();
            intersection.nodes.Add(slot);
        }
    }

    void OnSceneGUI()
    {
        
    }
}
