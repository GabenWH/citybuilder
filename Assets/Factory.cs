using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class Factory : MonoBehaviour
{

    public Transform start; // Set these in the inspector
    public Transform end;
    public int waypointsCount = 10; // Default value, can be adjusted in the inspector
    public GameObject waypointPrefab; // A sphere or any other visual representation for waypoints
    private LineRenderer lineRenderer;

    private void Update()
    {
        // For this example, I'm using the Space key. Change this to any other input if needed.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var track = CreateTrack(start, end, waypointsCount);
        }
    }

    public Track CreateTrack(Transform start, Transform end, int waypointsCount)
    {
        List<Transform> waypoints = GenerateWaypoints(start, end, waypointsCount);

        GameObject trackObject = new GameObject("GeneratedTrack");
        Track track = trackObject.AddComponent<Track>();
        track.waypoints = waypoints.ToArray();
        foreach(Transform waypoint in waypoints){
            waypoint.parent = trackObject.transform;
        }

        return track;
    }

    private List<Transform> GenerateWaypoints(Transform start, Transform end, int count)
    {
        List<Transform> waypoints = new List<Transform>();


        Vector3 p0 = start.position;
        Vector3 p3 = end.position;

        // Calculate control points based on start and end rotations.
        Vector3 p1 = p0 + start.forward * Vector3.Distance(p0, p3) / 2;
        Vector3 p2 = p3 - end.forward * Vector3.Distance(p0, p3) / 2;


        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 point = CalculateBezierPoint(t, p0, p1, p2, p3);

            var waypoint = new GameObject($"Waypoint {i}").transform;
            waypoint.position = point;

            waypoints.Add(waypoint);
        }

        return waypoints;
    }

    private static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }
}