using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum TrainDirection
{
    Ascending,
    Descending
}

public class TrainController : MonoBehaviour
{

    public event EventHandler TrackTransitioned;
    public float speed = 5f;
    public float waypointReachedThreshold = 0.1f;
    public float rotationSpeed = 5f;

    public Track currentTrack;
    public Transform currentWaypoint;
    public Transform previousWaypoint;
    public TrainDirection trainDirection = TrainDirection.Ascending;
    public TrainDirection trainOrientation = TrainDirection.Ascending;

    // Flag to determine if the train can switch direction manually
    public bool canSwitchDirection = true;

    public virtual void OnTrackTransitioned() {
        // Raise the event
        Debug.Log("Firing: TrackTransitioned?");
        TrackTransitioned?.Invoke(this, EventArgs.Empty);
    }
    public virtual void Start()
    {
        // Check if there's a current track assigned
        if (currentTrack == null)
        {
            Debug.LogError("No current track assigned to the TrainController!");
            return;
        }
        previousWaypoint = currentWaypoint;
    }

    public virtual void Update()

    {

        /*
        if (Input.GetKeyDown(KeyCode.W))
    {
        Debug.Log("w");
        if (trainDirection == TrainDirection.Descending && previousWaypoint != null && currentWaypoint != null)
        {
            SwitchDirection();
        } 
    {
        }
    }
    if (Input.GetKeyDown(KeyCode.S))
    {
        if (trainDirection == TrainDirection.Ascending && previousWaypoint != null && currentWaypoint != null)
        {
            SwitchDirection();
        }
    }
    */
        MoveTrain();
        //MoveTowardsEndOfTrack();
        //TransitionToNextTrackIfNeeded();
        RotateTrain();
    }
    public virtual void UpdateTrackSwitchIfNeeded(int index)
    {
        if(currentTrack is TrackSwitch trackSwitch){
            trackSwitch.selectedOutputIndex = index;
        }
        // Step 1: Check if the current or upcoming track involves a switch.
        // This may involve checking the currentTrack or the next track in the route for being a TrackSwitch type.

        // Step 2: Determine the correct direction for the switch.
        // Based on the train's destination or the planned route, decide which direction the track switch should be set.
        // This might require comparing the next track in the route with the possible paths of the switch.

        // Step 3: Apply the switch change.
        // If the track is a switch and requires changing, adjust the switch to the correct direction.
        // This ensures the train continues on the correct path towards its destination.

        // Note: Consider edge cases, such as when multiple trains are interacting with the same switch,
        // to ensure coherent behavior in a dynamic environment.
    }
    public virtual void UpdateTrackSwitchIfNeeded(Track selectedTrack){
        
    }
    private void MoveTowardsEndOfTrack()
    {
        // Logic to move the train towards the current waypoint
        Vector3 directionToWaypoint = (currentWaypoint.position - transform.position).normalized;
        float distanceThisFrame = speed * Time.deltaTime;
        if (currentWaypoint != null)
        {
            transform.position += directionToWaypoint * distanceThisFrame;
        }
    }

    private void TransitionToNextTrackIfNeeded()
    {
        if (currentWaypoint==null &&currentTrack.IsTransitioningToNextTrack(previousWaypoint,trainDirection))
        {
            // Determine the next track, considering the TrackSwitch logic if applicable
            // Update currentTrack, currentWaypoint, etc.
            currentTrack = currentTrack.GetAttachedTrack(trainDirection);
            OnTrackTransitioned();
            if(currentTrack is TrackSwitch trackSwitch) UpdateTrackSwitchIfNeeded(trackSwitch);
            currentWaypoint = (trainDirection == TrainDirection.Ascending) ? currentTrack.GetFirstWaypoint() : currentTrack.GetLastWaypoint();
        }
    }

    private void MoveTrain()
    {
        if ( currentTrack == null)
        {
            return;
        }

        // Calculate the direction from the train's current position to the current waypoint
        Vector3 directionToWaypoint = (currentWaypoint.position - transform.position).normalized;

        // Calculate the distance the train should move in this frame
        float distanceThisFrame = speed * Time.deltaTime;

        // Check if the train has reached the current waypoint within the threshold
        if(currentTrack.IsTerminatingWaypoint(currentWaypoint,trainDirection)&&!currentTrack.HasAttachedTrack(currentWaypoint)){
            currentWaypoint = currentWaypoint;//essentially the same thing as setting it to stop
        }
        else if (Vector3.Distance(transform.position, currentWaypoint.position) <= waypointReachedThreshold)
        {
            
            // Move to the next waypoint based on the direction of the train
            previousWaypoint = currentWaypoint;
            currentWaypoint = currentTrack.GetNextWaypoint(currentWaypoint, trainDirection);
            // If there is no next waypoint, switch to the attached track if available
            TransitionToNextTrackIfNeeded();
        }

        // Move towards the current waypoint
        if (currentWaypoint != null)
        {
            transform.position += directionToWaypoint * distanceThisFrame;
        }
    }
    public void SwitchDirection()
{
    // Save the current direction to be able to restore it in case of failure
    TrainDirection oldDirection = trainDirection;

    // Predict the new direction
    TrainDirection newDirection = trainDirection == TrainDirection.Ascending 
                                  ? TrainDirection.Descending 
                                  : TrainDirection.Ascending;

    (Transform predictedPreviousWaypoint, Track predictedPreviousTrack) = currentTrack.GetPreviousWaypointAndTrack(oldDirection, currentWaypoint);

    // Check if there is a previous waypoint and track in the new predicted direction
    if (predictedPreviousWaypoint != null && predictedPreviousTrack != null)
    {
        // Set the train's current waypoint and track to the predicted previous ones
        currentWaypoint = predictedPreviousWaypoint;
        currentTrack = predictedPreviousTrack;
        trainDirection = newDirection;  // Successfully switch the direction here
    }
    else
    {
        // Restore the old direction and possibly notify the user
        trainDirection = oldDirection;
        Debug.Log("The train cannot switch direction at this point!");
    }
}

    private void RotateTrain()
    {
        if (currentWaypoint == null || currentTrack == null)
        {
            return;
        }

        // Calculate the direction from the train's current position to the current waypoint
        Vector3 directionToWaypoint = currentWaypoint.position - transform.position;

        // Calculate the rotation step based on the rotation speed
        float rotationStep = rotationSpeed * Time.deltaTime;

        // Calculate the rotation to look at the current waypoint
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);

        // Smoothly rotate the train towards the current waypoint
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationStep);
    }
}