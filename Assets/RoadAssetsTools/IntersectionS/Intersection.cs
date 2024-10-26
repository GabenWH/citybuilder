using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
public class Intersection : Connectable
{
    public List<Node> nodes = new List<Node>();
    public List<Road> connectedRoads = new List<Road>();
    public List<Intersection> connectedIntersections = new List<Intersection>();
    public float detectionRadius = 5.0f;
    public GameObject intersectionTarmac;

    public override void Connect(Connectable other){
        throw new NotImplementedException();
    }
    public override void Disconnect(Connectable other){
        throw new NotImplementedException();
    }
    public override float getWidth(){
        return detectionRadius;
    }
    public void AttachIntersection(Intersection intersection){
        if(connectedIntersections.Contains(intersection)){
            connectedIntersections.Remove(intersection);
        }
        
    }
    public void AttachRoad(Road road)
    {
        if (connectedRoads.Contains(road))
        {
            connectedRoads.Remove(road);
            foreach (var lane in road.lanes)
            {
                Node selectedNode = nodes.FirstOrDefault(node => node.lane == lane);
                if(selectedNode != null)
                {
                    Debug.Log(selectedNode.gameObject.name + "Removed");
                    nodes.Remove(selectedNode);
                    DestroyImmediate(selectedNode.gameObject);
                }
            }
        }
        AttachNodesToRoad(road);
        BuildIntersectionTarmac(SimpleIntersectionStyle);
    }
    public void AttachToNearbyRoads()
    {
        List<Vector3> intersectionVertices = new List<Vector3>();
        // Detect all road endpoints within a certain radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<RoadCollider>(out RoadCollider roadCollider))
            {
                var road = roadCollider.road;
                if (road != null && !connectedRoads.Contains(road))
                {
                    AttachNodesToRoad(road);
                }
            }
        }
        BuildIntersectionTarmac(SimpleIntersectionStyle);

    }
    private void AttachNodesToRoad(Road road)
    {
        foreach (var lane in road.lanes)
        {
            var pointsWithDistance = lane.controlPoints
            .Select(p => new { Point = p, Distance = Vector3.Distance(transform.position, p + lane.transform.position) })
            .ToList();

            pointsWithDistance.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            Vector3[] sortedPoints = pointsWithDistance.Select(p => p.Point).ToArray();
            GameObject node = new GameObject(road.gameObject.name + " " + lane.gameObject.name + " Node");
            var nodeComponent = node.AddComponent<Node>();
            nodeComponent.lane = lane;
            if (lane.isBidirectional)
            {
                nodeComponent.input = true;
                nodeComponent.output = true;
            }
            else if (sortedPoints[0] == lane.output.Item1)
            {
                nodeComponent.output = true;
            }
            else
            {
                nodeComponent.input = true;
            }
            node.transform.position = sortedPoints[0] + road.transform.position;
            node.transform.parent = transform;
            nodes.Add(nodeComponent);
            lane.nodes.Add((nodeComponent, FindClosestControlPointIndex(sortedPoints[0], lane.controlPoints)));
        }

        if (Vector3.Distance(transform.position, road.controlPoints[0] + road.transform.position) < detectionRadius)
        {
            connectedRoads.Add(road);
            road.ConnectToIntersection(this, true);  // Connect start of the road
        }
        else
        if (Vector3.Distance(transform.position, road.transform.position + road.controlPoints[road.controlPoints.Length - 1]) < detectionRadius)
        {
            connectedRoads.Add(road);
            road.ConnectToIntersection(this, false);  // Connect end of the road
        }
    }
    public void BuildIntersectionTarmac(Func<List<Road>, Transform, List<Vector3>> buildVerticesFunc)
    {
        DestroyImmediate(intersectionTarmac);
        intersectionTarmac = new GameObject("Intersection Tarmac");
        intersectionTarmac.transform.parent = transform;
        intersectionTarmac.transform.localPosition = Vector3.zero;
        intersectionTarmac.transform.localRotation = Quaternion.identity;
        intersectionTarmac.transform.localScale = Vector3.one;

        // Use the provided function to generate the vertices
        List<Vector3> intersectionVertices = buildVerticesFunc(connectedRoads, this.transform);

        var mesh = intersectionTarmac.AddComponent<PolygonMesh>();
        mesh.vertices.Clear();
        foreach (var vert in intersectionVertices)
        {
            mesh.vertices.Add(vert);
        }
        mesh.OrderVerticesByAngle();
        mesh.CreateMesh();
    }
    public List<Vector3> SimpleIntersectionStyle(List<Road> roads, Transform intersectionTransform)
    {
        List<Vector3> vertices = new List<Vector3>();

        foreach (var road in roads)
        {
            var verts = road.FindClosestVertices(intersectionTransform.position, detectionRadius);
            if(verts.Count == 0 || verts.Count == 1)continue;
            Vector3 worldSpaceVertex = road.transform.TransformPoint(verts[0]);
            Vector3 localSpaceVertex = intersectionTransform.InverseTransformPoint(worldSpaceVertex);
            vertices.Add(localSpaceVertex);

            worldSpaceVertex = road.transform.TransformPoint(verts[1]);
            localSpaceVertex = intersectionTransform.InverseTransformPoint(worldSpaceVertex);
            vertices.Add(localSpaceVertex);
        }

        return vertices;
    }
    void OnDrawGizmosSelected()
    {
        // Visualize the detection area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
    void OnDrawGizmos()
    {
        // Highlight the main intersection point
        Gizmos.color = Color.green;  // Using green to distinguish the main intersection
        Gizmos.DrawWireSphere(transform.position, detectionRadius / 2);  // Larger sphere for main intersection
    }

    int FindClosestControlPointIndex(Vector3 position, Vector3[] controlPoints)
    {
        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < controlPoints.Length; i++)
        {
            float distance = Vector3.Distance(position, controlPoints[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
