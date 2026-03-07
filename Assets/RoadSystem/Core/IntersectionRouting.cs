using System.Collections.Generic;
using System;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// A reference to a lane at a specific node (incoming or outgoing).
    /// </summary>
    [System.Serializable]
    public class LaneEndpoint
    {
        public int NodeId { get; }
        public int SegmentId { get; }
        public int LaneIndex { get; }
        public bool IsIncoming { get; }

        public LaneEndpoint(int nodeId, int segmentId, int laneIndex, bool isIncoming)
        {
            NodeId = nodeId;
            SegmentId = segmentId;
            LaneIndex = laneIndex;
            IsIncoming = isIncoming;
        }
    }

    /// <summary>
    /// Defines allowed targets from a given incoming lane.
    /// </summary>
    [System.Serializable]
    public class LaneConnection
    {
        public LaneEndpoint From { get; }
        public List<LaneEndpoint> To { get; }

        public LaneConnection(LaneEndpoint from, List<LaneEndpoint> to)
        {
            From = from;
            To = to ?? new List<LaneEndpoint>();
        }
    }

    /// <summary>
    /// Concrete connector geometry from one lane to another (polyline).
    /// </summary>
    [System.Serializable]
    public class LaneConnector
    {
        public LaneEndpoint From { get; }
        public LaneEndpoint To { get; }
        public List<Vector3> Points { get; }
        public int LaneId { get; set; }
        public int EndNodeId { get; set; }

        public LaneConnector(LaneEndpoint from, LaneEndpoint to, List<Vector3> points)
        {
            From = from;
            To = to;
            Points = points ?? new List<Vector3>();
            LaneId = -1;
            EndNodeId = to != null ? to.NodeId : -1;
        }
    }

    /// <summary>
    /// Scaffolding for intersection signaling/phases.
    /// </summary>
    [System.Serializable]
    public class IntersectionSignalPhase
    {
        public string Name;
        public float Duration;
        public List<LaneConnection> AllowedConnections = new List<LaneConnection>();
    }

    [System.Serializable]
    public class IntersectionSignalPlan
    {
        public List<IntersectionSignalPhase> Phases = new List<IntersectionSignalPhase>();
        public int StartPhaseIndex = 0;
    }

    public enum IntersectionType
    {
        Star = 0,
        Custom = 1,
        Roundabout = 2,
        Cross = 3
    }
}
