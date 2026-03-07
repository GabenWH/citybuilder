#include "road_sim.hpp"

#include <algorithm>

namespace citybuilder::roads
{
    Vec3 RoadAgent::Position() const
    {
        if (lanePath.empty()) return {};
        // Walk along the polyline until we reach the distance.
        float remaining = distance;
        for (size_t i = 1; i < lanePath.size(); ++i)
        {
            Vec3 a = lanePath[i - 1];
            Vec3 b = lanePath[i];
            float segLen = Distance(a, b);
            if (segLen <= 0.0001f) continue;
            if (remaining <= segLen)
            {
                float t = remaining / segLen;
                return a + (b - a) * t;
            }
            remaining -= segLen;
        }
        return lanePath.back();
    }

    RoadSimulation::RoadSimulation(RoadNetwork& network)
        : network(network)
    {
    }

    int RoadSimulation::SpawnAgent(int startNodeId, int endNodeId, float speed)
    {
        if (!network.Nodes().count(startNodeId) || !network.Nodes().count(endNodeId))
        {
            return -1;
        }
        auto nodePath = network.FindPathAStar(startNodeId, endNodeId);
        if (nodePath.size() < 2)
        {
            return -1;
        }
        auto lanePath = network.BuildLanePath(nodePath);
        if (lanePath.size() < 2)
        {
            return -1;
        }

        RoadAgent agent;
        agent.id = nextAgentId++;
        agent.startNodeId = startNodeId;
        agent.endNodeId = endNodeId;
        agent.speed = speed;
        agent.nodePath = std::move(nodePath);
        agent.lanePath = std::move(lanePath);
        agent.distance = 0.0f;
        agent.finished = false;
        agents.push_back(std::move(agent));
        return agents.back().id;
    }

    void RoadSimulation::Update(float deltaTime)
    {
        for (auto& agent : agents)
        {
            if (agent.finished) continue;
            // Advance distance.
            agent.distance += agent.speed * deltaTime;
            // Check if beyond path length.
            float totalLen = 0.0f;
            for (size_t i = 1; i < agent.lanePath.size(); ++i)
            {
                totalLen += Distance(agent.lanePath[i - 1], agent.lanePath[i]);
            }
            if (agent.distance >= totalLen)
            {
                agent.distance = totalLen;
                agent.finished = true;
            }
        }
    }

    void RoadSimulation::ClearFinished()
    {
        agents.erase(std::remove_if(agents.begin(), agents.end(), [](const RoadAgent& a) { return a.finished; }), agents.end());
    }
} // namespace citybuilder::roads

