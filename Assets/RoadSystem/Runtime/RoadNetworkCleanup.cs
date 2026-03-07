using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Optional helper to clear all road/intersection views at play start to avoid stale references.
    /// </summary>
    public class RoadNetworkCleanup : MonoBehaviour
    {
        [SerializeField] private RoadNetworkRuntime runtime;
        [SerializeField] private bool clearOnStart = true;

        private void Start()
        {
            if (clearOnStart && runtime != null)
            {
                runtime.ClearAllViews();
            }
        }
    }
}
