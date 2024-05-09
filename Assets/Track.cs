using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public enum TrackDirection
{
    Ascending,
    Descending
}
public enum TrackStatus
{
    Free,
    Reserved,
    PreMaintenance,
    Maintenance
}

public class Track : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public Material material;
    public Transform[] waypoints; // Array of waypoint Transforms
    public Track attachedStartTrack; // Reference to the attached track at the start of this track

    public Track attachedEndTrack; // Reference to the attached track at the end of this track
    public TrackDirection direction = TrackDirection.Ascending; // Direction of the track
    public TrackStatus status = TrackStatus.Free;
    public List<TrainController> trainsOnTrack = new List<TrainController>();
    public List<TrainController> trainsReservingTrack = new List<TrainController>();



    private void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
    }
    private void Start()
    {
        // Check if there are waypoints to follow
        if (waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned for the track!");
            return;
        }
        UpdateLineRenderer();
    }

    //Track Management
    public void Reserve(TrainController train)
    {
        if (CanReserve())
        {
            trainsOnTrack.Add(train);
            status = TrackStatus.Reserved;
        }
    }
    public bool CanReserve()
    {
        return status == TrackStatus.Free || status == TrackStatus.Reserved;
    }
    public void Release(TrainController train)
    {
        trainsOnTrack.Remove(train);
        if (trainsOnTrack.Count == 0)
        {
            if (status == TrackStatus.PreMaintenance)
            {
                status = TrackStatus.Maintenance;
            }
            else
            {
                status = TrackStatus.Free;
            }
        }
    }
    public void SetPreMaintenance()
    {
        if (status == TrackStatus.Free || status == TrackStatus.Reserved)
        {
            status = TrackStatus.PreMaintenance;
        }
    }

public virtual List<Track> GetConnectedTracks()
    {
        List<Track> connectedTracks = new List<Track>();

        // Add logic here to determine what tracks are connected.
        // This could involve checking any attached tracks, and if it's a TrackSwitch, any output tracks.
        // Example:
        if (attachedStartTrack != null)
        {
            connectedTracks.Add(attachedStartTrack);
        }
        if (attachedEndTrack != null)
        {
            connectedTracks.Add(attachedEndTrack);
        }

        // If there are other ways tracks can be connected in your implementation, add them here.

        return connectedTracks;
    }

    //Train Display Functions
    public void UpdateLineRenderer()
    {
        if (lineRenderer == null || waypoints == null) return;

        lineRenderer.positionCount = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
        {
            lineRenderer.SetPosition(i, waypoints[i].position);
        }
        lineRenderer.material = material;
    }
    protected virtual void OnDrawGizmos()
    {
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            // Set Gizmo color to green for start waypoint, red for end waypoint, and blue for others
            if (i == 0) Gizmos.color = Color.green;
            else if (i == waypoints.Length - 2) Gizmos.color = Color.red;
            else Gizmos.color = Color.blue;

            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        // Draw spheres to better visualize start and end points
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(waypoints[0].position, 0.5f); // adjust sphere size as needed

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 0.5f); // adjust sphere size as needed
    }

    //Track Navigation Functions
    public Transform GetNextWaypoint(Transform currentWaypoint, TrainDirection direction)
    {
        if (waypoints == null || currentWaypoint == null)
        {
            Debug.LogError("null pointer:\n Waypoint list:" + waypoints + "\ncurrent waypoint: " + currentWaypoint);
            return null;
        }

        int index = -1;

        // Find the index of the current waypoint in the track's waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == currentWaypoint)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            return null; // Waypoint not found in the track's waypoints
        }

        // Determine the next waypoint based on the direction of the train
        if (direction == TrainDirection.Ascending)
        {
            // Move to the next waypoint
            index++;
        }
        else
        {
            // Move to the previous waypoint
            index--;
        }

        // Check if the new index is within bounds of the waypoints array
        if (index >= 0 && index < waypoints.Length)
        {
            return waypoints[index];
        }

        return null; // Out of bounds, no next waypoint found
    }
    public bool IsTerminatingWaypoint(Transform waypoint, TrainDirection direction)
    {
        if (direction == TrainDirection.Descending)
        {
            return waypoint == waypoints[0];
        }
        else // TrainDirection.Descending
        {
            return waypoint == waypoints[waypoints.Length - 1];
        }
    }

    //Track Attachment and Management Functions
    public bool HasAttachedTrack(Transform waypoint)
    {
        if (waypoint == waypoints[waypoints.Length - 1])
        {
            return attachedEndTrack != null;
        }
        if (waypoint == waypoints[0])
        {
            return attachedStartTrack != null;
        }
        return false;
    }
    public virtual Track GetAttachedTrack(TrainDirection direction)
    {
        if (direction == TrainDirection.Ascending)
        {
            return attachedEndTrack;
        }
        else
        {
            return attachedStartTrack;
        }
    }
    public (Transform, Track) GetPreviousWaypointAndTrack(TrainDirection direction, Transform currentWaypoint)
    {
        Transform previousWaypoint = null;
        Track previousTrack = null;

        // Determine the index of the current waypoint in the waypoints array
        int currentWaypointIndex = System.Array.IndexOf(waypoints, currentWaypoint);

        if (direction == TrainDirection.Ascending)
        {
            // If the train is at the first waypoint and there is an attached start track
            if (currentWaypointIndex == 0 && attachedStartTrack != null)
            {
                previousWaypoint = attachedStartTrack.waypoints[attachedStartTrack.waypoints.Length - 1];
                previousTrack = attachedStartTrack;
            }
            else if (currentWaypointIndex > 0)
            {
                previousWaypoint = waypoints[currentWaypointIndex - 1];
                previousTrack = this;
            }
        }
        else // TrainDirection.Descending
        {
            // If the train is at the last waypoint and there is an attached end track
            if (currentWaypointIndex == waypoints.Length - 1 && attachedEndTrack != null)
            {
                previousWaypoint = attachedEndTrack.waypoints[0];
                previousTrack = GetAttachedEndTrack();
            }
            else if (currentWaypointIndex < waypoints.Length - 1)
            {
                previousWaypoint = waypoints[currentWaypointIndex + 1];
                previousTrack = this;
            }
        }
        return (previousWaypoint, previousTrack);
    }

    //helper function for trains
    public bool IsTransitioningToNextTrack(Transform currentWaypoint, TrainDirection direction)
    {
        // Check if the train is at the last waypoint in its direction
        bool atLastWaypoint = (direction == TrainDirection.Ascending && currentWaypoint == waypoints[waypoints.Length - 1])
                            || (direction == TrainDirection.Descending && currentWaypoint == waypoints[0]);

        // Check if there is an attached track in the train's direction
        bool hasAttachedTrack = HasAttachedTrack(currentWaypoint);

        // Return true if both conditions are met
        return atLastWaypoint && hasAttachedTrack;
    }

    public Transform GetFirstWaypoint()
    {
        return waypoints[0];
    }

    public Transform GetLastWaypoint()
    {
        return waypoints[waypoints.Length - 1];
    }

    public virtual Track GetAttachedEndTrack()
    {
        return attachedEndTrack;
    }
    public virtual List<Track> GetTerminations()
    {
        List<Track> tracks = new List<Track>();
        if (GetAttachedEndTrack() != null)
        {
            tracks.Add(attachedEndTrack);
        }
        return tracks;
    }

    public void SortAndAssignWaypoints()
    {
        // Get all child objects
        Transform[] children = GetComponentsInChildren<Transform>();

        // Prepare a list to hold child Transforms that have "tracklink" in their names
        List<Transform> trackLinks = new List<Transform>();

        foreach (Transform child in children)
        {
            if (child.gameObject.name.Contains("tracklink"))
            {
                trackLinks.Add(child);
            }
        }

        // Sort the list based on the number in the name
        trackLinks = trackLinks.OrderBy(t =>
        {
            string name = t.gameObject.name;
            int startIndex = name.IndexOf("(") + 1;
            int endIndex = name.IndexOf(")");

            if (startIndex >= 0 && endIndex >= 0)
            {
                string numberString = name.Substring(startIndex, endIndex - startIndex);
                if (int.TryParse(numberString, out int number))
                {
                    return number;
                }
            }

            // Return a large number as a default in case the name doesn't follow the expected format
            return int.MaxValue;
        }).ToList();

        // Convert the sorted list to an array and assign to waypoints
        waypoints = trackLinks.ToArray();
    }
    public Transform GetClosestWaypoint(Vector3 position)
    {
        Transform closestWaypoint = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = position;
        foreach(Transform waypoint in waypoints)
        {
            Vector3 directionToWaypoint = waypoint.position - currentPosition;
            float dSqrToWaypoint = directionToWaypoint.sqrMagnitude;
            if(dSqrToWaypoint < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToWaypoint;
                closestWaypoint = waypoint;
            }
        }

        return closestWaypoint;
    }
}