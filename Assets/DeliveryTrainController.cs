using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryTrainController : DestinationTrainController
{
    public List<Track> trip;
    public Transform pickupLocation; // The location where cargo is picked up
    public RailMap railMap;
    public Transform deliveryDestination; // The final destination for delivery
    private bool hasCargo = false; // Flag to track whether the train is carrying cargo

    // Override methods or add new methods as needed
    public override void Start()
    {
        base.Start();
        // Additional setup for delivery train
    }



    // You might want to override the method that handles reaching the destination
    // to include picking up or delivering cargo based on the train's current state
    public virtual void OnReachDestination()
    {
        if (!hasCargo)
        {
            // Pick up cargo
            PickUpCargo();
        }
        else
        {
            // Deliver cargo
            DeliverCargo();
            // Possibly set the next destination back to the pickup location or to another destination
        }
    }

    private void PickUpCargo()
    {
        // Implement cargo pickup logic
        hasCargo = true;
        // Set the next destination to the delivery location
        //SetDestination(deliveryDestination.position);
    }

    private void DeliverCargo()
    {
        // Implement cargo delivery logic
        hasCargo = false;
        // Optionally, set the next destination or perform other actions post-delivery
    }
}
