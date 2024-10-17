#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PauseIntersectionCreationWindow : EditorWindow
{
    private static StreetLayoutGenerator generator;

    public static void ShowWindow(StreetLayoutGenerator generatorReference)
    {
        generator = generatorReference;
        PauseIntersectionCreationWindow window = GetWindow<PauseIntersectionCreationWindow>("Resolve Conflict");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Intersection Conflict Detected", EditorStyles.boldLabel);
        GUILayout.Label("Options:");

        if (GUILayout.Button("Resolve Automatically"))
        {
            generator.ResolveConflictAutomatically();
            this.Close();
        }

        if (GUILayout.Button("Stop Operation"))
        {
            generator.StopCreation();
            this.Close();
        }

        if (GUILayout.Button("Continue Manually"))
        {
            SceneView.lastActiveSceneView.pivot = generator.currentConflictPosition;
            SceneView.lastActiveSceneView.size = 10;
            SceneView.lastActiveSceneView.Repaint();
            this.Close();
        }
    }
}
#endif
