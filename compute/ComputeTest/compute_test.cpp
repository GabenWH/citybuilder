#include <chrono>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <vector>

// Simple 1D lane-cell movement stress test (CPU).
// Agents attempt to move forward if next cell is free.
// This is not a physically correct traffic model; it's a throughput benchmark.

struct Config
{
    uint32_t agentCount = 500000;
    uint32_t cellCount = 1000000;
    uint32_t ticks = 300;
};

static bool ParseU32(const char* arg, uint32_t& out)
{
    if (!arg) return false;
    char* end = nullptr;
    unsigned long v = std::strtoul(arg, &end, 10);
    if (end == arg) return false;
    out = static_cast<uint32_t>(v);
    return true;
}

int main(int argc, char** argv)
{
    Config cfg;
    for (int i = 1; i < argc; i++)
    {
        if (std::strcmp(argv[i], "--agents") == 0 && i + 1 < argc)
        {
            ParseU32(argv[++i], cfg.agentCount);
        }
        else if (std::strcmp(argv[i], "--cells") == 0 && i + 1 < argc)
        {
            ParseU32(argv[++i], cfg.cellCount);
        }
        else if (std::strcmp(argv[i], "--ticks") == 0 && i + 1 < argc)
        {
            ParseU32(argv[++i], cfg.ticks);
        }
        else if (std::strcmp(argv[i], "--help") == 0)
        {
            std::printf("Usage: compute_test [--agents N] [--cells N] [--ticks N]\n");
            return 0;
        }
    }

    if (cfg.cellCount <= cfg.agentCount)
    {
        cfg.cellCount = cfg.agentCount + 1;
    }

    std::vector<uint32_t> agentCell(cfg.agentCount);
    std::vector<uint32_t> currOcc(cfg.cellCount, 0);
    std::vector<uint32_t> nextOcc(cfg.cellCount, 0);

    for (uint32_t i = 0; i < cfg.agentCount; i++)
    {
        agentCell[i] = i;
        currOcc[i] = i + 1;
    }

    auto start = std::chrono::high_resolution_clock::now();
    for (uint32_t t = 0; t < cfg.ticks; t++)
    {
        std::fill(nextOcc.begin(), nextOcc.end(), 0);

        for (uint32_t a = 0; a < cfg.agentCount; a++)
        {
            uint32_t cell = agentCell[a];
            uint32_t next = cell + 1;
            if (next >= cfg.cellCount) next = 0;

            if (nextOcc[next] == 0)
            {
                nextOcc[next] = a + 1;
                agentCell[a] = next;
            }
            else if (nextOcc[cell] == 0)
            {
                nextOcc[cell] = a + 1;
                agentCell[a] = cell;
            }
            else
            {
                agentCell[a] = cell;
            }
        }

        currOcc.swap(nextOcc);
    }
    auto end = std::chrono::high_resolution_clock::now();

    double ms = std::chrono::duration<double, std::milli>(end - start).count();
    double updates = static_cast<double>(cfg.agentCount) * static_cast<double>(cfg.ticks);
    double mups = updates / (ms * 1000.0);

    std::printf("agents=%u cells=%u ticks=%u\n", cfg.agentCount, cfg.cellCount, cfg.ticks);
    std::printf("elapsed_ms=%.2f updates=%.0f MUPS=%.2f\n", ms, updates, mups);

    return 0;
}
