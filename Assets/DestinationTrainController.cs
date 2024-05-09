using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationTrainController : TrainController
{
    public float CruiseSpeed = 5;
    public float distanceSpeed = 5f/10f;
    public Route route;

    public DestinationTrainController(){
        this.TrackTransitioned += (sender, e)=>FollowRoute();
    }

    public override void Update(){
        base.Update();
        UpdateSpeed();
    }

    void UpdateSpeed() {
    if (route == null || route.route.Count == 0) {
        speed = 0;
        /*
        if (currentTrack != Homebase) {
            // Set speed to return to Homebase
        } else {
            // Stop the train if it's at Homebase and there's no route
            speed = 0;
        }
        */
    }
    else if(route.route.Count == 1 && route.finalDestination != null)
    {
        if(Vector3.Distance(this.transform.position,route.finalDestination.position)<distanceSpeed){
            speed=0;
        }
        else{
            speed=CruiseSpeed;
        }
    }
    else{
        speed = CruiseSpeed;
    }
}
    public RailMap railMap; // Reference to the RailMap in your game
    // Method to navigate to the closest track to a given position
    public void NavigateToPosition(Transform destination)
    {
        // Find the closest track to the destination
        Track destinationTrack = railMap.GetClosestTrack(destination);


        // Find the closest track to the current position of the train
        Track startTrack = railMap.GetClosestTrack(transform);

        // Compute the route from the current position to the destination track
        route = railMap.ComputeRoute(startTrack, destinationTrack);
        route.finalDestination = destinationTrack.GetClosestWaypoint(destination.position);
        if(currentTrack != route.route.Peek()){
            Debug.LogError("Route not equal to start:\n" + currentTrack.ToString()+ " " + route.route.Peek().ToString());
        }
        // Move the train along this route
    }
    public void NavigateToPosition(Track track){
        Route createdRoute = railMap.ComputeRoute(currentTrack, track);
        Debug.Log(createdRoute);
        route = createdRoute;
    }
    public void NavigateToPosition(Track track, Transform destination){
        route = railMap.ComputeRoute(currentTrack, track);
        route.finalDestination = track.GetClosestWaypoint(destination.position);
    }

    //this is entirely unnec, but leaving it in becase it may be useful in the future to my robot overlord
    public override void UpdateTrackSwitchIfNeeded(Track track){
    }
    public virtual void EndofRoute(){
        this.TrackTransitioned -= (sender, e)=>FollowRoute();
        //rest of the script
    }
    // Coroutine to move the train along a given route
    private void FollowRoute()
    {
        route.route.Dequeue();
        if(route.route.Count == 0){
            EndofRoute();
        }
        else if(route.route.Peek() is TrackSwitch trackSwitch){
            List<Track> routeList = new List<Track>(route.route);
            Debug.Log(string.Join(", ", routeList));
            if(routeList.Count>1){
                trackSwitch.selectTrack(routeList[1]);
            }
        }
    }
}
