using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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
        Intersection intersection = (Intersection)target;
        intersection.CheckNodes();
        
        intersection.connectedRoads.Clear();
        for (int i = intersection.nodes.Count - 1; i >= 0; i--)
        {
            if (intersection.nodes[i]== null){
                intersection.nodes.RemoveAt(i);
                continue;
            }
            DestroyImmediate(intersection.nodes[i].gameObject);
            
            intersection.nodes.RemoveAt(i);
        }
        intersection.nodes.Clear();
        intersection.AttachToNearbyRoads();
        SphereCollider collider = intersection.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.radius = intersection.detectionRadius;
        }
    }
}
