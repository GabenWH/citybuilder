#pragma once

#include <algorithm>
#include <cmath>
#include <cstddef>
#include <functional>
#include <limits>
#include <optional>
#include <random>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>

namespace citybuilder::roads
{
    struct Vec2i
    {
        int x = 0;
        int y = 0;
    };

    struct Vec3
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        Vec3() = default;
        Vec3(float xx, float yy, float zz) : x(xx), y(yy), z(zz) {}

        Vec3 operator+(const Vec3& other) const { return Vec3{x + other.x, y + other.y, z + other.z}; }
        Vec3 operator-(const Vec3& other) const { return Vec3{x - other.x, y - other.y, z - other.z}; }
        Vec3 operator*(float s) const { return Vec3{x * s, y * s, z * s}; }
        Vec3 operator/(float s) const { return Vec3{x / s, y / s, z / s}; }

        Vec3& operator+=(const Vec3& other)
        {
            x += other.x;
            y += other.y;
            z += other.z;
            return *this;
        }
    };

    inline float Dot(const Vec3& a, const Vec3& b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    inline float Length(const Vec3& v)
    {
        return std::sqrt(Dot(v, v));
    }

    inline Vec3 Normalize(const Vec3& v)
    {
        float len = Length(v);
        if (len <= std::numeric_limits<float>::epsilon())
        {
            return Vec3{};
        }
        return v / len;
    }

    inline float Distance(const Vec3& a, const Vec3& b)
    {
        return Length(a - b);
    }

    enum class EntryExitType
    {
        None = 0,
        Entry = 1 << 0,
        Exit = 1 << 1,
        Both = Entry | Exit
    };

    inline EntryExitType operator|(EntryExitType a, EntryExitType b)
    {
        return static_cast<EntryExitType>(static_cast<int>(a) | static_cast<int>(b));
    }

    inline EntryExitType operator&(EntryExitType a, EntryExitType b)
    {
        return static_cast<EntryExitType>(static_cast<int>(a) & static_cast<int>(b));
    }

    class RoadNode
    {
    public:
        RoadNode(int id, const Vec3& position, const Vec2i& sector);

        int Id() const { return id; }
        const Vec3& Position() const { return position; }
        const Vec2i& Sector() const { return sector; }
        EntryExitType Role() const { return role; }
        const std::unordered_set<int>& Segments() const { return segments; }

        void SetPosition(const Vec3& pos);
        void SetSector(const Vec2i& sec);
        void SetRole(EntryExitType newRole);
        bool HasRole(EntryExitType mask) const;

        void AddSegment(int segmentId);
        void RemoveSegment(int segmentId);

    private:
        int id;
        Vec3 position;
        Vec2i sector;
        EntryExitType role = EntryExitType::None;
        std::unordered_set<int> segments;
    };

    class RoadSegment
    {
    public:
        struct LaneDefinition
        {
            float offset = 0.0f; // Offset from centerline (left positive)
            float width = 0.0f;
            bool forward = true;
            std::string type;
        };

        static std::function<bool(float)> DefaultLaneDirectionResolver;

        RoadSegment(int id,
                    int startNodeId,
                    int endNodeId,
                    const std::vector<Vec3>& controlPoints,
                    float width,
                    int lanes,
                    const Vec2i& sector);

        int Id() const { return id; }
        int StartNodeId() const { return startNodeId; }
        int EndNodeId() const { return endNodeId; }
        float Width() const { return width; }
        int Lanes() const { return lanes; }
        const std::vector<Vec3>& ControlPoints() const { return controlPoints; }
        const Vec2i& Sector() const { return sector; }
        const std::vector<LaneDefinition>& LaneDefinitions() const { return laneDefinitions; }
        const std::vector<std::vector<Vec3>>& LaneCenterlines() const { return laneCenterlines; }

        void SetControlPoints(const std::vector<Vec3>& points);
        void SetWidth(float w);
        void SetLanes(int l);
        void SetSector(const Vec2i& sec);
        void SetLaneDefinitions(const std::vector<LaneDefinition>& definitions);

    private:
        int id;
        int startNodeId;
        int endNodeId;
        float width;
        int lanes;
        std::vector<Vec3> controlPoints;
        Vec2i sector;
        std::vector<LaneDefinition> laneDefinitions;
        std::vector<std::vector<Vec3>> laneCenterlines;

        void RebuildLanes();
        void EnsureLaneDefinitions();
        static std::vector<Vec3> OffsetPolyline(const std::vector<Vec3>& points, float offset);

        friend class RoadNetwork;
        friend class IntersectionRoutingBuilder;
    };

    class LaneEndpoint
    {
    public:
        LaneEndpoint(int nodeId, int segmentId, int laneIndex, bool isIncoming)
            : nodeId(nodeId), segmentId(segmentId), laneIndex(laneIndex), isIncoming(isIncoming)
        {
        }

        int NodeId() const { return nodeId; }
        int SegmentId() const { return segmentId; }
        int LaneIndex() const { return laneIndex; }
        bool IsIncoming() const { return isIncoming; }

    private:
        int nodeId;
        int segmentId;
        int laneIndex;
        bool isIncoming;
    };

    class LaneConnection
    {
    public:
        LaneConnection(const LaneEndpoint& from, std::vector<LaneEndpoint> to)
            : from(from), to(std::move(to))
        {
        }

        const LaneEndpoint& From() const { return from; }
        const std::vector<LaneEndpoint>& To() const { return to; }

    private:
        LaneEndpoint from;
        std::vector<LaneEndpoint> to;
    };

    class LaneConnector
    {
    public:
        LaneConnector(const LaneEndpoint& from, const LaneEndpoint& to, std::vector<Vec3> points)
            : from(from), to(to), points(std::move(points))
        {
        }

        const LaneEndpoint& From() const { return from; }
        const LaneEndpoint& To() const { return to; }
        const std::vector<Vec3>& Points() const { return points; }

        int LaneId = -1;
        int EndNodeId = -1;

    private:
        LaneEndpoint from;
        LaneEndpoint to;
        std::vector<Vec3> points;
    };

    enum class IntersectionType
    {
        Star = 0,
        Custom = 1,
        Roundabout = 2,
        Cross = 3
    };

    class RoadNetwork
    {
    public:
        RoadNetwork() = default;

        const std::unordered_map<int, RoadNode>& Nodes() const { return nodes; }
        const std::unordered_map<int, RoadSegment>& Segments() const { return segments; }

        std::optional<RoadNode> TryGetRandomExitNode(const std::vector<int>& excludeNodeIds = {}) const;

        RoadNode& AddNode(const Vec3& position, const Vec2i& sector);
        RoadNode& AddNodeWithId(int id, const Vec3& position, const Vec2i& sector, EntryExitType role = EntryExitType::None);
        bool RemoveNode(int nodeId);

        RoadSegment& AddSegment(int startNodeId, int endNodeId, const std::vector<Vec3>& controlPoints, float width, int lanes, const Vec2i& sector);
        RoadSegment& AddSegmentWithId(int id, int startNodeId, int endNodeId, const std::vector<Vec3>& controlPoints, float width, int lanes, const Vec2i& sector);
        bool RemoveSegment(int segmentId);

        bool TryGetNode(int nodeId, RoadNode*& node);
        bool TryGetSegment(int segmentId, RoadSegment*& segment);
        bool TryGetNode(int nodeId, const RoadNode*& node) const;
        bool TryGetSegment(int segmentId, const RoadSegment*& segment) const;

        std::vector<int> FindPathAStar(int startNodeId, int endNodeId) const;

        void AddEntryPoint(int nodeId);
        void RemoveEntryPoint(int nodeId);
        void AddExitPoint(int nodeId);
        void RemoveExitPoint(int nodeId);
        void ClearEntryExitRoles(int nodeId);
        void SetEntryExitRole(int nodeId, EntryExitType role);

        RoadNode& SplitSegment(int segmentId, const Vec3& point, int segmentIndex, std::function<void(RoadSegment&)> onSegmentCreated = nullptr);

        std::vector<Vec3> BuildLanePath(const std::vector<int>& nodePath) const;

    private:
        std::unordered_map<int, RoadNode> nodes;
        std::unordered_map<int, RoadSegment> segments;
        int nextNodeId = 1;
        int nextSegmentId = 1;

        std::vector<RoadSegment::LaneDefinition> CloneLaneDefinitions(const std::vector<RoadSegment::LaneDefinition>& src) const;
        std::vector<std::pair<int, float>> GetReachableNeighbors(int nodeId) const;
        float GetLaneLength(const RoadSegment& segment, int laneIndex) const;
        float HeuristicCost(int nodeId, int targetNodeId) const;
        int GetLowestFScore(const std::vector<int>& openList, const std::unordered_map<int, float>& fScore) const;
        std::vector<int> ReconstructPath(const std::unordered_map<int, int>& cameFrom, int current) const;
        bool TryBuildHop(int fromNodeId, int toNodeId, std::tuple<const RoadSegment*, int, int, int, std::vector<Vec3>>& hop) const;
        LaneConnector* FindConnector(int nodeId, const RoadSegment* incomingSeg, int incomingLaneIndex, const RoadSegment* outgoingSeg, int outgoingLaneIndex) const;
        void AppendPoints(std::vector<Vec3>& destination, const std::vector<Vec3>& source) const;
    };

    class IntersectionRoutingBuilder
    {
    public:
        static std::vector<LaneConnection> BuildConnections(const RoadNode& node, const RoadNetwork& network, IntersectionType type = IntersectionType::Star);
        static std::vector<LaneConnection> BuildStarConnections(const RoadNode& node, const RoadNetwork& network);
        static std::vector<LaneConnector> BuildConnectors(const RoadNode& node, const RoadNetwork& network, IntersectionType type = IntersectionType::Star, float curveStrength = 0.35f, int samples = 8);

    private:
        static int CombineLaneId(int segmentId, int laneIndex);
        static std::optional<std::pair<Vec3, Vec3>> GetLaneEndpointData(int nodeId, const RoadSegment& seg, int laneIndex, bool incoming);
        static std::vector<Vec3> SampleBezier(const Vec3& p0, const Vec3& p1, const Vec3& p2, const Vec3& p3, int samples);
    };
} // namespace citybuilder::roads

