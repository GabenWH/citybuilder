using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Builds lane-to-lane connections at a node. Default is a "star": every incoming lane can go to every outgoing lane on other segments.
    /// </summary>
    public static class IntersectionRoutingBuilder
    {
        public static List<LaneConnection> BuildConnections(RoadNode node, RoadNetwork network, IntersectionType type = IntersectionType.Star)
        {
            switch (type)
            {
                case IntersectionType.Star:
                default:
                    return BuildStarConnections(node, network);
            }
        }

        public static List<LaneConnection> BuildStarConnections(RoadNode node, RoadNetwork network)
        {
            var connections = new List<LaneConnection>();
            if (node == null || network == null) return connections;

            var incoming = new List<LaneEndpoint>();
            var outgoing = new List<LaneEndpoint>();

            foreach (int segId in node.Segments)
            {
                if (!network.TryGetSegment(segId, out var seg)) continue;
                bool nodeIsStart = seg.StartNodeId == node.Id;

                for (int laneIndex = 0; laneIndex < seg.LaneDefinitions.Count; laneIndex++)
                {
                    var lane = seg.LaneDefinitions[laneIndex];
                    bool forward = lane.Forward; // true = start->end

                    bool isIncoming = nodeIsStart ? !forward : forward;
                    bool isOutgoing = !isIncoming;

                    var endpoint = new LaneEndpoint(node.Id, seg.Id, laneIndex, isIncoming);
                    if (isIncoming)
                    {
                        incoming.Add(endpoint);
                    }
                    if (isOutgoing)
                    {
                        outgoing.Add(endpoint);
                    }
                }
            }

            if (incoming.Count == 0 || outgoing.Count == 0) return connections;

            foreach (var inc in incoming)
            {
                var targets = outgoing.Where(outEp => outEp.SegmentId != inc.SegmentId).ToList();
                connections.Add(new LaneConnection(inc, targets));
            }

            return connections;
        }

        public static List<LaneConnector> BuildConnectors(RoadNode node, RoadNetwork network, IntersectionType type = IntersectionType.Star, float curveStrength = 0.35f, int samples = 8)
        {
            var connectors = new List<LaneConnector>();
            if (node == null || network == null) return connectors;

            var connections = BuildConnections(node, network, type);
            foreach (var conn in connections)
            {
                foreach (var to in conn.To)
                {
                    if (!network.TryGetSegment(conn.From.SegmentId, out var fromSeg)) continue;
                    if (!network.TryGetSegment(to.SegmentId, out var toSeg)) continue;

                    var startInfo = GetLaneEndpointData(node.Id, fromSeg, conn.From.LaneIndex, conn.From.IsIncoming);
                    var endInfo = GetLaneEndpointData(node.Id, toSeg, to.LaneIndex, !conn.From.IsIncoming); // outgoing from node
                    if (!startInfo.HasValue || !endInfo.HasValue) continue;

                    var (startPos, startDir) = startInfo.Value;
                    var (endPos, endDir) = endInfo.Value;
                    float chord = Vector3.Distance(startPos, endPos);
                    float handle = chord * curveStrength;

                    Vector3 p0 = startPos;
                    Vector3 p1 = startPos + startDir * handle;
                    Vector3 p2 = endPos - endDir * handle;
                    Vector3 p3 = endPos;

                    var polyline = SampleBezier(p0, p1, p2, p3, samples);
                    var connector = new LaneConnector(conn.From, to, polyline);
                    connector.LaneId = CombineLaneId(fromSeg.Id, conn.From.LaneIndex);
                    connector.EndNodeId = to.NodeId;
                    connectors.Add(connector);
                }
            }

            return connectors;
        }

        private static int CombineLaneId(int segmentId, int laneIndex)
        {
            // Pack segment id and lane index into a single int (16 bits each) for simple identification.
            // Assumes reasonable limits on counts; adjust packing if needed.
            return (segmentId << 16) | (laneIndex & 0xFFFF);
        }

        private static (Vector3, Vector3)? GetLaneEndpointData(int nodeId, RoadSegment seg, int laneIndex, bool incoming)
        {
            if (seg.LaneCenterlines == null || laneIndex < 0 || laneIndex >= seg.LaneCenterlines.Count)
            {
                return null;
            }

            var lane = seg.LaneCenterlines[laneIndex];
            if (lane == null || lane.Count < 2) return null;

            bool nodeIsStart = seg.StartNodeId == nodeId;
            Vector3 pos;
            Vector3 dir;
            if (incoming)
            {
                if (nodeIsStart)
                {
                    pos = lane[0];
                    dir = (lane[0] - lane[1]).normalized; // towards node
                }
                else
                {
                    pos = lane[lane.Count - 1];
                    dir = (lane[lane.Count - 1] - lane[lane.Count - 2]).normalized;
                }
            }
            else
            {
                if (nodeIsStart)
                {
                    pos = lane[0];
                    dir = (lane[1] - lane[0]).normalized; // away from node
                }
                else
                {
                    pos = lane[lane.Count - 1];
                    dir = (lane[lane.Count - 2] - lane[lane.Count - 1]).normalized; // away from node
                }
            }

            return (pos, dir);
        }

        private static List<Vector3> SampleBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int samples)
        {
            var pts = new List<Vector3>(samples + 1);
            samples = Mathf.Max(2, samples);
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float u = 1f - t;
                Vector3 point = u * u * u * p0 +
                                3f * u * u * t * p1 +
                                3f * u * t * t * p2 +
                                t * t * t * p3;
                pts.Add(point);
            }
            return pts;
        }
    }
}
