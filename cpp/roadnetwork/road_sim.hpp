#pragma once

#include "road_network.hpp"

#include <deque>
#include <mutex>
#include <optional>
#include <string>
#include <vector>

namespace citybuilder::roads
{
    struct RoadAgent
    {
        int id = 0;
        int startNodeId = -1;
        int endNodeId = -1;
        float speed = 5.0f; // units per second
        float distance = 0.0f;
        std::vector<int> nodePath;
        std::vector<Vec3> lanePath;
        bool finished = false;

        Vec3 Position() const;
    };

    class RoadSimulation
    {
    public:
        explicit RoadSimulation(RoadNetwork& network);

        // Spawn an agent; returns agent id or -1 on failure.
        int SpawnAgent(int startNodeId, int endNodeId, float speed = 5.0f);

        void Update(float deltaTime);
        const std::vector<RoadAgent>& Agents() const { return agents; }
        void ClearFinished();

    private:
        RoadNetwork& network;
        std::vector<RoadAgent> agents;
        int nextAgentId = 1;
    };
} // namespace citybuilder::roads

