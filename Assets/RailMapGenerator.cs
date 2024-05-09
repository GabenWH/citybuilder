using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailMapGenerator : MonoBehaviour
{
    public RailMap railMap; // Reference to the RailMap component
    public Track startTrack; // The starting track in the railway setup

    void Start()
    {
        GenerateRailMapFromStartTrack(startTrack);
    }

    // Method to generate the rail map from a given start track
    private void GenerateRailMapFromStartTrack(Track startTrack)
    {
        // A queue to hold tracks that need to be processed
        Queue<Track> trackQueue = new Queue<Track>();
        trackQueue.Enqueue(startTrack);

        // A set to keep track of processed tracks to avoid duplication
        HashSet<Track> processedTracks = new HashSet<Track>();

        while (trackQueue.Count > 0)
        {
            Track currentTrack = trackQueue.Dequeue();

            // Skip if this track has already been processed
            if (processedTracks.Contains(currentTrack))
                continue;

            // Add this track to the processed set
            processedTracks.Add(currentTrack);

            // Get connected tracks from the current track
            // Assuming currentTrack has a method or a property to get its connected tracks
            List<Track> connectedTracks = currentTrack.GetTerminations();

            // Add the current track and its connections to the rail map
            railMap.AddToNetworkMap(currentTrack, connectedTracks);

            // Enqueue the connected tracks for processing
            foreach (Track connectedTrack in connectedTracks)
            {
                if (!processedTracks.Contains(connectedTrack))
                {
                    trackQueue.Enqueue(connectedTrack);
                }
            }
        }
    }
}