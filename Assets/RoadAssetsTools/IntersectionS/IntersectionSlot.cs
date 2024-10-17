using UnityEngine;

public class IntersectionSlot : MonoBehaviour
{
    public Vector3 positionOffset;
    public Quaternion rotationOffset;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 worldPosition = transform.position + positionOffset;
        Gizmos.DrawSphere(worldPosition, 0.1f);  // Visualize the slot position
        Gizmos.DrawLine(worldPosition, worldPosition + rotationOffset * Vector3.forward * 0.5f);  // Visualize the slot orientation
    }
}
