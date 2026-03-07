using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Authoring data for a placeable building, including its prefab and entry/exit role.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingDefinition", menuName = "CityBuilder/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject prefab;

        [Header("Traffic Roles")]
        public EntryExitType role = EntryExitType.None;

        [Tooltip("Optional offset from the building origin to use when spawning agents.")]
        public Vector3 entryOffset = Vector3.zero;

        [Tooltip("Optional offset from the building origin to use when despawning agents.")]
        public Vector3 exitOffset = Vector3.zero;

        [Header("Metadata")]
        public string displayName;
        public int capacity;

        public bool HasEntry => (role & EntryExitType.Entry) != 0;
        public bool HasExit => (role & EntryExitType.Exit) != 0;
    }
}
