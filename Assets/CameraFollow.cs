
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Target object to follow
    public float smoothSpeed = 0.125f; // Speed at which the camera moves
    public Vector3 offset; // Offset from the target object

    void FixedUpdate()
    {
        if (target != null)
        {

            transform.LookAt(target);
        }
    }
}