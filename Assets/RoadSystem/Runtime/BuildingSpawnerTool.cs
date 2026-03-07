using System;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Place building prefabs in the scene and mark nearby road nodes with entry/exit roles.
    /// </summary>
    public class BuildingSpawnerTool : MonoBehaviour, ITool
    {
        [SerializeField] private RoadNetworkRuntime runtime;
        [SerializeField] private float snapRadius = 2f;
        [SerializeField] private float drivewayInset = 1.5f;
        [SerializeField] private float drivewayWidth = 4f;
        [SerializeField] private int drivewayLanes = 1;
        [SerializeField] private float minDrivewayLength = 1f;
        [SerializeField] private bool splitRoadForDriveway = false;
        [SerializeField] private Transform buildingParent; // Optional parent for spawned buildings (defaults to runtime transform)
        [SerializeField] private LayerMask placementMask = ~0;
        [SerializeField] private BuildingDefinition[] spawnableBuildings; // ScriptableObjects hold prefab + roles; we instantiate their prefab here.
        [SerializeField] private int selectedDefinitionIndex = 0;
        [SerializeField] private bool alignToSurfaceNormal = true;
        [SerializeField] private int scrollSteps = 0; // Tracks cumulative scroll (+up/-down) for debugging.

        public string ToolName => "BuildingBuild";

        private Camera _cam;
        private bool placingBuilding = false;
        private GameObject _previewInstance;
        private Vector3 _previewNormal = Vector3.up;

        // Cache the main camera reference on load.
        private void Awake()
        {
            _cam = Camera.main;
        }

        // Main input loop: handles selection cycling, placement, and cancel actions.
        private void Update()
        {
            if (runtime == null || runtime.Network == null) return; // Ensure network exists.
            if (_cam == null) _cam = Camera.main; // Refresh camera if needed.

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > Mathf.Epsilon && spawnableBuildings != null && spawnableBuildings.Length > 0)
            {
                selectedDefinitionIndex += scroll > 0 ? 1 : -1; // Scroll cycles building selection.
                scrollSteps += scroll > 0 ? 1 : -1; // Track total scroll direction changes.
                selectedDefinitionIndex = Mathf.Clamp(selectedDefinitionIndex, 0, spawnableBuildings.Length - 1); // Stay in bounds.
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement(); // Right-click/Escape cancels.
                return;
            }
            if (placingBuilding)
            {
                if (!TryGetPlacementPoint(out var hitPoint, out var hitNormal)) return;
                UpdatePreview(hitPoint, hitNormal);
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (!TryGetPlacementPoint(out var hitPoint, out var hitNormal)) return; // Raycast failed; skip.
                if(!placingBuilding)
                {
                    BeginPlacement(hitPoint, hitNormal); // Create preview on first click.
                }
                else
                {
                    PlaceBuilding(_previewInstance.transform.position, hitNormal); // Place selected building at hit.
                }
            }
        }

        // Instantiate the selected building prefab and tag the nearest node with entry/exit roles.
        private void PlaceBuilding(Vector3 position, Vector3 surfaceNormal)
        {
            var definition = GetSelectedDefinition(); // Grab current building definition (prefab + roles).
            if (definition == null)
            {
                throw new InvalidOperationException("No BuildingDefinition is selected; cannot place building.");
            }
            if (definition.prefab == null)
            {
                throw new InvalidOperationException($"BuildingDefinition '{definition.name}' is missing a prefab.");
            }
            if (definition.role == EntryExitType.None)
            {
                throw new InvalidOperationException($"BuildingDefinition '{definition.name}' must define an entry/exit role before placement.");
            }

            // Use preview rotation if available to avoid any prefab-baked offsets drifting placement.
            var rotation = _previewInstance != null ? _previewInstance.transform.rotation : definition.prefab.transform.rotation;
            Transform parent = buildingParent != null ? buildingParent : (runtime != null ? runtime.transform : null);
            var instance = Instantiate(definition.prefab, position, rotation, parent); // Spawn the building in the scene.
            var marker = instance.GetComponent<BuildingMarker>() ?? instance.AddComponent<BuildingMarker>();

            var node = FindOrCreateNode(position); // Snap/create road node nearby.
            ApplyRole(node, definition.role); // Mark node with entry/exit role from definition.
            ConnectToNearestRoad(node);
            marker.attachedNodeId = node.Id;

            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
            }
            placingBuilding = false;
        }

        // Reset temporary state.
        private void CancelPlacement()
        {
            placingBuilding = false;
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
            }
        }

        /// <summary>
        /// Cancel placement and hide the preview. Call when disabling the tool.
        /// </summary>
        public void CancelBuild()
        {
            CancelPlacement();
        }

        // Auto-cancel if the tool component is disabled.
        private void OnDisable()
        {
            CancelPlacement();
        }

        // Create a preview instance of the selected building.
        private void BeginPlacement(Vector3 hitPoint, Vector3 hitNormal)
        {
            var definition = GetSelectedDefinition();
            if (definition == null || definition.prefab == null) return;

            _previewInstance = Instantiate(definition.prefab);
            _previewInstance.transform.position = hitPoint;
            _previewInstance.AddComponent<FloatBob>(); // Add simple hover.
            _previewInstance.name = $"{definition.prefab.name}_Preview";
            SetPreviewLayer(_previewInstance, LayerMask.NameToLayer("Ignore Raycast")); // Keep preview out of raycasts.
            placingBuilding = true;
        }

        // Move and orient the preview to follow the cursor hit point.
        private void UpdatePreview(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!placingBuilding || _previewInstance == null) return;


            Vector3 toTarget = hitPoint - _previewInstance.transform.position;
            toTarget.y = 0f; // Only rotate on the horizontal plane.
            if (toTarget.sqrMagnitude < 0.0001f) return; // Avoid zero-length look vector.
            Quaternion yaw = Quaternion.LookRotation(toTarget.normalized, alignToSurfaceNormal ? hitNormal : Vector3.up);
            _previewInstance.transform.rotation = yaw;
            _previewNormal = hitNormal;

            var definition = GetSelectedDefinition();
            if (definition == null || definition.prefab == null) return;
        }

        // Snap to an existing nearby node or create a new one at the given position.
        private RoadNode FindOrCreateNode(Vector3 position)
        {
            RoadNode closest = null; // Best candidate within snap radius.
            float closestDist = float.MaxValue; // Track nearest distance.
            foreach (var node in runtime.Network.Nodes.Values)
            {
                float d = Vector3.Distance(node.Position, position); // Distance to existing node.
                if (d < closestDist && d <= snapRadius) // Within snap radius and closer than previous.
                {
                    closestDist = d;
                    closest = node;
                }
            }

            if (closest != null)
            {
                return closest; // Snap to existing node.
            }

            return runtime.Network.AddNode(position, Vector2Int.zero); // Otherwise create a new node.
        }

        // Raycast the mouse position to find a valid placement point in world space.
        private bool TryGetPlacementPoint(out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.up;
            if (_cam == null) return false;

            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, placementMask))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }

            // Fallback to y=0 plane if nothing hit (e.g., empty scene)
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0)
            {
                point = ray.origin + ray.direction * t;
                normal = Vector3.up;
                return true;
            }

            return false;
        }

        // ITool hook: enable the component when activated.
        public void OnToolActivated()
        {
            enabled = true;
        }

        // ITool hook: cancel any placement and disable when deactivated.
        public void OnToolDeactivated()
        {
            CancelPlacement();
            enabled = false;
        }

        private BuildingDefinition GetSelectedDefinition() => GetSelectedDefinition(selectedDefinitionIndex); // Default to current selection.

        private BuildingDefinition GetSelectedDefinition(int selection)
        {
            if (spawnableBuildings == null || spawnableBuildings.Length == 0) return null; // Nothing assigned.
            int clamped = Mathf.Clamp(selection, 0, spawnableBuildings.Length - 1); // Clamp requested index.
            selectedDefinitionIndex = clamped; // Keep inspector/state in sync.
            return spawnableBuildings[clamped]; // Return chosen definition.
        }

        private void ApplyRole(RoadNode node, EntryExitType role)
        {
            if (node == null || role == EntryExitType.None) return;

            if ((role & EntryExitType.Entry) != 0)
            {
                runtime.Network.AddEntryPoint(node.Id);
            }

            if ((role & EntryExitType.Exit) != 0)
            {
                runtime.Network.AddExitPoint(node.Id);
            }
        }

        private void SetPreviewLayer(GameObject go, int layer)
        {
            if (go == null) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        private void ConnectToNearestRoad(RoadNode buildingNode)
        {
            if (buildingNode == null || runtime == null || runtime.Network == null) return;

            RoadSegment closestSegment = null;
            float closestDist = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            int closestIndex = -1;

            foreach (var seg in runtime.Network.Segments.Values)
            {
                // Measure distance to the segment polyline
                var pts = seg.ControlPoints;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector3 a = pts[i];
                    Vector3 b = pts[i + 1];
                    Vector3 closest = ClosestPointOnSegment(a, b, buildingNode.Position);
                    float d = Vector3.Distance(buildingNode.Position, closest);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestPoint = closest;
                        closestSegment = seg;
                        closestIndex = i;
                    }
                }
            }

            if (closestSegment == null)
            {
                return;
            }

            RoadNode targetNode;
            if (splitRoadForDriveway)
            {
                var splitNode = InsertNodeOnSegment(closestSegment, closestPoint, closestIndex);
                if (splitNode == null) return;
                targetNode = splitNode;
            }
            else
            {
                // Without splitting, just connect to the nearer endpoint of the closest segment
                float distStart = Vector3.Distance(closestPoint, closestSegment.ControlPoints[0]);
                float distEnd = Vector3.Distance(closestPoint, closestSegment.ControlPoints[closestSegment.ControlPoints.Count - 1]);
                int nodeId = distStart <= distEnd ? closestSegment.StartNodeId : closestSegment.EndNodeId;
                if (!runtime.Network.TryGetNode(nodeId, out targetNode)) return;

            }

            // Create a driveway from building to the target node, inset to avoid overlap
            Vector3 dir = (targetNode.Position - buildingNode.Position);
            float length = dir.magnitude;
            if (length < Mathf.Epsilon) return;
            dir /= length;
            Vector3 start = buildingNode.Position;
            Vector3 end = targetNode.Position - dir * drivewayInset;

            // Guarantee a minimum driveway length so agents get a usable segment even when snapped close to a node.
            if (Vector3.Distance(start, end) < minDrivewayLength)
            {
                end = start + dir * minDrivewayLength;
            }

            var controlPoints = new System.Collections.Generic.List<Vector3> { start, end };
            var driveway = runtime.Network.AddSegment(buildingNode.Id, targetNode.Id, controlPoints, drivewayWidth, drivewayLanes, Vector2Int.zero);
            // Make driveway bidirectional by default when only one lane was requested.
            if (drivewayLanes <= 1)
            {
                driveway.SetLaneDefinitions(BuildBidirectionalLanes(drivewayWidth));
            }
            runtime.CreateOrUpdateView(driveway);
            runtime.CreateOrUpdateIntersectionViews();
        }

        private RoadNode InsertNodeOnSegment(RoadSegment segment, Vector3 point, int segmentIndex)
        {
            if (segment == null) return null;
            var newNode = runtime.Network.SplitSegment(segment.Id, point, segmentIndex, seg =>
            {
                runtime.CreateOrUpdateView(seg);
            });
            runtime.CreateOrUpdateIntersectionViews();
            return newNode;
        }

        private System.Collections.Generic.List<RoadSegment.LaneDefinition> BuildBidirectionalLanes(float width)
        {
            var lanes = new System.Collections.Generic.List<RoadSegment.LaneDefinition>();
            float laneWidth = width * 0.5f;
            float offset = laneWidth * 0.5f;
            lanes.Add(new RoadSegment.LaneDefinition { Offset = -offset, Width = laneWidth, Forward = true });
            lanes.Add(new RoadSegment.LaneDefinition { Offset = offset, Width = laneWidth, Forward = false });
            return lanes;
        }

        private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + t * ab;
        }
    }
}
