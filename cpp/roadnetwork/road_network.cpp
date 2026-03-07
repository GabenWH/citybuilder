#include "road_network.hpp"

#include <stdexcept>

namespace citybuilder::roads
{
    // Defaults to the C# behavior: lanes with offset <= 0 are forward from start->end.
    std::function<bool(float)> RoadSegment::DefaultLaneDirectionResolver = [](float offset) { return offset <= 0.0f; };

    // --- RoadNode --------------------------------------------------------------------
    RoadNode::RoadNode(int id, const Vec3& position, const Vec2i& sector)
        : id(id), position(position), sector(sector)
    {
    }

    void RoadNode::SetPosition(const Vec3& pos)
    {
        position = pos;
    }

    void RoadNode::SetSector(const Vec2i& sec)
    {
        sector = sec;
    }

    void RoadNode::SetRole(EntryExitType newRole)
    {
        role = newRole;
    }

    bool RoadNode::HasRole(EntryExitType mask) const
    {
        return static_cast<int>(role & mask) != 0;
    }

    void RoadNode::AddSegment(int segmentId)
    {
        segments.insert(segmentId);
    }

    void RoadNode::RemoveSegment(int segmentId)
    {
        segments.erase(segmentId);
    }

    // --- RoadSegment -----------------------------------------------------------------
    RoadSegment::RoadSegment(int id,
                             int startNodeId,
                             int endNodeId,
                             const std::vector<Vec3>& controlPoints,
                             float width,
                             int lanes,
                             const Vec2i& sector)
        : id(id),
          startNodeId(startNodeId),
          endNodeId(endNodeId),
          width(width),
          lanes(std::max(1, lanes)),
          controlPoints(controlPoints),
          sector(sector)
    {
        EnsureLaneDefinitions();
        RebuildLanes();
    }

    void RoadSegment::SetControlPoints(const std::vector<Vec3>& points)
    {
        controlPoints = points;
        EnsureLaneDefinitions();
        RebuildLanes();
    }

    void RoadSegment::SetWidth(float w)
    {
        width = std::max(0.01f, w);
        EnsureLaneDefinitions();
        RebuildLanes();
    }

    void RoadSegment::SetLanes(int l)
    {
        lanes = std::max(1, l);
        EnsureLaneDefinitions();
        RebuildLanes();
    }

    void RoadSegment::SetSector(const Vec2i& sec)
    {
        sector = sec;
    }

    void RoadSegment::SetLaneDefinitions(const std::vector<LaneDefinition>& definitions)
    {
        laneDefinitions = definitions;
        lanes = std::max(1, static_cast<int>(laneDefinitions.size()));
        RebuildLanes();
    }

    void RoadSegment::RebuildLanes()
    {
        laneCenterlines.clear();
        if (controlPoints.size() < 2 || lanes <= 0)
        {
            return;
        }

        for (const auto& lane : laneDefinitions)
        {
            laneCenterlines.emplace_back(OffsetPolyline(controlPoints, lane.offset));
        }
    }

    void RoadSegment::EnsureLaneDefinitions()
    {
        laneDefinitions.clear();
        float laneWidth = width / static_cast<float>(lanes);
        float offsetStart = -width * 0.5f + laneWidth * 0.5f;
        for (int i = 0; i < lanes; ++i)
        {
            float offset = offsetStart + i * laneWidth;
            bool forward = DefaultLaneDirectionResolver ? DefaultLaneDirectionResolver(offset) : offset <= 0.0f;
            laneDefinitions.push_back({offset, laneWidth, forward, {}});
        }
    }

    std::vector<Vec3> RoadSegment::OffsetPolyline(const std::vector<Vec3>& points, float offset)
    {
        std::vector<Vec3> result;
        result.reserve(points.size());
        for (size_t i = 0; i < points.size(); ++i)
        {
            Vec3 forward;
            if (i == 0)
            {
                forward = Normalize(points[i + 1] - points[i]);
            }
            else if (i == points.size() - 1)
            {
                forward = Normalize(points[i] - points[i - 1]);
            }
            else
            {
                forward = Normalize((points[i] - points[i - 1]) + (points[i + 1] - points[i]));
            }

            Vec3 left{-forward.z, 0.0f, forward.x};
            left = Normalize(left);
            result.push_back(points[i] + left * offset);
        }
        return result;
    }

    // --- RoadNetwork -----------------------------------------------------------------
    RoadNode& RoadNetwork::AddNode(const Vec3& position, const Vec2i& sector)
    {
        return AddNodeWithId(nextNodeId++, position, sector, EntryExitType::None);
    }

    RoadNode& RoadNetwork::AddNodeWithId(int id, const Vec3& position, const Vec2i& sector, EntryExitType role)
    {
        if (id >= nextNodeId)
        {
            nextNodeId = id + 1;
        }
        auto [iter, inserted] = nodes.emplace(id, RoadNode{id, position, sector});
        iter->second.SetRole(role);
        return iter->second;
    }

    bool RoadNetwork::RemoveNode(int nodeId)
    {
        auto it = nodes.find(nodeId);
        if (it == nodes.end())
        {
            return false;
        }

        // Remove attached segments first.
        std::vector<int> attached(it->second.Segments().begin(), it->second.Segments().end());
        for (int segId : attached)
        {
            RemoveSegment(segId);
        }

        nodes.erase(it);
        return true;
    }

    RoadSegment& RoadNetwork::AddSegment(int startNodeId,
                                         int endNodeId,
                                         const std::vector<Vec3>& controlPoints,
                                         float width,
                                         int lanes,
                                         const Vec2i& sector)
    {
        return AddSegmentWithId(nextSegmentId++, startNodeId, endNodeId, controlPoints, width, lanes, sector);
    }

    RoadSegment& RoadNetwork::AddSegmentWithId(int id,
                                               int startNodeId,
                                               int endNodeId,
                                               const std::vector<Vec3>& controlPoints,
                                               float width,
                                               int lanes,
                                               const Vec2i& sector)
    {
        if (!nodes.count(startNodeId))
        {
            throw std::invalid_argument("Missing start node");
        }
        if (!nodes.count(endNodeId))
        {
            throw std::invalid_argument("Missing end node");
        }
        if (controlPoints.size() < 2)
        {
            throw std::invalid_argument("Segments need at least 2 control points");
        }
        if (id >= nextSegmentId)
        {
            nextSegmentId = id + 1;
        }

        auto [iter, inserted] = segments.emplace(id, RoadSegment{id, startNodeId, endNodeId, controlPoints, width, lanes, sector});
        nodes.at(startNodeId).AddSegment(id);
        nodes.at(endNodeId).AddSegment(id);
        return iter->second;
    }

    bool RoadNetwork::RemoveSegment(int segmentId)
    {
        auto it = segments.find(segmentId);
        if (it == segments.end())
        {
            return false;
        }

        const RoadSegment& seg = it->second;
        if (auto nodeIt = nodes.find(seg.StartNodeId()); nodeIt != nodes.end())
        {
            nodeIt->second.RemoveSegment(segmentId);
        }
        if (auto nodeIt = nodes.find(seg.EndNodeId()); nodeIt != nodes.end())
        {
            nodeIt->second.RemoveSegment(segmentId);
        }

        segments.erase(it);
        return true;
    }

    bool RoadNetwork::TryGetNode(int nodeId, RoadNode*& node)
    {
        auto it = nodes.find(nodeId);
        if (it == nodes.end())
        {
            return false;
        }
        node = &it->second;
        return true;
    }

    bool RoadNetwork::TryGetSegment(int segmentId, RoadSegment*& segment)
    {
        auto it = segments.find(segmentId);
        if (it == segments.end())
        {
            return false;
        }
        segment = &it->second;
        return true;
    }

    bool RoadNetwork::TryGetNode(int nodeId, const RoadNode*& node) const
    {
        auto it = nodes.find(nodeId);
        if (it == nodes.end())
        {
            return false;
        }
        node = &it->second;
        return true;
    }

    bool RoadNetwork::TryGetSegment(int segmentId, const RoadSegment*& segment) const
    {
        auto it = segments.find(segmentId);
        if (it == segments.end())
        {
            return false;
        }
        segment = &it->second;
        return true;
    }

    std::optional<RoadNode> RoadNetwork::TryGetRandomExitNode(const std::vector<int>& excludeNodeIds) const
    {
        std::unordered_set<int> excluded(excludeNodeIds.begin(), excludeNodeIds.end());
        std::vector<const RoadNode*> exits;
        for (const auto& kvp : nodes)
        {
            const auto& node = kvp.second;
            if (node.HasRole(EntryExitType::Exit) && !excluded.count(node.Id()))
            {
                exits.push_back(&node);
            }
        }

        if (exits.empty())
        {
            return std::nullopt;
        }

        std::random_device rd;
        std::mt19937 gen(rd());
        std::uniform_int_distribution<size_t> dist(0, exits.size() - 1);
        return *exits[dist(gen)];
    }

    std::vector<int> RoadNetwork::FindPathAStar(int startNodeId, int endNodeId) const
    {
        if (startNodeId == endNodeId)
        {
            return {startNodeId};
        }
        if (!nodes.count(startNodeId) || !nodes.count(endNodeId))
        {
            return {};
        }

        std::unordered_set<int> openSet{startNodeId};
        std::vector<int> openList{startNodeId};
        std::unordered_map<int, int> cameFrom;
        std::unordered_map<int, float> gScore{{startNodeId, 0.0f}};
        std::unordered_map<int, float> fScore{{startNodeId, HeuristicCost(startNodeId, endNodeId)}};

        while (!openList.empty())
        {
            int current = GetLowestFScore(openList, fScore);
            if (current == endNodeId)
            {
                return ReconstructPath(cameFrom, current);
            }

            openList.erase(std::remove(openList.begin(), openList.end(), current), openList.end());
            openSet.erase(current);

            for (const auto& [neighbor, cost] : GetReachableNeighbors(current))
            {
                float tentativeG = gScore[current] + cost;
                auto it = gScore.find(neighbor);
                if (it == gScore.end() || tentativeG < it->second)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + HeuristicCost(neighbor, endNodeId);
                    if (!openSet.count(neighbor))
                    {
                        openSet.insert(neighbor);
                        openList.push_back(neighbor);
                    }
                }
            }
        }

        return {};
    }

    void RoadNetwork::AddEntryPoint(int nodeId)
    {
        SetEntryExitRole(nodeId, EntryExitType::Entry | (nodes.at(nodeId).Role() & EntryExitType::Exit));
    }

    void RoadNetwork::RemoveEntryPoint(int nodeId)
    {
        SetEntryExitRole(nodeId, nodes.at(nodeId).Role() & static_cast<EntryExitType>(~static_cast<int>(EntryExitType::Entry)));
    }

    void RoadNetwork::AddExitPoint(int nodeId)
    {
        SetEntryExitRole(nodeId, EntryExitType::Exit | (nodes.at(nodeId).Role() & EntryExitType::Entry));
    }

    void RoadNetwork::RemoveExitPoint(int nodeId)
    {
        SetEntryExitRole(nodeId, nodes.at(nodeId).Role() & static_cast<EntryExitType>(~static_cast<int>(EntryExitType::Exit)));
    }

    void RoadNetwork::ClearEntryExitRoles(int nodeId)
    {
        SetEntryExitRole(nodeId, EntryExitType::None);
    }

    void RoadNetwork::SetEntryExitRole(int nodeId, EntryExitType role)
    {
        auto it = nodes.find(nodeId);
        if (it == nodes.end())
        {
            throw std::invalid_argument("Missing node");
        }
        it->second.SetRole(role);
    }

    RoadNode& RoadNetwork::SplitSegment(int segmentId, const Vec3& point, int segmentIndex, std::function<void(RoadSegment&)> onSegmentCreated)
    {
        auto segIt = segments.find(segmentId);
        if (segIt == segments.end())
        {
            throw std::invalid_argument("Missing segment");
        }
        RoadSegment& segment = segIt->second;
        if (segment.ControlPoints().size() < 2)
        {
            throw std::invalid_argument("Segment has insufficient control points");
        }
        if (segmentIndex < 0 || segmentIndex >= static_cast<int>(segment.ControlPoints().size() - 1))
        {
            throw std::out_of_range("segmentIndex must be a valid segment between control points");
        }

        const auto& cp = segment.ControlPoints();
        std::vector<Vec3> first;
        std::vector<Vec3> second;
        for (int i = 0; i <= segmentIndex; ++i) first.push_back(cp[i]);
        first.push_back(point);
        second.push_back(point);
        for (size_t i = static_cast<size_t>(segmentIndex + 1); i < cp.size(); ++i) second.push_back(cp[i]);

        RoadNode& newNode = AddNode(point, segment.Sector());
        RemoveSegment(segment.Id());

        auto laneDefs = CloneLaneDefinitions(segment.LaneDefinitions());

        RoadSegment& segA = AddSegment(segment.StartNodeId(), newNode.Id(), first, segment.Width(), segment.Lanes(), segment.Sector());
        segA.SetLaneDefinitions(laneDefs);
        if (onSegmentCreated) onSegmentCreated(segA);

        RoadSegment& segB = AddSegment(newNode.Id(), segment.EndNodeId(), second, segment.Width(), segment.Lanes(), segment.Sector());
        segB.SetLaneDefinitions(laneDefs);
        if (onSegmentCreated) onSegmentCreated(segB);

        return newNode;
    }

    std::vector<Vec3> RoadNetwork::BuildLanePath(const std::vector<int>& nodePath) const
    {
        std::vector<Vec3> points;
        if (nodePath.empty())
        {
            return points;
        }
        if (nodePath.size() < 2)
        {
            const RoadNode* single = nullptr;
            if (TryGetNode(nodePath[0], single))
            {
                points.push_back(single->Position());
                points.push_back(single->Position());
            }
            return points;
        }

        std::vector<std::tuple<const RoadSegment*, int, int, int, std::vector<Vec3>>> hops;
        for (size_t i = 0; i < nodePath.size() - 1; ++i)
        {
            int currentId = nodePath[i];
            int nextId = nodePath[i + 1];
            std::tuple<const RoadSegment*, int, int, int, std::vector<Vec3>> hop;
            if (TryBuildHop(currentId, nextId, hop))
            {
                hops.push_back(hop);
            }
            else
            {
                const RoadNode* c = nullptr;
                const RoadNode* n = nullptr;
                if (TryGetNode(currentId, c) && TryGetNode(nextId, n))
                {
                    hops.push_back({nullptr, -1, currentId, nextId, std::vector<Vec3>{c->Position(), n->Position()}});
                }
            }
        }

        if (hops.empty())
        {
            return points;
        }

        points.insert(points.end(), std::get<4>(hops[0]).begin(), std::get<4>(hops[0]).end());
        for (size_t i = 0; i + 1 < hops.size(); ++i)
        {
            const auto& current = hops[i];
            const auto& next = hops[i + 1];
            int junctionNode = std::get<3>(current);

            LaneConnector* connector = FindConnector(junctionNode,
                                                     std::get<0>(current),
                                                     std::get<1>(current),
                                                     std::get<0>(next),
                                                     std::get<1>(next));
            if (connector && !connector->Points().empty())
            {
                AppendPoints(points, connector->Points());
            }
            else
            {
                AppendPoints(points, std::get<4>(next));
            }
        }

        return points;
    }

    std::vector<RoadSegment::LaneDefinition> RoadNetwork::CloneLaneDefinitions(const std::vector<RoadSegment::LaneDefinition>& src) const
    {
        return src;
    }

    std::vector<std::pair<int, float>> RoadNetwork::GetReachableNeighbors(int nodeId) const
    {
        std::vector<std::pair<int, float>> neighbors;
        const RoadNode* node = nullptr;
        if (!TryGetNode(nodeId, node))
        {
            return neighbors;
        }

        for (int segId : node->Segments())
        {
            const RoadSegment* seg = nullptr;
            if (!TryGetSegment(segId, seg))
            {
                continue;
            }
            bool nodeIsStart = seg->StartNodeId() == nodeId;
            bool nodeIsEnd = seg->EndNodeId() == nodeId;
            if (!nodeIsStart && !nodeIsEnd)
            {
                continue;
            }

            int neighborId = nodeIsStart ? seg->EndNodeId() : seg->StartNodeId();
            if (!nodes.count(neighborId))
            {
                continue;
            }

            bool hasOutgoingLane = false;
            float cost = 0.0f;
            for (size_t laneIndex = 0; laneIndex < seg->LaneDefinitions().size(); ++laneIndex)
            {
                const auto& lane = seg->LaneDefinitions()[laneIndex];
                bool outgoing = nodeIsStart ? lane.forward : !lane.forward;
                if (!outgoing)
                {
                    continue;
                }
                hasOutgoingLane = true;
                cost = GetLaneLength(*seg, static_cast<int>(laneIndex));
                break;
            }

            if (!hasOutgoingLane)
            {
                continue;
            }
            if (cost <= 0.0f)
            {
                cost = Distance(nodes.at(nodeId).Position(), nodes.at(neighborId).Position());
            }

            neighbors.emplace_back(neighborId, cost);
        }

        return neighbors;
    }

    float RoadNetwork::GetLaneLength(const RoadSegment& segment, int laneIndex) const
    {
        if (laneIndex < 0 || laneIndex >= static_cast<int>(segment.LaneCenterlines().size()))
        {
            return 0.0f;
        }

        const auto& lane = segment.LaneCenterlines()[static_cast<size_t>(laneIndex)];
        if (lane.size() < 2)
        {
            return 0.0f;
        }

        float length = 0.0f;
        for (size_t i = 1; i < lane.size(); ++i)
        {
            length += Distance(lane[i - 1], lane[i]);
        }
        return length;
    }

    float RoadNetwork::HeuristicCost(int nodeId, int targetNodeId) const
    {
        const RoadNode* node = nullptr;
        const RoadNode* target = nullptr;
        if (!TryGetNode(nodeId, node) || !TryGetNode(targetNodeId, target))
        {
            return 0.0f;
        }
        return Distance(node->Position(), target->Position());
    }

    int RoadNetwork::GetLowestFScore(const std::vector<int>& openList, const std::unordered_map<int, float>& fScore) const
    {
        int bestId = openList.front();
        float bestScore = std::numeric_limits<float>::infinity();
        if (auto it = fScore.find(bestId); it != fScore.end())
        {
            bestScore = it->second;
        }

        for (size_t i = 1; i < openList.size(); ++i)
        {
            int id = openList[i];
            float score = std::numeric_limits<float>::infinity();
            if (auto it = fScore.find(id); it != fScore.end())
            {
                score = it->second;
            }
            if (score < bestScore)
            {
                bestScore = score;
                bestId = id;
            }
        }
        return bestId;
    }

    std::vector<int> RoadNetwork::ReconstructPath(const std::unordered_map<int, int>& cameFrom, int current) const
    {
        std::vector<int> path{current};
        int cursor = current;
        auto it = cameFrom.find(cursor);
        while (it != cameFrom.end())
        {
            cursor = it->second;
            path.push_back(cursor);
            it = cameFrom.find(cursor);
        }
        std::reverse(path.begin(), path.end());
        return path;
    }

    bool RoadNetwork::TryBuildHop(int fromNodeId, int toNodeId, std::tuple<const RoadSegment*, int, int, int, std::vector<Vec3>>& hop) const
    {
        const RoadNode* from = nullptr;
        const RoadNode* to = nullptr;
        if (!TryGetNode(fromNodeId, from) || !TryGetNode(toNodeId, to))
        {
            return false;
        }

        const RoadSegment* chosen = nullptr;
        int laneIndex = -1;
        for (int segId : from->Segments())
        {
            const RoadSegment* seg = nullptr;
            if (!TryGetSegment(segId, seg))
            {
                continue;
            }
            bool fromIsStart = seg->StartNodeId() == fromNodeId && seg->EndNodeId() == toNodeId;
            bool fromIsEnd = seg->EndNodeId() == fromNodeId && seg->StartNodeId() == toNodeId;
            if (!fromIsStart && !fromIsEnd)
            {
                continue;
            }

            for (size_t l = 0; l < seg->LaneDefinitions().size(); ++l)
            {
                const auto& lane = seg->LaneDefinitions()[l];
                bool outgoing = fromIsStart ? lane.forward : !lane.forward;
                if (!outgoing)
                {
                    continue;
                }
                chosen = seg;
                laneIndex = static_cast<int>(l);
                break;
            }

            if (chosen)
            {
                break;
            }
        }

        // Fallback to any lane so we can still connect the last hop even if directions are restrictive.
        if (!chosen)
        {
            for (int segId : from->Segments())
            {
                const RoadSegment* seg = nullptr;
                if (!TryGetSegment(segId, seg))
                {
                    continue;
                }
                bool matches = (seg->StartNodeId() == fromNodeId && seg->EndNodeId() == toNodeId) ||
                               (seg->EndNodeId() == fromNodeId && seg->StartNodeId() == toNodeId);
                if (!matches)
                {
                    continue;
                }
                if (!seg->LaneDefinitions().empty())
                {
                    chosen = seg;
                    laneIndex = 0;
                    break;
                }
            }
        }

        std::vector<Vec3> lanePoints;
        if (chosen && laneIndex >= 0 && laneIndex < static_cast<int>(chosen->LaneCenterlines().size()))
        {
            lanePoints = chosen->LaneCenterlines()[static_cast<size_t>(laneIndex)];
            if (!lanePoints.empty() && chosen->StartNodeId() != fromNodeId)
            {
                std::reverse(lanePoints.begin(), lanePoints.end());
            }
        }

        if (lanePoints.empty())
        {
            lanePoints.push_back(from->Position());
            lanePoints.push_back(to->Position());
        }

        hop = {chosen, laneIndex, fromNodeId, toNodeId, lanePoints};
        return true;
    }

    LaneConnector* RoadNetwork::FindConnector(int nodeId,
                                              const RoadSegment* incomingSeg,
                                              int incomingLaneIndex,
                                              const RoadSegment* outgoingSeg,
                                              int outgoingLaneIndex) const
    {
        const RoadNode* node = nullptr;
        if (!incomingSeg || !outgoingSeg || !TryGetNode(nodeId, node))
        {
            return nullptr;
        }

        // Build connectors on the fly for now; for debugging this avoids storing extra state.
        static thread_local std::vector<LaneConnector> scratch;
        scratch = IntersectionRoutingBuilder::BuildConnectors(*node, *this, IntersectionType::Star);

        for (auto& connector : scratch)
        {
            if (connector.From().SegmentId() == incomingSeg->Id() &&
                connector.From().LaneIndex() == incomingLaneIndex &&
                connector.To().SegmentId() == outgoingSeg->Id() &&
                connector.To().LaneIndex() == outgoingLaneIndex)
            {
                return &connector;
            }
        }

        return nullptr;
    }

    void RoadNetwork::AppendPoints(std::vector<Vec3>& destination, const std::vector<Vec3>& source) const
    {
        if (destination.empty())
        {
            destination.insert(destination.end(), source.begin(), source.end());
            return;
        }

        for (size_t i = 0; i < source.size(); ++i)
        {
            const Vec3& pt = source[i];
            if (!destination.empty() && i == 0 && Distance(destination.back(), pt) < 0.001f)
            {
                continue;
            }
            destination.push_back(pt);
        }
    }

    // --- IntersectionRoutingBuilder --------------------------------------------------
    std::vector<LaneConnection> IntersectionRoutingBuilder::BuildConnections(const RoadNode& node,
                                                                              const RoadNetwork& network,
                                                                              IntersectionType type)
    {
        switch (type)
        {
            case IntersectionType::Star:
            default:
                return BuildStarConnections(node, network);
        }
    }

    std::vector<LaneConnection> IntersectionRoutingBuilder::BuildStarConnections(const RoadNode& node, const RoadNetwork& network)
    {
        std::vector<LaneConnection> connections;
        std::vector<LaneEndpoint> incoming;
        std::vector<LaneEndpoint> outgoing;

        for (int segId : node.Segments())
        {
            const RoadSegment* seg = nullptr;
            if (!network.TryGetSegment(segId, seg))
            {
                continue;
            }
            bool nodeIsStart = seg->StartNodeId() == node.Id();

            for (size_t laneIndex = 0; laneIndex < seg->LaneDefinitions().size(); ++laneIndex)
            {
                const auto& lane = seg->LaneDefinitions()[laneIndex];
                bool forward = lane.forward;
                bool isIncoming = nodeIsStart ? !forward : forward;
                bool isOutgoing = !isIncoming;

                LaneEndpoint endpoint(node.Id(), seg->Id(), static_cast<int>(laneIndex), isIncoming);
                if (isIncoming) incoming.push_back(endpoint);
                if (isOutgoing) outgoing.push_back(endpoint);
            }
        }

        if (incoming.empty() || outgoing.empty())
        {
            return connections;
        }

        for (const auto& inc : incoming)
        {
            std::vector<LaneEndpoint> targets;
            for (const auto& out : outgoing)
            {
                if (out.SegmentId() == inc.SegmentId())
                {
                    continue;
                }
                targets.push_back(out);
            }
            connections.emplace_back(inc, targets);
        }

        return connections;
    }

    std::vector<LaneConnector> IntersectionRoutingBuilder::BuildConnectors(const RoadNode& node,
                                                                           const RoadNetwork& network,
                                                                           IntersectionType type,
                                                                           float curveStrength,
                                                                           int samples)
    {
        std::vector<LaneConnector> connectors;
        auto connections = BuildConnections(node, network, type);
        for (const auto& conn : connections)
        {
            for (const auto& to : conn.To())
            {
                const RoadSegment* fromSeg = nullptr;
                const RoadSegment* toSeg = nullptr;
                if (!network.TryGetSegment(conn.From().SegmentId(), fromSeg) || !network.TryGetSegment(to.SegmentId(), toSeg))
                {
                    continue;
                }

                auto startInfo = GetLaneEndpointData(node.Id(), *fromSeg, conn.From().LaneIndex(), conn.From().IsIncoming());
                auto endInfo = GetLaneEndpointData(node.Id(), *toSeg, to.LaneIndex(), !conn.From().IsIncoming());
                if (!startInfo.has_value() || !endInfo.has_value())
                {
                    continue;
                }

                const auto& [startPos, startDir] = *startInfo;
                const auto& [endPos, endDir] = *endInfo;
                float chord = Distance(startPos, endPos);
                float handle = chord * curveStrength;

                Vec3 p0 = startPos;
                Vec3 p1 = startPos + startDir * handle;
                Vec3 p2 = endPos - endDir * handle;
                Vec3 p3 = endPos;

                auto polyline = SampleBezier(p0, p1, p2, p3, samples);
                LaneConnector connector(conn.From(), to, polyline);
                connector.LaneId = CombineLaneId(fromSeg->Id(), conn.From().LaneIndex());
                connector.EndNodeId = to.NodeId();
                connectors.push_back(std::move(connector));
            }
        }

        return connectors;
    }

    int IntersectionRoutingBuilder::CombineLaneId(int segmentId, int laneIndex)
    {
        return (segmentId << 16) | (laneIndex & 0xFFFF);
    }

    std::optional<std::pair<Vec3, Vec3>> IntersectionRoutingBuilder::GetLaneEndpointData(int nodeId,
                                                                                          const RoadSegment& seg,
                                                                                          int laneIndex,
                                                                                          bool incoming)
    {
        if (laneIndex < 0 || laneIndex >= static_cast<int>(seg.LaneCenterlines().size()))
        {
            return std::nullopt;
        }
        const auto& lane = seg.LaneCenterlines()[static_cast<size_t>(laneIndex)];
        if (lane.size() < 2)
        {
            return std::nullopt;
        }

        bool nodeIsStart = seg.StartNodeId() == nodeId;
        Vec3 pos{};
        Vec3 dir{};
        if (incoming)
        {
            if (nodeIsStart)
            {
                pos = lane.front();
                dir = Normalize(lane.front() - lane[1]);
            }
            else
            {
                pos = lane.back();
                dir = Normalize(lane.back() - lane[lane.size() - 2]);
            }
        }
        else
        {
            if (nodeIsStart)
            {
                pos = lane.front();
                dir = Normalize(lane[1] - lane.front());
            }
            else
            {
                pos = lane.back();
                dir = Normalize(lane[lane.size() - 2] - lane.back());
            }
        }

        return std::make_pair(pos, dir);
    }

    std::vector<Vec3> IntersectionRoutingBuilder::SampleBezier(const Vec3& p0, const Vec3& p1, const Vec3& p2, const Vec3& p3, int samples)
    {
        std::vector<Vec3> pts;
        samples = std::max(2, samples);
        pts.reserve(static_cast<size_t>(samples + 1));
        for (int i = 0; i <= samples; ++i)
        {
            float t = static_cast<float>(i) / static_cast<float>(samples);
            float u = 1.0f - t;
            Vec3 point = p0 * (u * u * u) +
                         p1 * (3.0f * u * u * t) +
                         p2 * (3.0f * u * t * t) +
                         p3 * (t * t * t);
            pts.push_back(point);
        }
        return pts;
    }
} // namespace citybuilder::roads
