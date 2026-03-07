using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityBuilder.Roads
{
    public enum EntryExitType
    {
        None = 0,
        Entry = 1 << 0,
        Exit = 1 << 1,
        Both = Entry | Exit
    }

    /// <summary>
    /// Pure data representation of a road node. No scene or GameObject concerns.
    /// </summary>
    public class RoadNode
    {
        public int Id { get; }
        public Vector3 Position { get; private set; }
        public Vector2Int Sector { get; private set; }
        public EntryExitType Role { get; private set; } = EntryExitType.None;
        private readonly HashSet<int> _segments = new HashSet<int>();

        public IReadOnlyCollection<int> Segments => _segments;

        public RoadNode(int id, Vector3 position, Vector2Int sector)
        {
            Id = id;
            Position = position;
            Sector = sector;
        }

        public void SetPosition(Vector3 position) => Position = position;

        public void SetSector(Vector2Int sector) => Sector = sector;

        public void AddSegment(int segmentId) => _segments.Add(segmentId);

        public void RemoveSegment(int segmentId) => _segments.Remove(segmentId);

        public void SetRole(EntryExitType role) => Role = role;

        public bool HasRole(EntryExitType role) => (Role & role) != 0;
    }

    /// <summary>
    /// Pure data representation of a road segment between two nodes.
    /// </summary>
    public class RoadSegment
    {
        // Allows customization of default lane direction based on offset. If null, lanes with offset <= 0 are forward.
        public static Func<float, bool> DefaultLaneDirectionResolver = offset => offset <= 0f;

        [Serializable]
        public class LaneDefinition
        {
            public float Offset; // Offset from centerline (left positive)
            public float Width;
            public bool Forward = true;
            public string Type;
        }

        public int Id { get; }
        public int StartNodeId { get; }
        public int EndNodeId { get; }
        public float Width { get; private set; }
        public int Lanes { get; private set; }
        public List<Vector3> ControlPoints { get; private set; }
        public Vector2Int Sector { get; private set; }
        public List<LaneDefinition> LaneDefinitions { get; private set; } = new List<LaneDefinition>();
        public readonly List<List<Vector3>> LaneCenterlines = new List<List<Vector3>>();

        public RoadSegment(int id, int startNodeId, int endNodeId, List<Vector3> controlPoints, float width, int lanes, Vector2Int sector)
        {
            Id = id;
            StartNodeId = startNodeId;
            EndNodeId = endNodeId;
            ControlPoints = controlPoints ?? throw new ArgumentNullException(nameof(controlPoints));
            Width = width;
            Lanes = Mathf.Max(1, lanes);
            Sector = sector;
            EnsureLaneDefinitions();
            RebuildLanes();
        }

        public void SetControlPoints(List<Vector3> controlPoints)
        {
            ControlPoints = controlPoints ?? throw new ArgumentNullException(nameof(controlPoints));
            EnsureLaneDefinitions();
            RebuildLanes();
        }

        public void SetWidth(float width)
        {
            Width = Mathf.Max(0.01f, width);
            EnsureLaneDefinitions();
            RebuildLanes();
        }

        public void SetLanes(int lanes)
        {
            Lanes = Mathf.Max(1, lanes);
            EnsureLaneDefinitions();
            RebuildLanes();
        }

        public void SetSector(Vector2Int sector)
        {
            Sector = sector;
        }

        public void SetLaneDefinitions(List<LaneDefinition> definitions)
        {
            LaneDefinitions = definitions ?? new List<LaneDefinition>();
            Lanes = Mathf.Max(1, LaneDefinitions.Count);
            RebuildLanes();
        }

        private void RebuildLanes()
        {
            LaneCenterlines.Clear();
            if (ControlPoints == null || ControlPoints.Count < 2 || Lanes <= 0)
            {
                return;
            }

            for (int i = 0; i < LaneDefinitions.Count; i++)
            {
                LaneCenterlines.Add(OffsetPolyline(ControlPoints, LaneDefinitions[i].Offset));
            }
        }

        private void EnsureLaneDefinitions()
        {
            if (LaneDefinitions == null) LaneDefinitions = new List<LaneDefinition>();
            LaneDefinitions.Clear();
            float laneWidth = Width / Lanes;
            float offsetStart = -Width * 0.5f + laneWidth * 0.5f;
            for (int i = 0; i < Lanes; i++)
            {
                float offset = offsetStart + i * laneWidth;
                bool forward = DefaultLaneDirectionResolver != null ? DefaultLaneDirectionResolver(offset) : offset <= 0f;
                LaneDefinitions.Add(new LaneDefinition
                {
                    Offset = offset,
                    Width = laneWidth,
                    Forward = forward,
                    Type = null,
                });
            }
        }

        private static List<Vector3> OffsetPolyline(IReadOnlyList<Vector3> points, float offset)
        {
            var result = new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward;
                if (i == 0)
                {
                    forward = (points[i + 1] - points[i]).normalized;
                }
                else if (i == points.Count - 1)
                {
                    forward = (points[i] - points[i - 1]).normalized;
                }
                else
                {
                    forward = ((points[i] - points[i - 1]) + (points[i + 1] - points[i])).normalized;
                }

                Vector3 left = new Vector3(-forward.z, 0f, forward.x).normalized;
                result.Add(points[i] + left * offset);
            }

            return result;
        }
    }

    /// <summary>
    /// Manages nodes and segments as pure data. Gameplay and topology live here; views and meshes listen in.
    /// </summary>
    public class RoadNetwork
    {
        private readonly Dictionary<int, RoadNode> _nodes = new Dictionary<int, RoadNode>();
        private readonly Dictionary<int, RoadSegment> _segments = new Dictionary<int, RoadSegment>();
        private int _nextNodeId = 1;
        private int _nextSegmentId = 1;

        public IReadOnlyDictionary<int, RoadNode> Nodes => _nodes;
        public IReadOnlyDictionary<int, RoadSegment> Segments => _segments;
        public IEnumerable<RoadNode> EntryNodes => _nodes.Values.Where(n => n.HasRole(EntryExitType.Entry));
        public IEnumerable<RoadNode> ExitNodes => _nodes.Values.Where(n => n.HasRole(EntryExitType.Exit));

        public bool TryGetRandomExitNode(out RoadNode node) => TryGetRandomExitNode(Array.Empty<int>(), out node);

        public bool TryGetRandomExitNode(IEnumerable<int> excludeNodeIds, out RoadNode node)
        {
            var excluded = excludeNodeIds != null ? new HashSet<int>(excludeNodeIds) : new HashSet<int>();
            var exits = ExitNodes.Where(n => !excluded.Contains(n.Id)).ToList();
            if (exits.Count == 0)
            {
                node = null;
                return false;
            }

            node = exits[UnityEngine.Random.Range(0, exits.Count)];
            return true;
        }

        public RoadNode AddNode(Vector3 position, Vector2Int sector) => AddNodeWithId(_nextNodeId++, position, sector, EntryExitType.None);

        public RoadNode AddNodeWithId(int id, Vector3 position, Vector2Int sector, EntryExitType role = EntryExitType.None)
        {
            if (id >= _nextNodeId) _nextNodeId = id + 1;
            var node = new RoadNode(id, position, sector);
            node.SetRole(role);
            _nodes[id] = node;
            return node;
        }

        public bool RemoveNode(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                return false;
            }

            foreach (int segmentId in new List<int>(node.Segments))
            {
                RemoveSegment(segmentId);
            }

            _nodes.Remove(nodeId);
            return true;
        }

        public RoadSegment AddSegment(int startNodeId, int endNodeId, List<Vector3> controlPoints, float width, int lanes, Vector2Int sector)
            => AddSegmentWithId(_nextSegmentId++, startNodeId, endNodeId, controlPoints, width, lanes, sector);

        public RoadSegment AddSegmentWithId(int id, int startNodeId, int endNodeId, List<Vector3> controlPoints, float width, int lanes, Vector2Int sector)
        {
            if (!_nodes.ContainsKey(startNodeId))
            {
                throw new ArgumentException($"Missing start node {startNodeId}", nameof(startNodeId));
            }

            if (!_nodes.ContainsKey(endNodeId))
            {
                throw new ArgumentException($"Missing end node {endNodeId}", nameof(endNodeId));
            }

            if (controlPoints == null || controlPoints.Count < 2)
            {
                throw new ArgumentException("Segments need at least 2 control points", nameof(controlPoints));
            }

            if (id >= _nextSegmentId) _nextSegmentId = id + 1;

            var segment = new RoadSegment(id, startNodeId, endNodeId, new List<Vector3>(controlPoints), width, lanes, sector);
            _segments[id] = segment;
            _nodes[startNodeId].AddSegment(id);
            _nodes[endNodeId].AddSegment(id);
            return segment;
        }

        public bool RemoveSegment(int segmentId)
        {
            if (!_segments.TryGetValue(segmentId, out var segment))
            {
                return false;
            }

            if (_nodes.TryGetValue(segment.StartNodeId, out var start))
            {
                start.RemoveSegment(segmentId);
            }

            if (_nodes.TryGetValue(segment.EndNodeId, out var end))
            {
                end.RemoveSegment(segmentId);
            }

            _segments.Remove(segmentId);
            return true;
        }

        public bool TryGetNode(int nodeId, out RoadNode node) => _nodes.TryGetValue(nodeId, out node);

        public bool TryGetSegment(int segmentId, out RoadSegment segment) => _segments.TryGetValue(segmentId, out segment);

        /// <summary>
        /// Finds a drivable path between two nodes using A* search (respecting lane directions).
        /// Returns a list of node ids from start to end (empty if no path).
        /// </summary>
        public List<int> FindPathAStar(int startNodeId, int endNodeId)
        {
            // Early outs for identical nodes or invalid inputs.
            if (startNodeId == endNodeId) return new List<int> { startNodeId };
            if (!_nodes.ContainsKey(startNodeId) || !_nodes.ContainsKey(endNodeId))
            {
                return new List<int>();
            }

            // A* bookkeeping: open set, backpointers, and scores.
            var openSet = new HashSet<int> { startNodeId };
            var openList = new List<int> { startNodeId };
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [startNodeId] = 0f };
            var fScore = new Dictionary<int, float> { [startNodeId] = HeuristicCost(startNodeId, endNodeId) };

            while (openList.Count > 0)
            {
                // Select node with the lowest estimated total cost.
                int current = GetLowestFScore(openList, fScore);
                if (current == endNodeId)
                {
                    return ReconstructPath(cameFrom, current);
                }

                // Consume current and examine neighbors.
                openList.Remove(current);
                openSet.Remove(current);

                foreach (var (neighbor, cost) in GetReachableNeighbors(current))
                {
                    // Proposed better path to neighbor via current.
                    float tentativeG = gScore[current] + cost;
                    if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + HeuristicCost(neighbor, endNodeId);
                        if (!openSet.Contains(neighbor))
                        {
                            // Discover new node for evaluation.
                            openSet.Add(neighbor);
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            return new List<int>();
        }

        public void AddEntryPoint(int nodeId) => UpdateEntryExitRole(nodeId, EntryExitType.Entry, true);

        public void RemoveEntryPoint(int nodeId) => UpdateEntryExitRole(nodeId, EntryExitType.Entry, false);

        public void AddExitPoint(int nodeId) => UpdateEntryExitRole(nodeId, EntryExitType.Exit, true);

        public void RemoveExitPoint(int nodeId) => UpdateEntryExitRole(nodeId, EntryExitType.Exit, false);

        public void ClearEntryExitRoles(int nodeId) => SetEntryExitRole(nodeId, EntryExitType.None);

        public void SetEntryExitRole(int nodeId, EntryExitType role)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                throw new ArgumentException($"Missing node {nodeId}", nameof(nodeId));
            }

            node.SetRole(role);
        }

        private void UpdateEntryExitRole(int nodeId, EntryExitType role, bool add)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                throw new ArgumentException($"Missing node {nodeId}", nameof(nodeId));
            }

            var current = node.Role;
            var updated = add ? current | role : current & ~role;
            node.SetRole(updated);
        }

        /// <summary>
        /// Splits a segment at the given point (on the polyline between control point indices segmentIndex and segmentIndex+1).
        /// Removes the original segment, inserts a new node at the split point, and creates two new segments with cloned lane definitions.
        /// Returns the new node.
        /// </summary>
        public RoadNode SplitSegment(int segmentId, Vector3 point, int segmentIndex, System.Action<RoadSegment> onSegmentCreated = null)
        {
            if (!_segments.TryGetValue(segmentId, out var segment))
            {
                throw new ArgumentException($"Missing segment {segmentId}", nameof(segmentId));
            }
            if (segment.ControlPoints == null || segment.ControlPoints.Count < 2)
            {
                throw new ArgumentException($"Segment {segmentId} has insufficient control points to split");
            }
            if (segmentIndex < 0 || segmentIndex >= segment.ControlPoints.Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentIndex), "segmentIndex must point to a valid segment between control points");
            }

            var cp = segment.ControlPoints;
            var first = new List<Vector3>();
            var second = new List<Vector3>();
            for (int i = 0; i <= segmentIndex; i++) first.Add(cp[i]);
            first.Add(point);
            second.Add(point);
            for (int i = segmentIndex + 1; i < cp.Count; i++) second.Add(cp[i]);

            var newNode = AddNode(point, segment.Sector);

            RemoveSegment(segment.Id);

            var segA = AddSegment(segment.StartNodeId, newNode.Id, first, segment.Width, segment.Lanes, segment.Sector);
            segA.SetLaneDefinitions(CloneLaneDefinitions(segment.LaneDefinitions));
            onSegmentCreated?.Invoke(segA);

            var segB = AddSegment(newNode.Id, segment.EndNodeId, second, segment.Width, segment.Lanes, segment.Sector);
            segB.SetLaneDefinitions(CloneLaneDefinitions(segment.LaneDefinitions));
            onSegmentCreated?.Invoke(segB);

            return newNode;
        }

        private List<RoadSegment.LaneDefinition> CloneLaneDefinitions(List<RoadSegment.LaneDefinition> src)
        {
            var list = new List<RoadSegment.LaneDefinition>();
            if (src == null) return list;
            foreach (var lane in src)
            {
                list.Add(new RoadSegment.LaneDefinition
                {
                    Offset = lane.Offset,
                    Width = lane.Width,
                    Forward = lane.Forward,
                    Type = lane.Type
                });
            }
            return list;
        }

        /// <summary>
        /// Expands a node path into a lane-aligned polyline using outgoing lane centerlines where available.
        /// </summary>
        public List<Vector3> BuildLanePath(List<int> nodePath)
        {
            var points = new List<Vector3>();
            if (nodePath == null) return points;
            if (nodePath.Count < 2)
            {
                // Degenerate path: only one node. Return its position twice so movement systems have a segment to traverse.
                if (nodePath.Count == 1 && TryGetNode(nodePath[0], out var single))
                {
                    points.Add(single.Position);
                    points.Add(single.Position);
                }
                return points;
            }

            var hops = new List<(RoadSegment segment, int laneIndex, int fromNodeId, int toNodeId, List<Vector3> lanePoints)>();

            for (int i = 0; i < nodePath.Count - 1; i++)
            {
                int currentId = nodePath[i];
                int nextId = nodePath[i + 1];
                if (TryBuildHop(currentId, nextId, out var hop))
                {
                    hops.Add(hop);
                }
                else if (TryGetNode(currentId, out var c) && TryGetNode(nextId, out var n))
                {
                    hops.Add((null, -1, currentId, nextId, new List<Vector3> { c.Position, n.Position }));
                }
            }

            if (hops.Count == 0) return points;

            // Stitch hops together, inserting connector curves when available.
            points.AddRange(hops[0].lanePoints);
            for (int i = 0; i < hops.Count - 1; i++)
            {
                var current = hops[i];
                var next = hops[i + 1];
                int junctionNode = current.toNodeId;

                var connector = FindConnector(junctionNode, current.segment, current.laneIndex, next.segment, next.laneIndex);
                if (connector != null && connector.Points.Count > 0)
                {
                    AppendPoints(points, connector.Points);
                }
                else
                {
                    AppendPoints(points, next.lanePoints);
                }
            }

            return points;
        }

        private IEnumerable<(int neighborId, float cost)> GetReachableNeighbors(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node)) yield break;

            foreach (int segId in node.Segments)
            {
                if (!_segments.TryGetValue(segId, out var seg)) continue;
                bool nodeIsStart = seg.StartNodeId == nodeId;
                bool nodeIsEnd = seg.EndNodeId == nodeId;
                if (!nodeIsStart && !nodeIsEnd) continue;

                int neighborId = nodeIsStart ? seg.EndNodeId : seg.StartNodeId;
                if (!_nodes.ContainsKey(neighborId)) continue;

                bool hasOutgoingLane = false;
                float cost = 0f;
                for (int laneIndex = 0; laneIndex < seg.LaneDefinitions.Count; laneIndex++)
                {
                    var lane = seg.LaneDefinitions[laneIndex];
                    bool outgoing = nodeIsStart ? lane.Forward : !lane.Forward;
                    if (!outgoing) continue;

                    hasOutgoingLane = true;
                    cost = GetLaneLength(seg, laneIndex);
                    break;
                }

                if (!hasOutgoingLane) continue;
                if (cost <= 0f)
                {
                    cost = Vector3.Distance(_nodes[nodeId].Position, _nodes[neighborId].Position);
                }

                yield return (neighborId, cost);
            }
        }

        private float GetLaneLength(RoadSegment segment, int laneIndex)
        {
            if (segment.LaneCenterlines == null || laneIndex < 0 || laneIndex >= segment.LaneCenterlines.Count)
            {
                return 0f;
            }

            var lane = segment.LaneCenterlines[laneIndex];
            if (lane == null || lane.Count < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < lane.Count; i++)
            {
                length += Vector3.Distance(lane[i - 1], lane[i]);
            }
            return length;
        }

        private float HeuristicCost(int nodeId, int targetNodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node) || !_nodes.TryGetValue(targetNodeId, out var target))
            {
                return 0f;
            }
            return Vector3.Distance(node.Position, target.Position);
        }

        private int GetLowestFScore(List<int> openList, Dictionary<int, float> fScore)
        {
            int bestId = openList[0];
            float bestScore = fScore.TryGetValue(bestId, out var s) ? s : float.PositiveInfinity;
            for (int i = 1; i < openList.Count; i++)
            {
                int id = openList[i];
                float score = fScore.TryGetValue(id, out var val) ? val : float.PositiveInfinity;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestId = id;
                }
            }
            return bestId;
        }

        private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom.TryGetValue(current, out int previous))
            {
                current = previous;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private bool TryBuildHop(int fromNodeId, int toNodeId, out (RoadSegment segment, int laneIndex, int fromNodeId, int toNodeId, List<Vector3> lanePoints) hop)
        {
            hop = default;
            if (!TryGetNode(fromNodeId, out var from) || !TryGetNode(toNodeId, out var to))
            {
                return false;
            }

            RoadSegment chosen = null;
            int laneIndex = -1;
            bool directionAllowed = false;
            foreach (int segId in from.Segments)
            {
                if (!TryGetSegment(segId, out var seg)) continue;
                bool fromIsStart = seg.StartNodeId == fromNodeId && seg.EndNodeId == toNodeId;
                bool fromIsEnd = seg.EndNodeId == fromNodeId && seg.StartNodeId == toNodeId;
                if (!fromIsStart && !fromIsEnd) continue;

                for (int l = 0; l < seg.LaneDefinitions.Count; l++)
                {
                    var lane = seg.LaneDefinitions[l];
                    bool outgoing = fromIsStart ? lane.Forward : !lane.Forward;
                    if (!outgoing) continue;
                    chosen = seg;
                    laneIndex = l;
                    directionAllowed = true;
                    break;
                }

                if (chosen != null) break;
            }

            // If no outgoing lane was found but a segment exists, fall back to using any lane for the final hop
            // so agents can at least reach the destination node even if lane directions are restrictive.
            if (chosen == null)
            {
                foreach (int segId in from.Segments)
                {
                    if (!TryGetSegment(segId, out var seg)) continue;
                    bool matches = (seg.StartNodeId == fromNodeId && seg.EndNodeId == toNodeId) ||
                                   (seg.EndNodeId == fromNodeId && seg.StartNodeId == toNodeId);
                    if (!matches) continue;
                    if (seg.LaneDefinitions.Count > 0)
                    {
                        chosen = seg;
                        laneIndex = 0;
                        directionAllowed = false;
                        break;
                    }
                }
            }

            List<Vector3> lanePoints = null;
            if (chosen != null && laneIndex >= 0 && laneIndex < chosen.LaneCenterlines.Count)
            {
                lanePoints = chosen.LaneCenterlines[laneIndex];
                if (lanePoints != null && lanePoints.Count > 0)
                {
                    if (!(chosen.StartNodeId == fromNodeId))
                    {
                        lanePoints = new List<Vector3>(lanePoints);
                        lanePoints.Reverse();
                    }
                }
            }

            if (lanePoints == null || lanePoints.Count == 0)
            {
                lanePoints = new List<Vector3>();
                lanePoints.Add(from.Position);
                lanePoints.Add(to.Position);
            }

            hop = (chosen, laneIndex, fromNodeId, toNodeId, lanePoints);
            return true;
        }

        private LaneConnector FindConnector(int nodeId, RoadSegment incomingSeg, int incomingLaneIndex, RoadSegment outgoingSeg, int outgoingLaneIndex)
        {
            if (!TryGetNode(nodeId, out var node) || incomingSeg == null || outgoingSeg == null) return null;
            var connectors = IntersectionRoutingBuilder.BuildConnectors(node, this, IntersectionType.Star);
            foreach (var connector in connectors)
            {
                if (connector?.From == null || connector.To == null) continue;
                if (connector.From.SegmentId == incomingSeg.Id && connector.From.LaneIndex == incomingLaneIndex &&
                    connector.To.SegmentId == outgoingSeg.Id && connector.To.LaneIndex == outgoingLaneIndex)
                {
                    return connector;
                }
            }
            return null;
        }

        private void AppendPoints(List<Vector3> destination, List<Vector3> source)
        {
            if (destination == null || source == null || source.Count == 0) return;
            for (int i = 0; i < source.Count; i++)
            {
                Vector3 pt = source[i];
                if (destination.Count > 0 && i == 0 && Vector3.Distance(destination[destination.Count - 1], pt) < 0.001f)
                {
                    continue;
                }
                destination.Add(pt);
            }
        }
    }
}
