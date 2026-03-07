using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Serializes the current RoadNetworkRuntime (nodes, segments) and buildings under a designated parent to JSON.
    /// </summary>
    public static class RoadNetworkSerializer
    {
        public static void SaveToJsonFile(RoadNetworkRuntime runtime, Transform buildingParent, string jsonPath)
        {
            if (runtime == null || runtime.Network == null || string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogWarning("Cannot save network to json: missing runtime/network/path.");
                return;
            }

            var data = BuildData(runtime, buildingParent);
            var json = JsonUtility.ToJson(data, true);
            var dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(jsonPath, json);
        }

        public static void LoadFromJsonFile(RoadNetworkRuntime runtime, Transform buildingParent, string jsonPath, Dictionary<string, GameObject> prefabLookup = null)
        {
            if (runtime == null || runtime.Network == null || string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogWarning("Cannot load network from json: missing runtime/network/path.");
                return;
            }
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Cannot load network: json file not found at {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<RoadNetworkData>(json);
            if (data == null)
            {
                Debug.LogWarning($"Failed to parse road network json at {jsonPath}");
                return;
            }

            ApplyData(runtime, buildingParent, data, prefabLookup);
        }

        private static RoadNetworkData BuildData(RoadNetworkRuntime runtime, Transform buildingParent)
        {
            var data = new RoadNetworkData();

            foreach (var node in runtime.Network.Nodes.Values)
            {
                data.nodes.Add(new RoadNodeData
                {
                    id = node.Id,
                    position = node.Position,
                    sector = node.Sector,
                    role = node.Role
                });
            }

            foreach (var seg in runtime.Network.Segments.Values)
            {
                data.segments.Add(new RoadSegmentData
                {
                    id = seg.Id,
                    startNodeId = seg.StartNodeId,
                    endNodeId = seg.EndNodeId,
                    width = seg.Width,
                    lanes = seg.Lanes,
                    controlPoints = new List<Vector3>(seg.ControlPoints),
                    laneDefinitions = new List<RoadSegment.LaneDefinition>(seg.LaneDefinitions),
                    sector = seg.Sector
                });
            }

            if (buildingParent != null)
            {
                foreach (Transform child in buildingParent)
                {
                    if (child.GetComponent<BuildingMarker>() == null) continue;
                    var prefabName = child.gameObject.name.Replace("(Clone)", "").Trim();
                    var marker = child.GetComponent<BuildingMarker>();
                    data.buildings.Add(new BuildingData
                    {
                        prefabName = prefabName,
                        position = child.position,
                        rotation = child.rotation,
                        attachedNodeId = marker != null ? marker.attachedNodeId : 0
                    });
                }
            }

            return data;
        }

        private static void ApplyData(RoadNetworkRuntime runtime, Transform buildingParent, RoadNetworkData data, Dictionary<string, GameObject> prefabLookup = null)
        {
            // Clear views and data
            runtime.ClearAllViews();
            runtime.ResetNetwork();

            // Nodes with preserved IDs
            foreach (var n in data.nodes)
            {
                runtime.Network.AddNodeWithId(n.id, n.position, n.sector, n.role);
            }

            // Segments with preserved IDs
            foreach (var s in data.segments)
            {
                var seg = runtime.Network.AddSegmentWithId(s.id, s.startNodeId, s.endNodeId, new List<Vector3>(s.controlPoints), s.width, s.lanes, s.sector);
                seg.SetLaneDefinitions(new List<RoadSegment.LaneDefinition>(s.laneDefinitions));
                runtime.CreateOrUpdateView(seg);
            }

            runtime.CreateOrUpdateIntersectionViews();

            // Buildings: requires prefab lookup; here we try Resources.Load by name
            if (buildingParent != null)
            {
                foreach (Transform child in buildingParent)
                {
                    // Only clear previously spawned buildings so road/intersection views survive.
                    if (child.GetComponent<BuildingMarker>() != null)
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
            foreach (var b in data.buildings)
            {
                GameObject prefab = null;
                if (prefabLookup != null && prefabLookup.TryGetValue(b.prefabName, out var regPrefab))
                {
                    prefab = regPrefab;
                }
                else
                {
                    prefab = Resources.Load<GameObject>(b.prefabName);
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"Building prefab '{b.prefabName}' not found in Resources. Skipping.");
                    continue;
                }
                Transform parent = buildingParent != null ? buildingParent : runtime.transform;
                var inst = GameObject.Instantiate(prefab, b.position, b.rotation, parent);
                var marker = inst.GetComponent<BuildingMarker>();
                if (marker == null)
                {
                    marker = inst.AddComponent<BuildingMarker>();
                }
                marker.attachedNodeId = b.attachedNodeId;
            }
        }
    }
}

