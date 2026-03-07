#include "road_network_loader.hpp"
#include "simple_json.hpp"

#include <fstream>
#include <sstream>

namespace citybuilder::roads
{
    namespace
    {
        bool ReadFileToString(const std::string& path, std::string& out, std::string& errorMessage)
        {
            std::ifstream file(path);
            if (!file.is_open())
            {
                errorMessage = "Failed to open file: " + path;
                return false;
            }
            std::ostringstream ss;
            ss << file.rdbuf();
            out = ss.str();
            return true;
        }

        float AsFloat(const simple_json::JsonValue* v, float fallback = 0.0f)
        {
            if (!v) return fallback;
            if (v->IsNumber()) return static_cast<float>(v->number);
            return fallback;
        }

        int AsInt(const simple_json::JsonValue* v, int fallback = 0)
        {
            if (!v) return fallback;
            if (v->IsNumber()) return static_cast<int>(v->number);
            return fallback;
        }

        std::string AsString(const simple_json::JsonValue* v, const std::string& fallback = "")
        {
            if (!v) return fallback;
            if (v->IsString()) return v->string;
            return fallback;
        }

        Vec3 ParseVec3(const simple_json::JsonValue* obj)
        {
            if (!obj || !obj->IsObject()) return {};
            return Vec3{
                AsFloat(obj->Find("x")),
                AsFloat(obj->Find("y")),
                AsFloat(obj->Find("z"))};
        }

        Vec2i ParseVec2i(const simple_json::JsonValue* obj)
        {
            if (!obj || !obj->IsObject()) return {};
            return Vec2i{
                AsInt(obj->Find("x")),
                AsInt(obj->Find("y"))};
        }

        bool LoadNodes(const simple_json::JsonValue& root, RoadNetwork& network, std::string& errorMessage)
        {
            const auto* nodesVal = root.Find("nodes");
            if (!nodesVal || !nodesVal->IsArray())
            {
                errorMessage = "Missing or invalid 'nodes' array";
                return false;
            }

            for (const auto& n : nodesVal->array)
            {
                if (!n.IsObject())
                {
                    errorMessage = "Node entry is not an object";
                    return false;
                }
                int id = AsInt(n.Find("id"), -1);
                Vec3 pos = ParseVec3(n.Find("position"));
                Vec2i sector = ParseVec2i(n.Find("sector"));
                int roleInt = AsInt(n.Find("role"), 0);
                EntryExitType role = static_cast<EntryExitType>(roleInt);
                network.AddNodeWithId(id, pos, sector, role);
            }
            return true;
        }

        bool LoadSegments(const simple_json::JsonValue& root, RoadNetwork& network, std::string& errorMessage)
        {
            const auto* segVal = root.Find("segments");
            if (!segVal || !segVal->IsArray())
            {
                errorMessage = "Missing or invalid 'segments' array";
                return false;
            }

            for (const auto& s : segVal->array)
            {
                if (!s.IsObject())
                {
                    errorMessage = "Segment entry is not an object";
                    return false;
                }
                int id = AsInt(s.Find("id"), -1);
                int startNodeId = AsInt(s.Find("startNodeId"), -1);
                int endNodeId = AsInt(s.Find("endNodeId"), -1);
                float width = AsFloat(s.Find("width"), 4.0f);
                int lanes = AsInt(s.Find("lanes"), 1);
                Vec2i sector = ParseVec2i(s.Find("sector"));

                std::vector<Vec3> controlPoints;
                const auto* cps = s.Find("controlPoints");
                if (cps && cps->IsArray())
                {
                    for (const auto& cp : cps->array)
                    {
                        controlPoints.push_back(ParseVec3(&cp));
                    }
                }

                if (controlPoints.size() < 2)
                {
                    errorMessage = "Segment " + std::to_string(id) + " has fewer than 2 control points";
                    return false;
                }

                RoadSegment& seg = network.AddSegmentWithId(id, startNodeId, endNodeId, controlPoints, width, lanes, sector);

                const auto* laneDefs = s.Find("laneDefinitions");
                if (laneDefs && laneDefs->IsArray() && !laneDefs->array.empty())
                {
                    std::vector<RoadSegment::LaneDefinition> defs;
                    for (const auto& ld : laneDefs->array)
                    {
                        if (!ld.IsObject()) continue;
                        RoadSegment::LaneDefinition def;
                        def.offset = AsFloat(ld.Find("Offset"));
                        def.width = AsFloat(ld.Find("Width"));
                        def.forward = ld.Find("Forward") ? ld.Find("Forward")->boolean : true;
                        def.type = AsString(ld.Find("Type"));
                        defs.push_back(def);
                    }
                    if (!defs.empty())
                    {
                        seg.SetLaneDefinitions(defs);
                    }
                }
            }

            return true;
        }
    } // namespace

    bool LoadRoadNetworkFromJsonFile(const std::string& path, RoadNetwork& network, std::string& errorMessage)
    {
        std::string content;
        if (!ReadFileToString(path, content, errorMessage))
        {
            return false;
        }

        auto parsed = simple_json::Parse(content);
        if (!parsed.value)
        {
            errorMessage = parsed.error;
            return false;
        }
        if (!parsed.value->IsObject())
        {
            errorMessage = "Root JSON value must be an object";
            return false;
        }

        if (!LoadNodes(*parsed.value, network, errorMessage))
        {
            return false;
        }
        if (!LoadSegments(*parsed.value, network, errorMessage))
        {
            return false;
        }

        return true;
    }
} // namespace citybuilder::roads

