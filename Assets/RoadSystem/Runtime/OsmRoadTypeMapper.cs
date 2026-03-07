using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    [System.Serializable]
    public class OsmRoadTemplate
    {
        public string osmTag; // e.g., "residential", "primary"
        public float width = 8f;
        public int lanes = 2;
        public Material roadMaterial;
        public Material laneMarkingMaterial;
    }

    /// <summary>
    /// Maps OSM road type strings to internal road settings. Falls back to defaults if unknown.
    /// </summary>
    public class OsmRoadTypeMapper : MonoBehaviour
    {
        [SerializeField] private List<OsmRoadTemplate> templates = new List<OsmRoadTemplate>();
        [SerializeField] private float defaultWidth = 8f;
        [SerializeField] private int defaultLanes = 2;
        [SerializeField] private Material defaultRoadMaterial;
        [SerializeField] private Material defaultLaneMarkingMaterial;
        [SerializeField] private Material undefinedRoadMaterial;
        [SerializeField] private bool promptOnUnknown = false;

        private readonly Dictionary<string, OsmRoadTemplate> _templateMap = new Dictionary<string, OsmRoadTemplate>();

        private void Awake()
        {
            _templateMap.Clear();
            foreach (var t in templates)
            {
                if (t != null && !string.IsNullOrEmpty(t.osmTag))
                {
                    _templateMap[t.osmTag] = t;
                }
            }
        }

        public OsmRoadTemplate GetTemplateOrDefault(string osmTag)
        {
            if (!string.IsNullOrEmpty(osmTag) && _templateMap.TryGetValue(osmTag, out var tpl))
            {
                return tpl;
            }
            if (promptOnUnknown)
            {
                // In a real implementation, trigger UI to let the user define this tag and add it to templates.
                Debug.LogWarning($"OSM tag '{osmTag}' not defined. Prompting for definition is required.");
            }
            return new OsmRoadTemplate
            {
                osmTag = osmTag ?? "unknown",
                width = defaultWidth,
                lanes = defaultLanes,
                roadMaterial = undefinedRoadMaterial != null ? undefinedRoadMaterial : defaultRoadMaterial,
                laneMarkingMaterial = defaultLaneMarkingMaterial
            };
        }
    }
}
