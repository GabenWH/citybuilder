using UnityEditor;
using UnityEngine;
using CityBuilder.Roads;

[CustomEditor(typeof(RoadUserSpawner))]
public class RoadUserSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var spawner = (RoadUserSpawner)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(spawner == null || spawner.gameObject == null))
        {
            if (GUILayout.Button("Center Scene View On Start Node"))
            {
                FrameNode(spawner, spawner.StartNodeIdValue);
            }

            if (GUILayout.Button("Center Scene View On Exit Node"))
            {
                FrameNode(spawner, spawner.EndNodeIdValue);
            }

            EditorGUILayout.HelpBox("Enable 'Show Node Gizmos' on the component to visualize start/end nodes as gizmo spheres.", MessageType.None);
        }
    }

    private void FrameNode(RoadUserSpawner spawner, int nodeId)
    {
        if (spawner == null || spawner.Network == null) return;
        if (!spawner.Network.TryGetNode(nodeId, out var node)) return;
        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.Frame(new Bounds(node.Position, Vector3.one), false);
        }
    }
}
