using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Marker component to identify building instances for serialization/filtering.
    /// </summary>
    public class BuildingMarker : MonoBehaviour
    {
        [Tooltip("ID of the road node this building was snapped to when placed/serialized.")]
        public int attachedNodeId;
    }
}
