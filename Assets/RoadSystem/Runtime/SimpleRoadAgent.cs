using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Minimal agent that follows a list of path points at a constant speed.
    /// Intended as a placeholder for testing.
    /// </summary>
    public class SimpleRoadAgent : MonoBehaviour, RoadUser
    {
        [SerializeField] private float desiredSpeed = 5f;
        [SerializeField] private float arriveThreshold = 0.1f;
        [SerializeField] private bool loopPath = false;
        [SerializeField] private int startingLaneId = -1;

        [SerializeField] private List<Vector3> _path = new List<Vector3>();
        private int _currentIndex = 0;
        public RoadUserState State { get; private set; }
        public void SetState(RoadUserState state) => State = state;
        public Vector3 Position => transform.position;
        public float DesiredSpeed => desiredSpeed;

        public void RequestRoute(RoadNetwork network, int startNodeId, int endNodeId)
        {
            if (network == null) return;
            var nodePath = network.FindPathAStar(startNodeId, endNodeId); // Get node id path via A*.
            if (nodePath == null || nodePath.Count == 0) return;

            var pts = network.BuildLanePath(nodePath); // Let the network expand to lane/connector path.
            if (pts == null || pts.Count < 2)
            {
                Debug.LogWarning($"SimpleRoadAgent received an unusable path (count={pts?.Count ?? 0}) from {startNodeId} to {endNodeId}.", this);
                return;
            }

            // If the last point is still far from the destination node, append the destination node position.
            if (network.TryGetNode(endNodeId, out var destinationNode))
            {
                float endDist = Vector3.Distance(pts[pts.Count - 1], destinationNode.Position);
                if (endDist > arriveThreshold * 2f)
                {
                    pts.Add(destinationNode.Position);
                }
            }

            if (pts.Count > 0)
            {
                SetRoute(pts);
                if (State == null)
                {
                    State = new RoadUserState(GetInstanceID(), startingLaneId, endNodeId, desiredSpeed);
                }
                State.TargetNodeId = endNodeId;
            }
        }

        public void SetRoute(List<Vector3> pathPoints)
        {
            _path.Clear();
            if (pathPoints != null) _path.AddRange(pathPoints);
            _currentIndex = 0;
            // Reset state path progress
            if (State != null)
            {
                State.LaneT = 0f;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_path.Count == 0 || _currentIndex >= _path.Count) return;
            Vector3 target = _path[_currentIndex];
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;
            if (dist < arriveThreshold)
            {
                _currentIndex++;
                if (loopPath && _currentIndex >= _path.Count)
                {
                    _currentIndex = 0;
                }
                else if (_currentIndex >= _path.Count)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Derive facing from neighboring points to smooth rotation through corners.
            Vector3 dir = toTarget.normalized;
            // Blend using only previous->current direction to avoid oversmoothing when looking ahead.
            Vector3 blendedDir = dir;
            int prevIndex = Mathf.Max(0, _currentIndex - 1);
            Vector3 prev = _path[prevIndex];
            Vector3 curr = _path[_currentIndex];
            Vector3 incoming = (curr - prev).normalized;
            if (incoming.sqrMagnitude > 0.0001f)
            {
                blendedDir = (incoming + dir).normalized;
                if (blendedDir.sqrMagnitude < 0.0001f)
                {
                    blendedDir = dir; // Avoid zero-length blend.
                }
            }

            transform.position += dir * desiredSpeed * deltaTime;
            if (blendedDir.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(blendedDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, deltaTime * 10f);
            }

            // Update state representation
            if (State != null)
            {
                State.Speed = desiredSpeed;
                State.LaneT = Mathf.Clamp01(State.LaneT + (desiredSpeed * deltaTime)); // simplistic progress; real impl should map to lane length
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Awake()
        {
            if (startingLaneId >= 0)
            {
                State = new RoadUserState(GetInstanceID(), startingLaneId, -1, desiredSpeed);
            }
        }
    }
}
