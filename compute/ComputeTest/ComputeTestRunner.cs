using System.Diagnostics;
using UnityEngine;

public class ComputeTestRunner : MonoBehaviour
{
    [SerializeField] private ComputeShader compute;
    [SerializeField] private int agentCount = 500000;
    [SerializeField] private int cellCount = 1000000;
    [SerializeField] private int ticksPerFrame = 1;
    [SerializeField] private bool logEverySecond = true;

    private ComputeBuffer agentCellBuffer;
    private ComputeBuffer currOccBuffer;
    private ComputeBuffer nextOccBuffer;

    private int clearKernel;
    private int stepKernel;
    private Stopwatch stopwatch;
    private float logTimer;

    private void OnEnable()
    {
        if (compute == null)
        {
            UnityEngine.Debug.LogError("Compute shader not assigned.");
            enabled = false;
            return;
        }

        agentCount = Mathf.Max(1, agentCount);
        cellCount = Mathf.Max(agentCount + 1, cellCount);

        agentCellBuffer = new ComputeBuffer(agentCount, sizeof(uint));
        currOccBuffer = new ComputeBuffer(cellCount, sizeof(uint));
        nextOccBuffer = new ComputeBuffer(cellCount, sizeof(uint));

        var agentCells = new uint[agentCount];
        var occ = new uint[cellCount];
        for (int i = 0; i < agentCount; i++)
        {
            agentCells[i] = (uint)i;
            occ[i] = (uint)(i + 1);
        }
        agentCellBuffer.SetData(agentCells);
        currOccBuffer.SetData(occ);
        nextOccBuffer.SetData(new uint[cellCount]);

        clearKernel = compute.FindKernel("ClearNext");
        stepKernel = compute.FindKernel("StepAgents");

        compute.SetInt("_CellCount", cellCount);
        compute.SetInt("_AgentCount", agentCount);

        compute.SetBuffer(clearKernel, "NextOcc", nextOccBuffer);
        compute.SetBuffer(stepKernel, "AgentCell", agentCellBuffer);
        compute.SetBuffer(stepKernel, "CurrOcc", currOccBuffer);
        compute.SetBuffer(stepKernel, "NextOcc", nextOccBuffer);

        stopwatch = Stopwatch.StartNew();
    }

    private void Update()
    {
        if (compute == null) return;

        int clearGroups = Mathf.CeilToInt(cellCount / 256f);
        int stepGroups = Mathf.CeilToInt(agentCount / 256f);

        for (int t = 0; t < ticksPerFrame; t++)
        {
            compute.Dispatch(clearKernel, clearGroups, 1, 1);
            compute.Dispatch(stepKernel, stepGroups, 1, 1);

            // Swap buffers: next becomes current.
            var tmp = currOccBuffer;
            currOccBuffer = nextOccBuffer;
            nextOccBuffer = tmp;

            compute.SetBuffer(stepKernel, "CurrOcc", currOccBuffer);
            compute.SetBuffer(stepKernel, "NextOcc", nextOccBuffer);
            compute.SetBuffer(clearKernel, "NextOcc", nextOccBuffer);
        }

        if (logEverySecond)
        {
            logTimer += Time.deltaTime;
            if (logTimer >= 1f)
            {
                logTimer = 0f;
                long ms = stopwatch.ElapsedMilliseconds;
                UnityEngine.Debug.Log($"ComputeTest: agents={agentCount} cells={cellCount} ticks/frame={ticksPerFrame} elapsed_ms={ms}");
            }
        }
    }

    private void OnDisable()
    {
        agentCellBuffer?.Release();
        currOccBuffer?.Release();
        nextOccBuffer?.Release();
    }
}
