using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Simple runtime host that owns a RoadNetwork and spawns views for its segments.
    /// Drop this in a scene with a RoadSegmentView prefab assigned.
    /// </summary>
    public class RoadNetworkRuntime : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private RoadSegmentView roadViewPrefab;
        [SerializeField] private RoadIntersectionView intersectionViewPrefab;
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material intersectionMaterial;
        [SerializeField] private float intersectionExtraRadius = 0f; // TODO: Use to grow cap without moving roads
        [Header("Parents")]
        [SerializeField] private Transform roadParent;

        [Header("Debug Sample")]
        [SerializeField] private bool buildSampleNetworkOnStart = true;
        [SerializeField] private float sampleWidth = 8f;
        [SerializeField] private int sampleLanes = 2;

        private RoadNetwork _network;
        private readonly Dictionary<int, RoadSegmentView> _views = new Dictionary<int, RoadSegmentView>();
        private readonly Dictionary<int, RoadIntersectionView> _intersectionViews = new Dictionary<int, RoadIntersectionView>();

        private void Awake()
        {
            _network = new RoadNetwork();
        }

        private void Start()
        {
            if (buildSampleNetworkOnStart)
            {
                BuildSampleNetwork();
            }
        }

        private void BuildSampleNetwork()
        {
            // Simple “L” shaped sample
            var n0 = _network.AddNode(new Vector3(0, 0, 0), Vector2Int.zero);
            var n1 = _network.AddNode(new Vector3(0, 0, 20), Vector2Int.zero);
            var n2 = _network.AddNode(new Vector3(20, 0, 20), Vector2Int.zero);

            var seg0 = _network.AddSegment(n0.Id, n1.Id, new List<Vector3> { n0.Position, n1.Position }, sampleWidth, sampleLanes, Vector2Int.zero);
            var seg1 = _network.AddSegment(n1.Id, n2.Id, new List<Vector3> { n1.Position, n2.Position }, sampleWidth, sampleLanes, Vector2Int.zero);

            CreateOrUpdateView(seg0);
            CreateOrUpdateView(seg1);
            CreateOrUpdateIntersectionViews();
        }

        public void CreateOrUpdateView(RoadSegment segment)
        {
            if (segment == null) return;
            if (!_views.TryGetValue(segment.Id, out var view))
            {
                if (roadViewPrefab == null)
                {
                    Debug.LogError("Road view prefab not assigned.");
                    return;
                }

                Transform parent = roadParent != null ? roadParent : transform;
                view = Instantiate(roadViewPrefab, parent);
                var renderer = view.GetComponent<MeshRenderer>();
                if (renderer != null && roadMaterial != null)
                {
                    renderer.sharedMaterial = roadMaterial;
                }
                _views[segment.Id] = view;
            }

            view.ApplyFromSegment(segment);
        }

        public void CreateOrUpdateIntersectionViews()
        {
            if (intersectionViewPrefab == null)
            {
                return;
            }

            var toRemove = new List<int>();
            foreach (var kvp in _intersectionViews)
            {
                if (!_network.Nodes.ContainsKey(kvp.Key) || kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (int id in toRemove)
            {
                if (_intersectionViews[id] != null) DestroyImmediate(_intersectionViews[id].gameObject);
                _intersectionViews.Remove(id);
            }

            foreach (var kvp in _network.Nodes)
            {
                var node = kvp.Value;
                if (node.Segments.Count < 2) continue;

                if (!_intersectionViews.TryGetValue(node.Id, out var view) || view == null || view.Equals(null))
                {
                    Transform parent = roadParent != null ? roadParent : transform;
                    view = Instantiate(intersectionViewPrefab, parent);
                    var renderer = view.GetComponent<MeshRenderer>();
                    if (renderer != null && intersectionMaterial != null)
                    {
                        renderer.sharedMaterial = intersectionMaterial;
                    }
                    _intersectionViews[node.Id] = view;
                }

                try
                {
                    view.ApplyFromNode(node, _network, intersectionExtraRadius);
                }
                catch (MissingReferenceException)
                {
                    // Recreate if the view was destroyed externally
                    Transform parent = roadParent != null ? roadParent : transform;
                    var newView = Instantiate(intersectionViewPrefab, parent);
                    var renderer = newView.GetComponent<MeshRenderer>();
                    if (renderer != null && intersectionMaterial != null)
                    {
                        renderer.sharedMaterial = intersectionMaterial;
                    }
                    _intersectionViews[node.Id] = newView;
                    newView.ApplyFromNode(node, _network, intersectionExtraRadius);
                }
            }
        }

        public RoadNetwork Network => _network;

        public void ClearAllViews()
        {
            foreach (var v in _views.Values)
            {
                if (v != null) DestroyImmediate(v.gameObject);
            }
            _views.Clear();

            foreach (var v in _intersectionViews.Values)
            {
                if (v != null) DestroyImmediate(v.gameObject);
            }
            _intersectionViews.Clear();
        }

        public void ResetNetwork()
        {
            _network = new RoadNetwork();
        }
    }
}
