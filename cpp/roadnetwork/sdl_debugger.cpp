#include "road_network.hpp"
#include "road_network_loader.hpp"
#include "road_sim.hpp"

#include <SDL.h>
#include <algorithm>
#include <atomic>
#include <chrono>
#include <filesystem>
#include <iostream>
#include <mutex>
#include <optional>
#include <queue>
#include <string>
#include <thread>
#include <vector>

using namespace citybuilder::roads;

struct Command
{
    std::string type;
    std::vector<std::string> args;
};

static std::queue<Command> g_commands;
static std::mutex g_commandMutex;
static std::atomic<bool> g_running{true};

static void CommandThread()
{
    std::string line;
    while (g_running && std::getline(std::cin, line))
    {
        if (line.empty()) continue;
        Command cmd;
        size_t pos = 0;
        while (pos < line.size())
        {
            while (pos < line.size() && isspace(static_cast<unsigned char>(line[pos]))) ++pos;
            size_t start = pos;
            while (pos < line.size() && !isspace(static_cast<unsigned char>(line[pos]))) ++pos;
            if (start == pos) break;
            std::string token = line.substr(start, pos - start);
            if (cmd.type.empty())
            {
                cmd.type = token;
            }
            else
            {
                cmd.args.push_back(token);
            }
        }
        if (!cmd.type.empty())
        {
            std::lock_guard<std::mutex> lock(g_commandMutex);
            g_commands.push(cmd);
        }
    }
}

struct Viewport
{
    float minX = -10.0f;
    float maxX = 10.0f;
    float minZ = -10.0f;
    float maxZ = 10.0f;

    void Fit(const RoadNetwork& net)
    {
        bool has = false;
        for (const auto& kv : net.Nodes())
        {
            const auto& p = kv.second.Position();
            if (!has)
            {
                minX = maxX = p.x;
                minZ = maxZ = p.z;
                has = true;
            }
            else
            {
                minX = std::min(minX, p.x);
                maxX = std::max(maxX, p.x);
                minZ = std::min(minZ, p.z);
                maxZ = std::max(maxZ, p.z);
            }
        }
        if (!has)
        {
            minX = -10.0f;
            maxX = 10.0f;
            minZ = -10.0f;
            maxZ = 10.0f;
        }
        float padX = (maxX - minX) * 0.1f + 1.0f;
        float padZ = (maxZ - minZ) * 0.1f + 1.0f;
        minX -= padX;
        maxX += padX;
        minZ -= padZ;
        maxZ += padZ;
    }

    SDL_FPoint ToScreen(const Vec3& p, int width, int height) const
    {
        float nx = (p.x - minX) / (maxX - minX);
        float nz = (p.z - minZ) / (maxZ - minZ);
        // Flip z to screen y (top-down)
        float sx = nx * width;
        float sy = (1.0f - nz) * height;
        return SDL_FPoint{sx, sy};
    }
};

static void DrawCircle(SDL_Renderer* r, float cx, float cy, float radius, SDL_Color color)
{
    SDL_SetRenderDrawColor(r, color.r, color.g, color.b, color.a);
    int rad = static_cast<int>(radius);
    for (int dx = -rad; dx <= rad; ++dx)
    {
        for (int dy = -rad; dy <= rad; ++dy)
        {
            if (dx * dx + dy * dy <= rad * rad)
            {
                SDL_RenderDrawPointF(r, cx + dx, cy + dy);
            }
        }
    }
}

static void DrawSegment(SDL_Renderer* r, const std::vector<Vec3>& pts, const Viewport& vp, int w, int h, SDL_Color color)
{
    SDL_SetRenderDrawColor(r, color.r, color.g, color.b, color.a);
    for (size_t i = 1; i < pts.size(); ++i)
    {
        auto a = vp.ToScreen(pts[i - 1], w, h);
        auto b = vp.ToScreen(pts[i], w, h);
        SDL_RenderDrawLineF(r, a.x, a.y, b.x, b.y);
    }
}

int main(int argc, char** argv)
{
    std::string jsonPath = "network.json";
    if (argc > 1 && argv) jsonPath = argv[1];

    RoadNetwork net;
    {
        std::string err;
        if (!LoadRoadNetworkFromJsonFile(jsonPath, net, err))
        {
            std::cerr << "Failed to load " << jsonPath << ": " << err << "\n";
            return 1;
        }
    }

    RoadSimulation sim(net);

    if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS) != 0)
    {
        std::cerr << "SDL_Init failed: " << SDL_GetError() << "\n";
        return 1;
    }

    SDL_Window* window = SDL_CreateWindow("RoadNetwork Debugger", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, 900, 700, SDL_WINDOW_SHOWN);
    if (!window)
    {
        std::cerr << "SDL_CreateWindow failed: " << SDL_GetError() << "\n";
        SDL_Quit();
        return 1;
    }

    SDL_Renderer* renderer = SDL_CreateRenderer(window, -1, SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC);
    if (!renderer)
    {
        std::cerr << "SDL_CreateRenderer failed: " << SDL_GetError() << "\n";
        SDL_DestroyWindow(window);
        SDL_Quit();
        return 1;
    }

    Viewport vp;
    vp.Fit(net);

    std::thread cmdThread(CommandThread);

    auto last = std::chrono::steady_clock::now();
    bool quit = false;

    std::cout << "Commands: spawn <startNode> <endNode> [speed], clear, quit\n";

    while (!quit)
    {
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            if (e.type == SDL_QUIT)
            {
                quit = true;
            }
            else if (e.type == SDL_KEYDOWN)
            {
                if (e.key.keysym.sym == SDLK_ESCAPE) quit = true;
            }
        }

        {
            std::lock_guard<std::mutex> lock(g_commandMutex);
            while (!g_commands.empty())
            {
                auto cmd = g_commands.front();
                g_commands.pop();
                if (cmd.type == "quit" || cmd.type == "exit")
                {
                    quit = true;
                }
                else if (cmd.type == "spawn" && cmd.args.size() >= 2)
                {
                    int start = std::stoi(cmd.args[0]);
                    int end = std::stoi(cmd.args[1]);
                    float speed = cmd.args.size() >= 3 ? std::stof(cmd.args[2]) : 5.0f;
                    int id = sim.SpawnAgent(start, end, speed);
                    std::cout << (id > 0 ? "Spawned agent " + std::to_string(id) : "Failed to spawn agent") << "\n";
                }
                else if (cmd.type == "clear")
                {
                    sim.ClearFinished();
                    std::cout << "Cleared finished agents\n";
                }
            }
        }

        auto now = std::chrono::steady_clock::now();
        float dt = std::chrono::duration<float>(now - last).count();
        last = now;
        sim.Update(dt);

        int w, h;
        SDL_GetRendererOutputSize(renderer, &w, &h);
        SDL_SetRenderDrawColor(renderer, 15, 15, 18, 255);
        SDL_RenderClear(renderer);

        SDL_Color segmentColor{80, 150, 220, 255};
        SDL_Color nodeColor{220, 220, 220, 255};
        SDL_Color agentColor{250, 120, 80, 255};

        // Draw segments (use control points)
        for (const auto& kv : net.Segments())
        {
            DrawSegment(renderer, kv.second.ControlPoints(), vp, w, h, segmentColor);
        }

        // Draw nodes
        for (const auto& kv : net.Nodes())
        {
            auto s = vp.ToScreen(kv.second.Position(), w, h);
            DrawCircle(renderer, s.x, s.y, 4.0f, nodeColor);
        }

        // Draw agents
        for (const auto& agent : sim.Agents())
        {
            auto p = agent.Position();
            auto s = vp.ToScreen(p, w, h);
            DrawCircle(renderer, s.x, s.y, 5.0f, agentColor);
        }

        SDL_RenderPresent(renderer);
    }

    g_running = false;
    if (cmdThread.joinable()) cmdThread.join();
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
    return 0;
}

