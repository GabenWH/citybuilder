using UnityEngine;
using System.Collections.Generic;

public class Intersection : MonoBehaviour
{
    public List<IntersectionSlot> slots;
    public List<Road> connectedRoads = new List<Road>();
    public float detectionRadius = 5.0f;

    public void AttachToNearbyRoads()
    {
        // Detect all road endpoints within a certain radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            Road road = hitCollider.transform.parent.GetComponent<Road>();
            Debug.Log(road == null);
            if (road != null && !connectedRoads.Contains(road))
            {
                // Assume road endpoints are stored as the first and last points in the road
                if (Vector3.Distance(transform.position, road.controlPoints[0] + road.transform.position) < detectionRadius)
                {
                    connectedRoads.Add(road);
                    road.ConnectToIntersection(this, true);  // Connect start of the road
                }
                if (Vector3.Distance(transform.position, road.transform.position + road.controlPoints[road.controlPoints.Length - 1]) < detectionRadius)
                {
                    connectedRoads.Add(road);
                    road.ConnectToIntersection(this, false);  // Connect end of the road
                }
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        // Visualize the detection area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
    void OnDrawGizmos()
    {
        // Highlight the main intersection point
        Gizmos.color = Color.green;  // Using green to distinguish the main intersection
        Gizmos.DrawWireSphere(transform.position, detectionRadius/2);  // Larger sphere for main intersection
    }
}
