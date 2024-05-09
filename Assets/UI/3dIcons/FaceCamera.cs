using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Transform playerTransform; // Assign the player's transform in the inspector
    public Transform sphereCenter; // Assign the sphere's center transform in the inspector
    public float fixedDistanceFromSphere = 5f; // Distance from the sphere's center

    void Update()
    {
        // Calculate direction from sphere center to player
        Vector3 directionToPlayer = (playerTransform.position - sphereCenter.position).normalized;

        // Set the text GameObject's position
        transform.position = sphereCenter.position + directionToPlayer * fixedDistanceFromSphere;

        // Rotate text to face player
        transform.rotation = Quaternion.LookRotation(transform.position - playerTransform.position);
        
        // Optionally, adjust for the text to be upright relative to the world or player
        transform.Rotate(0, 0, 0); // Adjust if text appears backwards
    }
}