using UnityEngine;
using UnityEngine.EventSystems;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Right-click to inspect road/intersection lanes and connectors via ContextMenu.
    /// </summary>
    public class LaneInspectorTool : MonoBehaviour, ITool
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ContextMenu contextMenu;
        [SerializeField] private LayerMask hitMask = ~0;

        public string ToolName => "LaneInspector";

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }

        private void HandleRightClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (viewCamera == null) viewCamera = Camera.main;
            var ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, hitMask))
            {
                contextMenu?.Hide();
                return;
            }

            contextMenu?.ClearOptions();

            var segmentView = hit.collider.GetComponentInParent<RoadSegmentView>();
            var intersectionView = hit.collider.GetComponentInParent<RoadIntersectionView>();

            if (segmentView != null)
            {
                contextMenu.AddOption($"Segment {segmentView.SegmentId} | Lanes {segmentView.LaneCount}", () => { });
                contextMenu.AddOption($"Nodes {segmentView.StartNodeId}->{segmentView.EndNodeId}", () => { });
                contextMenu.AddOption("Toggle Lane Gizmos", () =>
                {
                    segmentView.ToggleGizmos();
                });
            }

            if (intersectionView != null)
            {
                contextMenu.AddOption($"Intersection Node {intersectionView.NodeId}", () => { });
                contextMenu.AddOption($"Connected Segments: {intersectionView.ConnectedSegments}", () => { });
                contextMenu.AddOption("Toggle Connector Gizmos", () => { intersectionView.ToggleConnectorGizmos(); });
            }

            if (contextMenu != null)
            {
                contextMenu.Show(Input.mousePosition);
            }
        }

        public void OnToolActivated()
        {
            enabled = true;
            contextMenu?.Hide();
        }

        public void OnToolDeactivated()
        {
            enabled = false;
            contextMenu?.Hide();
        }
    }
}
