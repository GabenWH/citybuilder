using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Data-first representation of a road user moving along lanes.
    /// </summary>
    public class RoadUserState
    {
        public int Id;
        public int CurrentLaneId;
        public float LaneT;
        public float Speed;
        public int NextLaneId = -1;
        public int TargetNodeId;
        public int Flags; // bitfield: e.g., queued, finished, etc.

        public RoadUserState(int id, int laneId, int targetNodeId, float initialSpeed = 0f)
        {
            Id = id;
            CurrentLaneId = laneId;
            TargetNodeId = targetNodeId;
            Speed = initialSpeed;
            LaneT = 0f;
        }
    }
}
