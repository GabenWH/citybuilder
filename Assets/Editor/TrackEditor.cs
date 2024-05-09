using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Track))]
public class TrackEditor : Editor
{
    private Vector3 prevPosition; // variable to store the previous position of the track
    private bool autoDisconnect = true; // variable to toggle auto disconnect

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Track myScript = (Track)target;

        // toggle for auto disconnect
        autoDisconnect = GUILayout.Toggle(autoDisconnect, "Auto Disconnect Tracks");

        if (GUILayout.Button("AutoConnect Tracks"))
        {
            AutoConnectTracks(myScript);
        }
        if (GUILayout.Button("Grab and sort waypoints")){
            myScript.SortAndAssignWaypoints();
        }
    }

    // This method is called every time the scene view is repainted, so we can use it to check for changes in position
    protected virtual void OnSceneGUI()
    {
        Track myScript = (Track)target;
        
        if (autoDisconnect && prevPosition != myScript.transform.position)
        {
            AutoDisconnectTracks(myScript);
            prevPosition = myScript.transform.position;
        }
    }
    private void AutoConnectTracks(Track currentTrack) 
{
    float threshold = 5.0f; // define a suitable threshold for proximity

    // find all Track objects in the scene
    Track[] allTracks = FindObjectsOfType<Track>();
    float lowestStartDistance = threshold;
    float lowestEndDistance = threshold;
    foreach (Track track in allTracks) 
    {
        if (track == currentTrack)
            continue; // skip self
        float startDistance = Vector3.Distance(currentTrack.waypoints[0].position, track.waypoints[track.waypoints.Length-1].position); 
        float endDistance = Vector3.Distance(currentTrack.waypoints[currentTrack.waypoints.Length-1].position, track.waypoints[0].position);
        // Check if the closest points are within the threshold distance
        if(startDistance < threshold && startDistance < lowestStartDistance)
        {
            currentTrack.attachedStartTrack = track;
            lowestStartDistance = startDistance;
        }

        if(endDistance < threshold && endDistance< lowestEndDistance)
        {
            currentTrack.attachedEndTrack = track;
            lowestEndDistance = endDistance;
        }
    }
}
    private void AutoDisconnectTracks(Track currentTrack) 
{
    float threshold = 5.0f; // define a suitable threshold for proximity

    // if attachedStartTrack is not null
    if (currentTrack.attachedStartTrack != null) 
    {
        // calculate distances between the start point of this track and the start and end points of the attached start track
        float distStartStart = Vector3.Distance(currentTrack.waypoints[0].position, currentTrack.attachedStartTrack.waypoints[0].position);
        float distStartEnd = Vector3.Distance(currentTrack.waypoints[0].position, currentTrack.attachedStartTrack.waypoints[currentTrack.attachedStartTrack.waypoints.Length - 1].position);

        // if both distances are greater than the threshold, disconnect the tracks
        if (distStartStart > threshold && distStartEnd > threshold) 
        {
            currentTrack.attachedStartTrack.attachedEndTrack = null;
            currentTrack.attachedStartTrack = null;
        }
    }

    // if attachedEndTrack is not null
    if (currentTrack.attachedEndTrack != null) 
    {
        // calculate distances between the end point of this track and the start and end points of the attached end track
        float distEndStart = Vector3.Distance(currentTrack.waypoints[currentTrack.waypoints.Length - 1].position, currentTrack.attachedEndTrack.waypoints[0].position);
        float distEndEnd = Vector3.Distance(currentTrack.waypoints[currentTrack.waypoints.Length - 1].position, currentTrack.attachedEndTrack.waypoints[currentTrack.attachedEndTrack.waypoints.Length - 1].position);

        // if both distances are greater than the threshold, disconnect the tracks
        if (distEndStart > threshold && distEndEnd > threshold) 
        {
            currentTrack.attachedEndTrack.attachedStartTrack = null;
            currentTrack.attachedEndTrack = null;
        }
    }
}

}