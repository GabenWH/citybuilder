using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Spawns simple agents and assigns them a connector path between two nodes for testing.
    /// </summary>
    public class RoadUserSpawner : MonoBehaviour
    {
        [SerializeField] private RoadNetworkRuntime runtime;
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private BuildingMarker buildingMarker;
        [SerializeField] private int startNodeId;
        [SerializeField] private int endNodeId;
        [SerializeField] private int samplesPerConnector = 8;
        [SerializeField] private bool autoConfigureOnStart = true;
        [SerializeField] private bool useBuildingMarkerNode = true;
        [SerializeField] private bool showNodeGizmos = false;
        [SerializeField] private float gizmoRadius = 0.5f;

        // Exposed for editor/debug tools.
        public int StartNodeIdValue => startNodeId;
        public int EndNodeIdValue => endNodeId;
        public RoadNetwork Network => runtime != null ? runtime.Network : null;

        private void Awake()
        {
            if (runtime == null)
            {
                runtime = FindObjectOfType<RoadNetworkRuntime>();
            }
            if (buildingMarker == null)
            {
                buildingMarker = GetComponent<BuildingMarker>();
            }
        }

        private void Start()
        {
            if (autoConfigureOnStart)
            {
                AutoConfigure();
            }
        }

        /// <summary>
        /// Auto-fill start/end node ids by picking the nearest entry and exit nodes to this spawner.
        /// Call this after the network is built/loaded.
        /// </summary>
        public void AutoConfigure()
        {
            if (runtime == null || runtime.Network == null) return;
            var pos = transform.position;

            // If attached to a building marker, prefer its node as the start.
            if (useBuildingMarkerNode && buildingMarker != null && buildingMarker.attachedNodeId != 0)
            {
                startNodeId = buildingMarker.attachedNodeId;
            }
            else
            {
                startNodeId = FindNearestNode(runtime.Network.EntryNodes, pos, startNodeId);
                if (startNodeId == 0)
                {
                    startNodeId = FindNearestNode(runtime.Network.Nodes.Values, pos, startNodeId);
                }
            }

            // Pick an exit as the destination.
            endNodeId = FindNearestNode(runtime.Network.ExitNodes, pos, endNodeId);

            // If no entry/exit roles are present, fall back to any nodes.
            if (endNodeId == 0) endNodeId = FindNearestNode(runtime.Network.Nodes.Values, pos, endNodeId);
        }

        /// <summary>
        /// Configure the spawner with an explicit building (for start node) and exit node id.
        /// </summary>
        public void Configure(BuildingMarker building, int exitNode)
        {
            buildingMarker = building;
            if (buildingMarker != null && buildingMarker.attachedNodeId != 0)
            {
                startNodeId = buildingMarker.attachedNodeId;
            }
            endNodeId = exitNode;
        }

        /// <summary>
        /// Convenience entry for spawning using a specific building and exit node without mutating serialized fields.
        /// </summary>
        public void SpawnFromBuilding(BuildingMarker building, int exitNode)
        {
            Configure(building, exitNode);
            SpawnAgent();
        }

        private int FindNearestNode(IEnumerable<RoadNode> nodes, Vector3 pos, int current)
        {
            float best = float.MaxValue;
            int bestId = current;
            foreach (var node in nodes)
            {
                float d = Vector3.SqrMagnitude(node.Position - pos);
                if (d < best)
                {
                    best = d;
                    bestId = node.Id;
                }
            }
            return bestId;
        }

        public void SpawnAgent()
        {
            if (runtime == null || runtime.Network == null)
            {
                Debug.LogWarning("RoadUserSpawner missing runtime.");
                return;
            }
            if (agentPrefab == null)
            {
                Debug.LogWarning("RoadUserSpawner missing agent prefab.");
                return;
            }
            if (!runtime.Network.TryGetNode(startNodeId, out var startNode))
            {
                Debug.LogWarning("RoadUserSpawner missing valid start node id. Call AutoConfigure() after loading.");
                return;
            }

            // Always choose a random exit, excluding the start node.
            if (!runtime.Network.TryGetRandomExitNode(new[] { startNodeId }, out var randomExitNode))
            {
                Debug.LogWarning("RoadUserSpawner could not find a valid exit node.");
                return;
            }
            int targetNodeId = randomExitNode.Id;

            // Spawn at the start node position and ask the agent to route itself via the network.
            var go = Instantiate(agentPrefab, startNode.Position, Quaternion.identity);
            if (go.TryGetComponent<RoadUser>(out var roadUser))
            {
                roadUser.RequestRoute(runtime.Network, startNodeId, targetNodeId);
            }
            else if (go.TryGetComponent<SimpleRoadAgent>(out var agent))
            {
                agent.RequestRoute(runtime.Network, startNodeId, targetNodeId); // Fallback if interface not found.
            }
        }

#if UNITY_EDITOR

        private void CenterOnStartNode()
        {
            if (runtime == null || runtime.Network == null) return;
            if (runtime.Network.TryGetNode(startNodeId, out var node))
            {
                UnityEditor.SceneView.lastActiveSceneView.Frame(new Bounds(node.Position, Vector3.one), false);
            }
        }
        private void CenterOnExitNode()
        {
            if (runtime == null || runtime.Network == null) return;
            if (runtime.Network.TryGetNode(endNodeId, out var node))
            {
                UnityEditor.SceneView.lastActiveSceneView.Frame(new Bounds(node.Position, Vector3.one), false);
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            if (!showNodeGizmos || runtime == null || runtime.Network == null) return;

            if (runtime.Network.TryGetNode(startNodeId, out var start))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(start.Position, gizmoRadius);
            }

            if (runtime.Network.TryGetNode(endNodeId, out var end))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(end.Position, gizmoRadius);
            }
        }
    }
}
