using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    [Serializable]
    public class RoadNodeData
    {
        public int id;
        public Vector3 position;
        public Vector2Int sector;
        public EntryExitType role;
    }

    [Serializable]
    public class RoadSegmentData
    {
        public int id;
        public int startNodeId;
        public int endNodeId;
        public float width;
        public int lanes;
        public List<Vector3> controlPoints = new List<Vector3>();
        public List<RoadSegment.LaneDefinition> laneDefinitions = new List<RoadSegment.LaneDefinition>();
        public Vector2Int sector;
    }

    [Serializable]
    public class BuildingData
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public int attachedNodeId;
    }

    [Serializable]
    public class RoadNetworkData
    {
        public List<RoadNodeData> nodes = new List<RoadNodeData>();
        public List<RoadSegmentData> segments = new List<RoadSegmentData>();
        public List<BuildingData> buildings = new List<BuildingData>();
    }
}
