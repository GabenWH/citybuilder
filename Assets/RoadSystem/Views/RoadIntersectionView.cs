using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RoadIntersectionView : MonoBehaviour
    {
        [SerializeField] private float inset = 2f;
        [SerializeField] private float colliderHeight = 0.1f;
        [SerializeField] private IntersectionType intersectionType = IntersectionType.Star;
        [SerializeField] private bool drawGizmos = false;
        [SerializeField] private Color gizmoColor = Color.magenta;
        [SerializeField] private bool drawConnectorGizmos = true;
        [SerializeField] private Color connectorColorOut = Color.cyan;
        [SerializeField] private Color connectorColorIn = Color.green;
        [SerializeField] private float arrowSize = 0.5f;
        [SerializeField] private float connectorCurveStrength = 0.35f;
        [SerializeField] private int connectorSamples = 8;
        [Header("Debug Data (read-only)")]
        [SerializeField] private int nodeId;
        [SerializeField] private int connectedSegments;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private BoxCollider _collider;
        [SerializeField] private List<Vector3> _ringWorld;
        [SerializeField] private List<LaneConnector> _connectors;

        public int NodeId => nodeId;
        public int ConnectedSegments => connectedSegments;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = _meshFilter.sharedMesh != null ? _meshFilter.sharedMesh : new Mesh();
            _mesh.name = "IntersectionMesh";
            _meshFilter.sharedMesh = _mesh;
            _collider = GetComponent<BoxCollider>();
        }

        public void Apply(RoadMeshData meshData, RoadColliderData colliderData, Vector3 center, List<Vector3> ringWorld)
        {
            _mesh.Clear();
            _mesh.vertices = meshData.Vertices;
            _mesh.triangles = meshData.Triangles;
            _mesh.uv = meshData.UVs;
            _mesh.normals = meshData.Normals;
            _mesh.RecalculateBounds();

            transform.position = center;
            transform.rotation = Quaternion.identity;
            _ringWorld = ringWorld;

            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }

            _collider.center = colliderData.Center;
            _collider.size = colliderData.Size;
            _collider.isTrigger = true;
            _collider.enabled = connectedSegments > 1; // disable collider for non-intersections
            _collider.transform.localRotation = colliderData.Rotation;
        }

        public void ApplyFromNode(RoadNode node, RoadNetwork network, float extraRadius = 0f)
        {
            if (!IntersectionMeshBuilder.TryBuild(node, network, inset, colliderHeight, extraRadius, out var meshData, out var colliderData))
            {
                return;
            }

            // Collect ring in world for gizmos
            var ringWorld = new List<Vector3>();
            for (int i = 1; i < meshData.Vertices.Length; i++)
            {
                ringWorld.Add(meshData.Vertices[i] + node.Position);
            }

            Apply(meshData, colliderData, node.Position, ringWorld);
            nodeId = node.Id;
            connectedSegments = node.Segments.Count;

            var connectors = IntersectionRoutingBuilder.BuildConnectors(node, network, intersectionType, connectorCurveStrength, connectorSamples);
            _connectors = connectors;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || _ringWorld == null || _ringWorld.Count < 3) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < _ringWorld.Count; i++)
            {
                Vector3 a = _ringWorld[i];
                Vector3 b = _ringWorld[(i + 1) % _ringWorld.Count];
                Gizmos.DrawLine(a, b);
            }

            if (drawConnectorGizmos && _connectors != null)
            {
                foreach (var c in _connectors)
                {
                    if (c.Points == null || c.Points.Count < 2) continue;
                    // Outward color at start, inward at end
                    for (int i = 0; i < c.Points.Count - 1; i++)
                    {
                        float t = i / (float)(c.Points.Count - 1);
                        Gizmos.color = Color.Lerp(connectorColorOut, connectorColorIn, t);
                        Gizmos.DrawLine(c.Points[i], c.Points[i + 1]);
                    }
                    // Draw arrow at the end
                    Vector3 end = c.Points[c.Points.Count - 1];
                    Vector3 prev = c.Points[c.Points.Count - 2];
                    Vector3 dir = (end - prev).normalized;
                    Gizmos.color = connectorColorIn;
                    DrawArrow(end, dir, arrowSize);
                }
            }
        }

        private void DrawArrow(Vector3 position, Vector3 direction, float size)
        {
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
            Gizmos.DrawLine(position, position + right * size);
            Gizmos.DrawLine(position, position + left * size);
        }

        public void ToggleConnectorGizmos()
        {
            drawConnectorGizmos = !drawConnectorGizmos;
        }
    }
}
