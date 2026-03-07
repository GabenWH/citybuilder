using UnityEngine;
using System.Collections.Generic;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Minimal interface for anything that moves along roads/lanes/connectors.
    /// </summary>
    public interface RoadUser
    {
        /// <summary> Backing state for lane/node/progress tracking. </summary>
        RoadUserState State { get; }

        /// <summary> Called to assign a new route (as lane connectors or node ids).</summary>
        void SetRoute(List<Vector3> pathPoints);

        /// <summary> Current world position of the user.</summary>
        Vector3 Position { get; }

        /// <summary> Desired speed in m/s.</summary>
        float DesiredSpeed { get; }

        /// <summary> Called each frame to advance along the path.</summary>
        void Tick(float deltaTime);

        /// <summary> Request a route from the network between two nodes and begin following it.</summary>
        void RequestRoute(RoadNetwork network, int startNodeId, int endNodeId);
    }
}
