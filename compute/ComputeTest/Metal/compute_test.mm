#import <Foundation/Foundation.h>
#import <Metal/Metal.h>

#include <chrono>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <vector>

struct Config
{
    uint32_t agentCount = 500000;
    uint32_t cellCount = 1000000;
    uint32_t ticks = 60;
    float accel = 0.2f;
    float decel = 0.5f;
    float maxSpeed = 1.0f;
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

static bool ParseF32(const char* arg, float& out)
{
    if (!arg) return false;
    char* end = nullptr;
    float v = std::strtof(arg, &end);
    if (end == arg) return false;
    out = v;
    return true;
}

int main(int argc, char** argv)
{
    @autoreleasepool
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
            else if (std::strcmp(argv[i], "--accel") == 0 && i + 1 < argc)
            {
                ParseF32(argv[++i], cfg.accel);
            }
            else if (std::strcmp(argv[i], "--decel") == 0 && i + 1 < argc)
            {
                ParseF32(argv[++i], cfg.decel);
            }
            else if (std::strcmp(argv[i], "--maxSpeed") == 0 && i + 1 < argc)
            {
                ParseF32(argv[++i], cfg.maxSpeed);
            }
            else if (std::strcmp(argv[i], "--help") == 0)
            {
                std::printf("Usage: compute_test_metal [--agents N] [--cells N] [--ticks N] [--accel F] [--decel F] [--maxSpeed F]\n");
                return 0;
            }
        }

        if (cfg.cellCount <= cfg.agentCount)
        {
            cfg.cellCount = cfg.agentCount + 1;
        }

        id<MTLDevice> device = MTLCreateSystemDefaultDevice();
        if (!device)
        {
            std::fprintf(stderr, "Metal device not available.\n");
            return 1;
        }

        NSError* error = nil;
        NSString* libPath = @"compute/ComputeTest/Metal/compute_test.metallib";
        id<MTLLibrary> library = [device newLibraryWithFile:libPath error:&error];
        if (!library)
        {
            std::fprintf(stderr, "Failed to load metallib: %s\n", error.localizedDescription.UTF8String);
            return 1;
        }

        id<MTLFunction> clearFn = [library newFunctionWithName:@"ClearNext"];
        id<MTLFunction> stepFn = [library newFunctionWithName:@"StepAgentsCollision"];
        if (!clearFn || !stepFn)
        {
            std::fprintf(stderr, "Failed to load compute functions.\n");
            return 1;
        }

        id<MTLComputePipelineState> clearPSO = [device newComputePipelineStateWithFunction:clearFn error:&error];
        id<MTLComputePipelineState> stepPSO = [device newComputePipelineStateWithFunction:stepFn error:&error];
        if (!clearPSO || !stepPSO)
        {
            std::fprintf(stderr, "Failed to create pipeline: %s\n", error.localizedDescription.UTF8String);
            return 1;
        }

        id<MTLCommandQueue> queue = [device newCommandQueue];
        if (!queue)
        {
            std::fprintf(stderr, "Failed to create command queue.\n");
            return 1;
        }

        std::vector<uint32_t> agentCells(cfg.agentCount);
        std::vector<float> agentSpeeds(cfg.agentCount, 0.0f);
        std::vector<uint32_t> currOcc(cfg.cellCount, 0);
        for (uint32_t i = 0; i < cfg.agentCount; i++)
        {
            agentCells[i] = i;
            currOcc[i] = i + 1;
        }

        id<MTLBuffer> agentCellBuf = [device newBufferWithBytes:agentCells.data()
                                                        length:agentCells.size() * sizeof(uint32_t)
                                                       options:MTLResourceStorageModeShared];
        id<MTLBuffer> agentSpeedBuf = [device newBufferWithBytes:agentSpeeds.data()
                                                         length:agentSpeeds.size() * sizeof(float)
                                                        options:MTLResourceStorageModeShared];
        id<MTLBuffer> currOccBuf = [device newBufferWithBytes:currOcc.data()
                                                      length:currOcc.size() * sizeof(uint32_t)
                                                     options:MTLResourceStorageModeShared];
        id<MTLBuffer> nextOccBuf = [device newBufferWithLength:currOcc.size() * sizeof(uint32_t)
                                                       options:MTLResourceStorageModeShared];

        uint32_t cellCount = cfg.cellCount;
        uint32_t agentCount = cfg.agentCount;
        float accel = cfg.accel;
        float decel = cfg.decel;
        float maxSpeed = cfg.maxSpeed;

        auto start = std::chrono::high_resolution_clock::now();

        id<MTLCommandBuffer> cmd = [queue commandBuffer];
        for (uint32_t t = 0; t < cfg.ticks; t++)
        {
            // ClearNext
            id<MTLComputeCommandEncoder> enc = [cmd computeCommandEncoder];
            [enc setComputePipelineState:clearPSO];
            [enc setBuffer:nextOccBuf offset:0 atIndex:0];
            [enc setBytes:&cellCount length:sizeof(uint32_t) atIndex:1];

            MTLSize gridClear = MTLSizeMake(cellCount, 1, 1);
            NSUInteger tgClear = clearPSO.maxTotalThreadsPerThreadgroup;
            if (tgClear > 256) tgClear = 256;
            MTLSize tgroupClear = MTLSizeMake(tgClear, 1, 1);
            [enc dispatchThreads:gridClear threadsPerThreadgroup:tgroupClear];
            [enc endEncoding];

            // StepAgentsCollision
            enc = [cmd computeCommandEncoder];
            [enc setComputePipelineState:stepPSO];
            [enc setBuffer:agentCellBuf offset:0 atIndex:0];
            [enc setBuffer:agentSpeedBuf offset:0 atIndex:1];
            [enc setBuffer:currOccBuf offset:0 atIndex:2];
            [enc setBuffer:nextOccBuf offset:0 atIndex:3];
            [enc setBytes:&cellCount length:sizeof(uint32_t) atIndex:4];
            [enc setBytes:&agentCount length:sizeof(uint32_t) atIndex:5];
            [enc setBytes:&accel length:sizeof(float) atIndex:6];
            [enc setBytes:&decel length:sizeof(float) atIndex:7];
            [enc setBytes:&maxSpeed length:sizeof(float) atIndex:8];

            MTLSize gridStep = MTLSizeMake(agentCount, 1, 1);
            NSUInteger tgStep = stepPSO.maxTotalThreadsPerThreadgroup;
            if (tgStep > 256) tgStep = 256;
            MTLSize tgroupStep = MTLSizeMake(tgStep, 1, 1);
            [enc dispatchThreads:gridStep threadsPerThreadgroup:tgroupStep];
            [enc endEncoding];

            // Swap occupancy buffers for next tick.
            id<MTLBuffer> tmp = currOccBuf;
            currOccBuf = nextOccBuf;
            nextOccBuf = tmp;
        }

        [cmd commit];
        [cmd waitUntilCompleted];

        auto end = std::chrono::high_resolution_clock::now();
        double ms = std::chrono::duration<double, std::milli>(end - start).count();
        double updates = static_cast<double>(cfg.agentCount) * static_cast<double>(cfg.ticks);
        double mups = updates / (ms * 1000.0);

        std::printf("agents=%u cells=%u ticks=%u\n", cfg.agentCount, cfg.cellCount, cfg.ticks);
        std::printf("elapsed_ms=%.2f updates=%.0f MUPS=%.2f\n", ms, updates, mups);

        return 0;
    }
}
