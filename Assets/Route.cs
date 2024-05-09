using System.Collections.Generic;
using UnityEngine;

public class Route
{
    public Transform finalDestination;
    public Queue<Track> route;

    // Constructor
    public Route(Transform finalDestination, Queue<Track> route)
    {
        this.finalDestination = finalDestination;
        this.route = route;
    }
    public Route(){
        
    }

    // You might want to add methods here for manipulating or querying the route
}