using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSwitch : Track
{
    public List<Track> outputTracks; // List of potential output tracks
    public int selectedOutputIndex; // Index of the selected output track

    public void selectTrack(Track track){
        int isInSelection = outputTracks.IndexOf(track);
        if(isInSelection==-1){
            Debug.Log(track.ToString() + " Not Found");
        }
        else{
            selectedOutputIndex = isInSelection;
        }
    }

    public void changeOutputIndex(int newIndex){
        selectedOutputIndex = newIndex%outputTracks.Count;
    }
    public override Track GetAttachedTrack(TrainDirection direction)
    {
        // If the train is coming from the input track (Ascending direction), return the selected output track
        if (direction == TrainDirection.Ascending)
        {
            if (outputTracks.Count == 0)
            {
                Debug.LogError("No output tracks assigned to the TrackSwitch!");
                return null;
            }

            return outputTracks[selectedOutputIndex];
        }
        // Otherwise, return the input track
        else
        {
            return attachedStartTrack;
        }
    }
    public override Track GetAttachedEndTrack()
    {
        return outputTracks[selectedOutputIndex];
    }
    public override List<Track> GetTerminations()
    {
        List<Track> tracks = new List<Track>();
        foreach (var track in outputTracks)
        {
            if (track != null)
            {
                tracks.Add(track);
            }
        }
        return tracks;
    }

    public override List<Track> GetConnectedTracks()
    {
        List<Track> connectedTracks = new List<Track>();

        // Check and add the start track if not null
        if (attachedStartTrack != null)
        {
            connectedTracks.Add(attachedStartTrack);
        }

        // Iterate through outputTracks and add them if they're not null
        foreach (var track in outputTracks)
        {
            if (track != null)
            {
                connectedTracks.Add(track);
            }
        }

        return connectedTracks;
    }
}