#include "road_network.hpp"
#include "road_network_loader.hpp"

#include <iostream>
#include <filesystem>

using namespace citybuilder::roads;

static void PrintPath(const std::vector<int>& nodes)
{
    std::cout << "Node path: ";
    for (size_t i = 0; i < nodes.size(); ++i)
    {
        if (i) std::cout << " -> ";
        std::cout << nodes[i];
    }
    std::cout << "\n";
}

static void PrintPoints(const std::vector<Vec3>& pts)
{
    std::cout << "Points (" << pts.size() << "):\n";
    for (size_t i = 0; i < pts.size(); ++i)
    {
        const auto& p = pts[i];
        std::cout << "  [" << i << "] (" << p.x << ", " << p.y << ", " << p.z << ")\n";
    }
}

int main(int argc, char** argv)
{
    std::string filePath = "network.json";
    if (argc > 1 && argv != nullptr)
    {
        filePath = argv[1];
    }

    RoadNetwork net;

    // If a JSON file exists in the working directory (or provided path), load it.
    if (std::filesystem::exists(filePath))
    {
        std::string error;
        if (!LoadRoadNetworkFromJsonFile(filePath, net, error))
        {
            std::cerr << "Failed to load " << filePath << ": " << error << "\n";
            return 1;
        }
        std::cout << "Loaded " << filePath << "\n";
    }
    else
    {
        // Simple "L" shaped road: 1 -> 2 -> 3.
        auto& a = net.AddNode({0.0f, 0.0f, 0.0f}, {0, 0});
        auto& b = net.AddNode({10.0f, 0.0f, 0.0f}, {0, 0});
        auto& c = net.AddNode({10.0f, 0.0f, 10.0f}, {0, 0});

        // Two-lane road each segment with default forward lanes.
        net.AddSegment(a.Id(), b.Id(), {a.Position(), b.Position()}, 6.0f, 2, {0, 0});
        net.AddSegment(b.Id(), c.Id(), {b.Position(), c.Position()}, 6.0f, 2, {0, 0});

        // Mark entries/exits to exercise TryGetRandomExitNode.
        net.SetEntryExitRole(a.Id(), EntryExitType::Entry);
        net.SetEntryExitRole(c.Id(), EntryExitType::Exit);
    }

    auto path = net.FindPathAStar(1, 3);
    PrintPath(path);

    auto lanePath = net.BuildLanePath(path);
    PrintPoints(lanePath);

    auto exitNode = net.TryGetRandomExitNode();
    if (exitNode)
    {
        std::cout << "Random exit node id: " << exitNode->Id() << "\n";
    }

    return 0;
}
