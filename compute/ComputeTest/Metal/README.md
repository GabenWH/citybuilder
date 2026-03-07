# Metal Compute Test

This is a minimal Metal compute test for 1D lane-cell collision behavior.

## Build

Compile the shader and link the host:
```
# Compile .metal to .air
xcrun -sdk macosx metal -c compute/ComputeTest/Metal/compute_test.metal -o compute/ComputeTest/Metal/compute_test.air

# Link .air to .metallib
xcrun -sdk macosx metallib compute/ComputeTest/Metal/compute_test.air -o compute/ComputeTest/Metal/compute_test.metallib

# Build the Objective-C++ host
clang++ -O2 -std=c++17 compute/ComputeTest/Metal/compute_test.mm \
  -framework Metal -framework Foundation \
  -o compute/ComputeTest/Metal/compute_test_metal
```

## Run

```
./compute/ComputeTest/Metal/compute_test_metal --agents 500000 --cells 1000000 --ticks 60
```

Arguments:
```
--agents N
--cells N
--ticks N
--accel F
--decel F
--maxSpeed F
```

Notes:
```
- This is a throughput test, not a full traffic sim.
- Each agent checks only the next cell ahead.
- Collision rule is an atomic claim on the next cell.
```
