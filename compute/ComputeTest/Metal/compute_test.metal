#include <metal_stdlib>
using namespace metal;

kernel void ClearNext(device uint* nextOcc [[buffer(0)]],
                      constant uint& cellCount [[buffer(1)]],
                      uint id [[thread_position_in_grid]])
{
    if (id >= cellCount) return;
    nextOcc[id] = 0;
}

kernel void StepAgentsCollision(device uint* agentCell [[buffer(0)]],
                                device float* agentSpeed [[buffer(1)]],
                                device uint* currOcc [[buffer(2)]],
                                device uint* nextOcc [[buffer(3)]],
                                constant uint& cellCount [[buffer(4)]],
                                constant uint& agentCount [[buffer(5)]],
                                constant float& accel [[buffer(6)]],
                                constant float& decel [[buffer(7)]],
                                constant float& maxSpeed [[buffer(8)]],
                                uint id [[thread_position_in_grid]])
{
    if (id >= agentCount) return;

    uint cell = agentCell[id];
    uint next = cell + 1;
    if (next >= cellCount) next = 0;

    float speed = agentSpeed[id];
    if (currOcc[next] != 0)
    {
        speed = max(0.0f, speed - decel);
    }
    else
    {
        speed = min(maxSpeed, speed + accel);
    }
    agentSpeed[id] = speed;

    uint target = (speed >= 0.5f) ? next : cell;

    // Atomic claim on nextOcc[target]. Use 0 as free, agentId+1 as claimed.
    atomic_uint* slot = reinterpret_cast<device atomic_uint*>(&nextOcc[target]);
    uint expected = 0;
    if (atomic_compare_exchange_weak_explicit(slot, &expected, id + 1,
                                              memory_order_relaxed, memory_order_relaxed))
    {
        agentCell[id] = target;
        return;
    }

    // Fall back to current cell.
    slot = reinterpret_cast<device atomic_uint*>(&nextOcc[cell]);
    expected = 0;
    atomic_compare_exchange_weak_explicit(slot, &expected, id + 1,
                                          memory_order_relaxed, memory_order_relaxed);
    agentCell[id] = cell;
}
