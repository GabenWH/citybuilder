using UnityEngine;
using System.Collections.Generic;

public enum LaneType
{
    Pedestrian,
    Car,
    Bicycle,
    EmergencyVehicle,
    TrashService,
    // Add other lane types as needed
}

public class Lane : MonoBehaviour
{
    public int laneIndex;
    public float width;
    public Vector3[] controlPoints; //LOCAL
    public (Vector3,int) output; //LOCAL Output for the lane
    public bool isBidirectional = false;
    public List<LaneActorAccessibility> actorAccessibilityList;
    public Dictionary<LaneType, float> actorAccessibility;
    public List<(Node,int)> nodes = new List<(Node,int)>();

    public void Initialize(int index, float laneWidth, Vector3[] parentControlPoints, float offset, bool isBi)
    {
        laneIndex = index;
        width = laneWidth;
        actorAccessibility = new Dictionary<LaneType, float>();
        isBidirectional = isBi;

        InitializeActorAccessibility();
        UpdateControlPoints(parentControlPoints, offset);
    }
    public void Initialize(int index, float laneWidth, Vector3[] parentControlPoints, float offset)
    {
        laneIndex = index;
        width = laneWidth;
        actorAccessibility = new Dictionary<LaneType, float>();
        InitializeActorAccessibility();
        UpdateControlPoints(parentControlPoints, offset);
    }

    private void InitializeActorAccessibility()
    {
        // Initialize default accessibility values
        actorAccessibility[LaneType.Pedestrian] = 0.1f;         // Near-zero usability
        actorAccessibility[LaneType.Car] = 0.1f;                // Near-zero usability
        actorAccessibility[LaneType.Bicycle] = 0.1f;            // Near-zero usability
        actorAccessibility[LaneType.EmergencyVehicle] = 0.1f;   // Near-zero usability
        actorAccessibility[LaneType.TrashService] = 0.1f;       // Near-zero usability
        // Add other default values as needed
        actorAccessibility = new Dictionary<LaneType, float>();

        // Populate dictionary from list
        foreach (var item in actorAccessibilityList)
        {
            actorAccessibility[item.actorType] = item.accessibility;
        }
    }

    public void UpdateActorAccessibility(LaneType actorType, float accessibility)
    {
        actorAccessibility[actorType] = accessibility;
    }

    public virtual void UpdateControlPoints(Vector3[] parentControlPoints, float offset)
    {
        controlPoints = new Vector3[parentControlPoints.Length];

        for (int i = 0; i < parentControlPoints.Length; i++)
        {
            Vector3 direction = (i == parentControlPoints.Length - 1)
                ? (parentControlPoints[i] - parentControlPoints[i - 1]).normalized
                : (parentControlPoints[i + 1] - parentControlPoints[i]).normalized;

            Vector3 left = new Vector3(-direction.z, 0, direction.x) * offset;
            controlPoints[i] = parentControlPoints[i] + left;
        }
        nodes.RemoveAll(tuple=>tuple.Item1==null);
        foreach(var node in nodes){
            node.Item1.gameObject.transform.position = controlPoints[node.Item2]+transform.parent.position;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (controlPoints == null || controlPoints.Length < 2)
            return;

        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(controlPoints[i]+transform.parent.position, controlPoints[i + 1]+transform.parent.position);
        }
    }

public Lane Clone()
{
    Lane clonedLane = new GameObject(this.gameObject.name).AddComponent<Lane>();
    clonedLane.laneIndex = this.laneIndex;
    clonedLane.width = this.width;
    clonedLane.controlPoints = (Vector3[])this.controlPoints.Clone();
    clonedLane.isBidirectional = this.isBidirectional;

    // Deep copy of actorAccessibilityList
    clonedLane.actorAccessibilityList = new List<LaneActorAccessibility>();
    foreach (var accessibility in this.actorAccessibilityList)
    {
        clonedLane.actorAccessibilityList.Add(new LaneActorAccessibility
        {
            actorType = accessibility.actorType,
            accessibility = accessibility.accessibility
        });
    }
    return clonedLane;
}

}