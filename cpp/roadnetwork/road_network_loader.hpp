#pragma once

#include "road_network.hpp"

#include <string>

namespace citybuilder::roads
{
    // Load a road network from a JSON file exported from Unity's RoadNetworkData (JsonUtility-friendly).
    // Returns true on success; errorMessage is populated on failure.
    bool LoadRoadNetworkFromJsonFile(const std::string& path, RoadNetwork& network, std::string& errorMessage);
}

