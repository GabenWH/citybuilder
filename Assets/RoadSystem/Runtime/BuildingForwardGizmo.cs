using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityBuilder.Roads
{
    /// <summary>
    /// Shows a forward arrow in the editor when the object is selected. Rotate the object to change facing.
    /// </summary>
    [ExecuteAlways]
    public class BuildingForwardGizmo : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        [SerializeField] private float length = 2f;

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 dir = transform.forward.normalized;
            Vector3 end = origin + dir * length;

            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(origin, end);

#if UNITY_EDITOR
            Handles.color = gizmoColor;
            Handles.ArrowHandleCap(0, end, Quaternion.LookRotation(dir), length * 0.15f, EventType.Repaint);
#endif
        }
    }
}
