using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Runtime road placement: click once to set start (snaps to existing nodes), move for preview, click again to commit.
    /// Cancel with Escape/right-click.
    /// </summary>
    public class RoadBuildTool : MonoBehaviour, ITool
    {
        [SerializeField] private RoadNetworkRuntime runtime;
        [SerializeField] private float defaultWidth = 8f;
        [SerializeField] private int defaultLanes = 2;
        [SerializeField] private float snapRadius = 2f;
        [SerializeField] private float connectionInset = 2f;
        [SerializeField] private float roadCreationOffset = 0.2f;
        [SerializeField] private LayerMask placementMask = ~0;
        [SerializeField] private Material previewMaterial;

        private RoadNode _startNode;
        private Vector3 _startPosition;
        private bool _hasStart;
        public string ToolName => "RoadBuild";

        private GameObject _previewObject;
        private MeshFilter _previewFilter;
        private MeshRenderer _previewRenderer;
        private Mesh _previewMesh;

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (runtime == null || runtime.Network == null) return;
            if (_cam == null) _cam = Camera.main;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            if (_hasStart)
            {
                if (TryGetPlacementPoint(out var current))
                {
                    UpdatePreview(current);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (!TryGetPlacementPoint(out var hitPoint)) return;

                if (!_hasStart)
                {
                    BeginPlacement(hitPoint);
                }
                else
                {
                    CommitSegment(hitPoint);
                }
            }
        }

        private void BeginPlacement(Vector3 startWorld)
        {
            _startNode = FindOrCreateNode(startWorld);
            _startPosition = _startNode.Position;
            _hasStart = true;
            EnsurePreview();
            UpdatePreview(startWorld);
        }

        private void CommitSegment(Vector3 endWorld)
        {
            var endNode = FindOrCreateNode(endWorld);
            if (endNode.Id == _startNode.Id)
            {
                CancelPlacement();
                return;
            }

            var controlPoints = BuildInsetControlPoints(_startNode.Position, endNode.Position);
            var segment = runtime.Network.AddSegment(_startNode.Id, endNode.Id, controlPoints, defaultWidth, defaultLanes, Vector2Int.zero);
            runtime.CreateOrUpdateView(segment);
            runtime.CreateOrUpdateIntersectionViews();

            CancelPlacement();
        }

        private void CancelPlacement()
        {
            _hasStart = false;
            _startNode = null;
            _startPosition = Vector3.zero;
            if (_previewObject != null)
            {
                _previewObject.SetActive(false);
            }
        }

        /// <summary>
        /// Cancel placement and hide the preview. Call when disabling the tool.
        /// </summary>
        public void CancelBuild()
        {
            CancelPlacement();
        }

        private void OnDisable()
        {
            CancelPlacement();
        }

        private RoadNode FindOrCreateNode(Vector3 position)
        {
            RoadNode closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in runtime.Network.Nodes.Values)
            {
                float d = Vector3.Distance(node.Position, position);
                if (d < closestDist && d <= snapRadius)
                {
                    closestDist = d;
                    closest = node;
                }
            }

            if (closest != null)
            {
                return closest;
            }

            return runtime.Network.AddNode(position + new Vector3(0f,roadCreationOffset,0f), Vector2Int.zero);
        }

        private void EnsurePreview()
        {
            if (_previewObject != null) return;

            _previewObject = new GameObject("RoadPreview");
            _previewObject.transform.SetParent(transform, false);
            _previewFilter = _previewObject.AddComponent<MeshFilter>();
            _previewRenderer = _previewObject.AddComponent<MeshRenderer>();
            _previewMesh = new Mesh { name = "RoadPreviewMesh" };
            _previewFilter.sharedMesh = _previewMesh;

            if (previewMaterial != null)
            {
                _previewRenderer.sharedMaterial = previewMaterial;
            }
            else
            {
                _previewRenderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0f, 1f, 1f, 0.35f) };
            }
        }

        private void UpdatePreview(Vector3 endWorld)
        {
            if (_previewObject == null || !_hasStart) return;

            var worldPoints = BuildInsetControlPoints(_startPosition, endWorld);
            Vector3 origin = worldPoints[0];
            var localPoints = new List<Vector3>(worldPoints.Count);
            for (int i = 0; i < worldPoints.Count; i++)
            {
                localPoints.Add(worldPoints[i] - origin);
            }

            if (!RoadMeshBuilder.TryBuildMesh(localPoints, defaultWidth, out var meshData))
            {
                _previewObject.SetActive(false);
                return;
            }

            _previewMesh.Clear();
            _previewMesh.vertices = meshData.Vertices;
            _previewMesh.triangles = meshData.Triangles;
            _previewMesh.uv = meshData.UVs;
            _previewMesh.normals = meshData.Normals;
            _previewMesh.RecalculateBounds();

            _previewObject.transform.position = origin;
            _previewObject.transform.rotation = Quaternion.identity;
            _previewObject.SetActive(true);
        }

        private List<Vector3> BuildInsetControlPoints(Vector3 start, Vector3 end)
        {
            var points = new List<Vector3>(2) { start, end };
            Vector3 dir = (end - start);
            float length = dir.magnitude;
            if (length < Mathf.Epsilon)
            {
                return points;
            }

            dir /= length;
            float inset = Mathf.Clamp(connectionInset, 0f, (length * 0.5f) - 0.05f);
            Vector3 startInset = start + dir * inset;
            Vector3 endInset = end - dir * inset;
            points[0] = startInset;
            points[1] = endInset;
            return points;
        }

        private bool TryGetPlacementPoint(out Vector3 point)
        {
            point = Vector3.zero;
            if (_cam == null) return false;

            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, placementMask))
            {
                point = hit.point;
                return true;
            }

            // Fallback to y=0 plane if nothing hit (e.g., empty scene)
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0)
            {
                point = ray.origin + ray.direction * t;
                return true;
            }

            return false;
        }

        public void OnToolActivated()
        {
            enabled = true;
        }

        public void OnToolDeactivated()
        {
            CancelPlacement();
            enabled = false;
        }
    }
}
