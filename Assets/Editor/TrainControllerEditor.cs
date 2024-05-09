using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrainController))]
public class TrainControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrainController myScript = (TrainController)target;
        if (GUILayout.Button("Switch Direction"))
        {
            myScript.SwitchDirection();
        }
    }
}
