using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RoadSegmentView : MonoBehaviour
    {
        [SerializeField] private float colliderHeight = 0.25f;
        [SerializeField] private List<BoxCollider> colliders = new List<BoxCollider>();
        [Header("Debug Gizmos")]
        [SerializeField] private bool drawGizmos = false;
        [SerializeField] private Color controlPointColor = Color.cyan;
        [SerializeField] private Color endPointColor = Color.yellow;
        [SerializeField] private Color segmentLineColor = Color.green;
        [SerializeField] private float pointSize = 0.25f;
        [SerializeField] private Color laneLineColor = Color.white;
        [SerializeField] private float lanePointSize = 0.1f;
        [SerializeField] private Color laneStartColor = Color.blue;
        [SerializeField] private Color laneEndColor = Color.red;
        [Header("Lane Markings")]
        [SerializeField] private bool showLaneMarkings = false;
        [SerializeField] private float laneMarkingWidth = 0.1f;
        [SerializeField] private Material laneMarkingMaterial;
        [Header("Debug Data (read-only)")]
        [SerializeField] private int startNodeId;
        [SerializeField] private int endNodeId;
        [SerializeField] private float width;
        [SerializeField] private int lanes;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        // Serialized so gizmos still draw in edit mode after play mode is stopped.
        [SerializeField] private List<Vector3> _controlPointsWorld;
        [SerializeField] private int _segmentId = -1;
        [SerializeField] private List<List<Vector3>> _laneCenterlinesWorld;
        private readonly List<Mesh> _laneMeshes = new List<Mesh>();
        private readonly List<MeshFilter> _laneFilters = new List<MeshFilter>();
        private readonly List<MeshRenderer> _laneRenderers = new List<MeshRenderer>();

        public int SegmentId => _segmentId;
        public int StartNodeId => startNodeId;
        public int EndNodeId => endNodeId;
        public int LaneCount => _laneCenterlinesWorld != null ? _laneCenterlinesWorld.Count : 0;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = _meshFilter.sharedMesh != null ? _meshFilter.sharedMesh : new Mesh();
            _mesh.name = "RoadSegmentMesh";
            _meshFilter.sharedMesh = _mesh;
        }

        public void Apply(RoadMeshData meshData, IReadOnlyList<RoadColliderData> colliderData)
        {
            if (meshData.Vertices == null || meshData.Vertices.Length == 0)
            {
                return;
            }

            _mesh.Clear();
            _mesh.vertices = meshData.Vertices;
            _mesh.triangles = meshData.Triangles;
            _mesh.uv = meshData.UVs;
            _mesh.normals = meshData.Normals;
            _mesh.RecalculateBounds();

            RebuildColliders(colliderData);
        }

        public void ApplyFromSegment(RoadSegment segment)
        {
            if (!RoadMeshBuilder.TryBuildMesh(segment, out var meshData))
            {
                return;
            }

            // Position the view at the first control point so mesh vertices are local to it.
            var points = segment.ControlPoints;
            Vector3 origin = points[0];
            var localPoints = new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                localPoints.Add(points[i] - origin);
            }

            if (!RoadMeshBuilder.TryBuildMesh(localPoints, segment.Width, out meshData))
            {
                return;
            }

            transform.position = origin;
            transform.rotation = Quaternion.identity;

            var colliderData = RoadMeshBuilder.BuildColliders(localPoints, segment.Width, colliderHeight);
            Apply(meshData, colliderData);
            _controlPointsWorld = new List<Vector3>(points);
            _segmentId = segment.Id;
            startNodeId = segment.StartNodeId;
            endNodeId = segment.EndNodeId;
            width = segment.Width;
            lanes = segment.LaneDefinitions != null ? segment.LaneDefinitions.Count : segment.Lanes;
            _laneCenterlinesWorld = new List<List<Vector3>>(segment.LaneCenterlines.Count);
            foreach (var lane in segment.LaneCenterlines)
            {
                _laneCenterlinesWorld.Add(new List<Vector3>(lane));
            }
            BuildLaneMarkings(segment);
        }

        private void RebuildColliders(IReadOnlyList<RoadColliderData> colliderData)
        {
            foreach (var existing in colliders)
            {
                if (existing != null)
                {
                    Destroy(existing.gameObject);
                }
            }

            colliders.Clear();

            if (colliderData == null)
            {
                return;
            }

            for (int i = 0; i < colliderData.Count; i++)
            {
                var data = colliderData[i];
                GameObject child = new GameObject($"RoadCollider_{i}");
                child.transform.SetParent(transform, false);
                child.transform.localPosition = data.Center;
                child.transform.localRotation = data.Rotation;
                var box = child.AddComponent<BoxCollider>();
                box.size = data.Size;
                colliders.Add(box);
            }
        }

        private void BuildLaneMarkings(RoadSegment segment)
        {
            // Clear existing lane meshes
            for (int i = 0; i < _laneFilters.Count; i++)
            {
                if (_laneFilters[i] != null)
                {
                    DestroyImmediate(_laneFilters[i].gameObject);
                }
            }
            _laneFilters.Clear();
            _laneRenderers.Clear();
            _laneMeshes.Clear();

            if (!showLaneMarkings || laneMarkingWidth <= 0f || laneMarkingMaterial == null)
            {
                return;
            }

            if (segment.LaneCenterlines == null || segment.LaneCenterlines.Count == 0)
            {
                return;
            }

            foreach (var lane in segment.LaneCenterlines)
            {
                // Build marking in local space relative to the view's origin
                var localCenterline = new List<Vector3>(lane.Count);
                Vector3 origin = _controlPointsWorld != null && _controlPointsWorld.Count > 0 ? _controlPointsWorld[0] : Vector3.zero;
                for (int i = 0; i < lane.Count; i++)
                {
                    localCenterline.Add(lane[i] - origin);
                }

                if (!LaneMarkingBuilder.TryBuildMarking(localCenterline, laneMarkingWidth, out var markingMesh))
                {
                    continue;
                }

                var child = new GameObject("LaneMarking");
                child.transform.SetParent(transform, false);
                var mf = child.AddComponent<MeshFilter>();
                var mr = child.AddComponent<MeshRenderer>();
                mf.sharedMesh = new Mesh { name = "LaneMarkingMesh" };
                mf.sharedMesh.vertices = markingMesh.Vertices;
                mf.sharedMesh.triangles = markingMesh.Triangles;
                mf.sharedMesh.uv = markingMesh.UVs;
                mf.sharedMesh.normals = markingMesh.Normals;
                mf.sharedMesh.RecalculateBounds();

                mr.sharedMaterial = laneMarkingMaterial;

                _laneFilters.Add(mf);
                _laneRenderers.Add(mr);
                _laneMeshes.Add(mf.sharedMesh);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || _controlPointsWorld == null || _controlPointsWorld.Count < 2)
            {
                return;
            }

            Gizmos.color = segmentLineColor;
            for (int i = 0; i < _controlPointsWorld.Count - 1; i++)
            {
                Gizmos.DrawLine(_controlPointsWorld[i], _controlPointsWorld[i + 1]);
            }

            for (int i = 0; i < _controlPointsWorld.Count; i++)
            {
                bool isEnd = i == 0 || i == _controlPointsWorld.Count - 1;
                Gizmos.color = isEnd ? endPointColor : controlPointColor;
                Gizmos.DrawSphere(_controlPointsWorld[i], pointSize);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(_controlPointsWorld[i], isEnd ? $"End {i}" : $"P{i}");
#endif
            }

#if UNITY_EDITOR
            if (_segmentId >= 0)
            {
                Vector3 mid = _controlPointsWorld[_controlPointsWorld.Count / 2];
                UnityEditor.Handles.Label(mid + Vector3.up * (pointSize * 1.5f), $"Seg {_segmentId}");
            }
#endif
            if (_laneCenterlinesWorld != null && _laneCenterlinesWorld.Count > 0)
            {
                Gizmos.color = laneLineColor;
                foreach (var lane in _laneCenterlinesWorld)
                {
                    if (lane == null || lane.Count < 2) continue;
                    for (int i = 0; i < lane.Count - 1; i++)
                    {
                        Gizmos.DrawLine(lane[i], lane[i + 1]);
                    }
                    for (int i = 0; i < lane.Count; i++)
                    {
                        bool start = i == 0;
                        bool end = i == lane.Count - 1;
                        Gizmos.color = start ? laneStartColor : end ? laneEndColor : laneLineColor;
                        Gizmos.DrawSphere(lane[i], lanePointSize);
                        Gizmos.color = laneLineColor;
                    }
                }
            }
        }

        public void ToggleGizmos()
        {
            drawGizmos = !drawGizmos;
        }

        private void OnDrawGizmosSelected()
        {
            // Mirror gizmo rendering when the object is selected in Game view (Gizmos enabled).
            if (drawGizmos)
            {
                OnDrawGizmos();
            }
        }
    }
}
