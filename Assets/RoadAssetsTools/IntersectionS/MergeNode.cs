using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public enum MergeBehaviour
{
    Default,
    RoundAbout,
    SinglePoint,
    TStyle
}
public class MergeNode : MonoBehaviour
{
    // Enum for lane generation behavior options


    // Selected merge behavior from the inspector
    public MergeBehaviour mergeBehaviour;
    public Intersection intersection;
    
    public List<Node> nodes = new List<Node>();

    public List<Lane> mergeLanes = new List<Lane>();

    // Delegate for lane generation behavior that takes nodes as parameters
    public Func<List<Node>, List<MergeNode>, PolygonMesh, List<Lane>> GenerateLaneBehaviour { get; set; }

    void Start()
    {
        // Assign the selected behavior based on inspector choice
        switch (mergeBehaviour)
        {
            case MergeBehaviour.RoundAbout:
                GenerateLaneBehaviour = RoundAboutLaneBehaviour;
                break;
            case MergeBehaviour.SinglePoint:
                GenerateLaneBehaviour = SinglePointLaneBehaviour;
                break;
            case MergeBehaviour.TStyle:
                GenerateLaneBehaviour = TStyleLaneBehaviour;
                break;
            case MergeBehaviour.Default:
            default:
                GenerateLaneBehaviour = DefaultGenerateLaneBehaviour;
                break;
        }
    }

    void Update()
    {
        // Update logic if needed
    }
public Vector3 GetGeometricCenterLinq(List<Node> nodes)
{
    // Filter out null entries, then apply Aggregate
    var validNodes = nodes.Where(n => n != null).ToList();
    if (!validNodes.Any())
    {
        Debug.LogWarning("No valid nodes found, returning Vector3.zero.");
        return Vector3.zero;
    }

    Vector3 sum = validNodes.Aggregate(Vector3.zero,
        (currentSum, node) => currentSum + node.transform.position);

    return sum / validNodes.Count;
}
    // Example of a default lane generation behavior
    private List<Lane> DefaultGenerateLaneBehaviour(List<Node> nodes, List<MergeNode> mergeNodes, PolygonMesh tarmac)
    {
        List<Lane> lanes = new List<Lane>();
        // Add logic for generating lanes for a default merge behavior using nodes
        return lanes;
    }

    // Another example behavior that could be assigned at runtime
    private List<Lane> RoundAboutLaneBehaviour(List<Node> nodes, List<MergeNode> mergeNodes, PolygonMesh tarmac)
    {
        List<Lane> lanes = new List<Lane>();
        // Add logic for generating lanes for a roundabout using nodes
        return lanes;
    }

    private List<Lane> SinglePointLaneBehaviour(List<Node> nodes, List<MergeNode> mergeNodes, PolygonMesh tarmac)
    {
        List<Lane> lanes = new List<Lane>();
        // Add logic for generating lanes for a single-point merge using nodes
        return lanes;
    }

    private List<Lane> TStyleLaneBehaviour(List<Node> nodes, List<MergeNode> mergeNodes, PolygonMesh tarmac)
    {
        List<Lane> lanes = new List<Lane>();
        // Add logic for generating lanes for a T-style merge using nodes
        return lanes;
    }
}
