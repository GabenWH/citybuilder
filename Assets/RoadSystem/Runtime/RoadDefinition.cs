using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Authoring data for a road type: geometry, lanes, and visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "RoadDefinition", menuName = "CityBuilder/Road Definition")]
    public class RoadDefinition : ScriptableObject
    {
        [Header("Geometry")]
        [Min(0.01f)] public float width = 8f;
        [Min(1)] public int lanes = 2;
        [Tooltip("Optional per-lane overrides; empty uses even lane spacing across width.")]
        public List<RoadSegment.LaneDefinition> laneDefinitions = new List<RoadSegment.LaneDefinition>();

        [Header("Visuals")]
        public Material roadMaterial;
        public Material intersectionMaterial;

        [Header("Behavior")]
        public float speedLimitKph = 50f;
    }
}
