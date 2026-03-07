# ComputeTest

This folder holds two minimal stress tests for 1D lane-cell movement:
- A Unity compute-shader version (GPU).
- A standalone C++ version (CPU).

Everything below is documented with code blocks, per request.

## Unity Compute Test (GPU)

Unity only compiles scripts and shaders under `Assets/`. To run this test in Unity:
```
# Option A: move the folder
mv compute/ComputeTest Assets/ComputeTest

# Option B: symlink (macOS)
ln -s "$(pwd)/compute/ComputeTest" Assets/ComputeTest
```

Add the component and assign the shader:
```
1) Create an empty GameObject.
2) Add `ComputeTestRunner` as a component.
3) Assign `ComputeTest.compute` to the ComputeShader field.
4) Press Play.
```

Behavior:
```
- Initializes a lane with one agent per cell for the first N agents.
- Each tick, agents attempt to move to the next cell if it is free.
- Uses a simple atomic claim on the next occupancy buffer.
```

Files:
```
compute/ComputeTest/ComputeTest.compute
compute/ComputeTest/ComputeTestRunner.cs
```

## Unity Compute Test with Collisions (GPU)

Add the component and assign the shader:
```
1) Create an empty GameObject.
2) Add `ComputeTestRunnerCollisions` as a component.
3) Assign `ComputeTestCollisions.compute` to the ComputeShader field.
4) Press Play.
```

Behavior:
```
- Each agent has a float speed.
- If the next cell is occupied, the agent decelerates.
- If the next cell is free, the agent accelerates up to a max speed.
- Agents move at most one cell per tick when speed >= 0.5.
- Occupancy uses atomics to prevent overlap (simple collision rule).
```

Files:
```
compute/ComputeTest/ComputeTestCollisions.compute
compute/ComputeTest/ComputeTestRunnerCollisions.cs
```

## C++ Compute Test (CPU)

Build:
```
c++ -O2 -std=c++17 compute/ComputeTest/compute_test.cpp -o compute/ComputeTest/compute_test
```

Run:
```
./compute/ComputeTest/compute_test --agents 500000 --cells 1000000 --ticks 60
```

Arguments:
```
--agents N   Number of agents
--cells N    Number of lane cells (must be > agents)
--ticks N    Number of simulation ticks
```

Behavior:
```
- Each agent occupies a single cell.
- Agents try to move forward by one cell each tick.
- If next cell is occupied, they try to stay in place.
- No intersections or rerouting; this is a throughput baseline.
```

Output:
```
agents=500000 cells=1000000 ticks=60
elapsed_ms=91.91 updates=30000000 MUPS=326.40
```

Notes:
```
- This is an optimistic baseline (no contention, no intersections).
- It is not a full traffic model; it is a stress test for raw updates.
```

## Metal Compute Test (GPU, standalone)

See:
```
compute/ComputeTest/Metal/README.md
```
