using UnityEngine;
using System.Collections.Generic;

using System;
using System.Linq;
using System.Collections.Generic;

public class RailMap : MonoBehaviour
{
    // This dictionary maps each Track to its connected Tracks
    private Dictionary<Track, List<Track>> networkMap = new Dictionary<Track, List<Track>>();

    // Adds a new track to the network map with its connections
    public void AddToNetworkMap(Track trackToAdd, List<Track> connectedTracks)
    {
        networkMap.Add(trackToAdd, connectedTracks);
    }

    // Removes a track from the network map and its connections from other tracks
    public void RemoveFromNetworkMap(Track trackToRemove)
    {
        networkMap.Remove(trackToRemove);
        foreach (var trackList in networkMap.Values)
        {
            trackList.Remove(trackToRemove);
        }
    }

    // Computes a route from the start track to the end track using a pathfinding algorithm
    public Route ComputeRoute(Track startTrack, Track endTrack)
    {
        var openSet = new HashSet<Track>();
        var closedSet = new HashSet<Track>();
        var gScore = new Dictionary<Track, float>();
        var fScore = new Dictionary<Track, float>();
        var cameFrom = new Dictionary<Track, Track>();

        gScore[startTrack] = 0;
        fScore[startTrack] = HeuristicCostEstimate(startTrack, endTrack);
        openSet.Add(startTrack);

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(track => fScore.ContainsKey(track) ? fScore[track] : float.MaxValue).First();

            if (current == endTrack)
            {
                Queue<Track> endRoute = new Queue<Track>(ReconstructPath(cameFrom, current));
                Route returnRoute = new Route(endTrack.waypoints[0],endRoute);
                foreach(Track track in endRoute){
                    Debug.Log(track);
                }
                return returnRoute;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in networkMap[current])
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                var tentativeGScore = gScore[current] + Distance(current, neighbor); // Implement Distance method based on your Track data

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, endTrack);
            }
        }
        Debug.Log("Failed to generate route");
        return new Route(); // Return an empty path if no path is found
    }


    private float HeuristicCostEstimate(Track a, Track b)
    {
        // Implement your heuristic here. A simple one could be straight-line distance.
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private List<Track> ReconstructPath(Dictionary<Track, Track> cameFrom, Track current)
    {
        List<Track> totalPath = new List<Track> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        foreach(Track track in totalPath){
            Debug.Log(track);
        }
        return totalPath;
    }

    // You need to implement the Distance method based on your Track class
    private float Distance(Track a, Track b)
    {
        // Return the distance between track a and track b
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    // Finds the closest track to a given position
    public Track GetClosestTrack(Transform position)
    {
        Track closestTrack = null;
        float closestDistance = float.MaxValue;

        foreach (var track in networkMap.Keys)
        {
            float distance = Vector3.Distance(position.position, track.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTrack = track;
            }
        }

        return closestTrack;
    }

    // Finds the closest terminating track to a given position
    public Track GetClosestTerminatingTrack(Transform position)
    {
        Track closestTerminatingTrack = null;
        float closestDistance = float.MaxValue;

        foreach (var track in networkMap.Keys)
        {
            if (networkMap[track].Count == 0) // Assuming terminating track means no connected tracks
            {
                float distance = Vector3.Distance(position.position, track.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTerminatingTrack = track;
                }
            }
        }

        return closestTerminatingTrack;
    }

    // Retrieves the closest waypoint from any track to a given position
    public Transform GetClosestWaypoint(Transform position)
    {
        Transform closestWaypoint = null;
        float closestDistance = float.MaxValue;

        foreach (var track in networkMap.Keys)
        {
            foreach (var waypoint in track.waypoints)
            {
                float distance = Vector3.Distance(position.position, waypoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWaypoint = waypoint;
                }
            }
        }

        return closestWaypoint;
    }
}