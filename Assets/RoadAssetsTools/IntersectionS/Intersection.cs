using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
public class Intersection : Connectable
{
    public List<Node> nodes = new List<Node>();
    public MergeNode mainNode;
    public MergeBehaviour intersectionStyle;
    public List<Road> connectedRoads = new List<Road>();
    public List<Intersection> connectedIntersections = new List<Intersection>();
    public float detectionRadius = 5.0f;
    public GameObject intersectionTarmac;

    public override void Connect(Connectable other)
    {
        Road road = other as Road;
        if (road != null){
            if(!connectedRoads.Contains(road)){
            connectedRoads.Add(road);
            }
            
            AttachNodesToRoad(road);
        }
        CheckNodes();
    }
    public override void Disconnect(Connectable other)
    {
        //throw new NotImplementedException();
        Road road = other as Road;
        if(road!=null&&nodes!=null){
            CheckNodes();
            var matchingNodes = nodes.Where(n => n.road == road).ToList();
            connectedRoads.Remove(road);
            foreach (var node in matchingNodes)
            {
                DestroyImmediate(node.gameObject);
            }
            CheckNodes();
            BuildMergeNode(intersectionStyle);
            BuildIntersectionTarmac(SimpleIntersectionStyle);
        }
    }
    public override void Check(Connectable other)
    {
        // Temporary list to store roads that are still within the detection radius
        List<Road> stillConnectedRoads = new List<Road>();
        // Perform an overlap sphere check to detect all colliders within the detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<RoadCollider>(out RoadCollider roadCollider))
            {
                var road = roadCollider.road;
                if (road != null && connectedRoads.Contains(road))
                {
                    // If this road was already connected and is still within the collision radius, keep it
                    stillConnectedRoads.Add(road);
                    road.Connect(this);
                }
            }
        }

        // Disconnect roads that are no longer within the detection radius
        foreach (var road in connectedRoads.ToList()) // Create a copy to safely modify during iteration
        {
            if (!stillConnectedRoads.Contains(road))
            {
                road.Disconnect(this); // Assuming `Disconnect` is a method to remove this connection from the road
                var nodesToRemove = nodes.Where(node=> node.road == road).ToList();
                nodes = nodes.Where(node=>node.road!=road).ToList();
                foreach(var node in nodesToRemove){
                    DestroyImmediate(node.gameObject);
                }

            }
        }

        // Update connected roads with only those still within the detection radius
        connectedRoads = stillConnectedRoads;

        // Reattach nearby roads if needed
        AttachToNearbyRoads();
        CheckNodes();
    }
    public void CheckNodes()
    {
        nodes.RemoveAll(n => n == null);

        List<Node> nodesInGameObject = gameObject.GetComponentsInChildren<Node>().ToList();
        
        // Loop through nodesInGameObject and delete the GameObjects of nodes not in intersection.nodes
        foreach (Node node in nodesInGameObject)
        {
            if (!nodes.Contains(node))
            {
                DestroyImmediate(node.gameObject); // Remove the GameObject from the scene
            }
        }
    }
    public override float getWidth()
    {
        return detectionRadius;
    }
    public void AttachIntersection(Intersection intersection)
    {
        if (connectedIntersections.Contains(intersection))
        {
            connectedIntersections.Remove(intersection);
        }

    }

    //Just to connect roads logic
    public void AttachRoad(Road road)
    {
        if (connectedRoads.Contains(road))
        {
            Disconnect(road);
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i].road == road)
                {
                    DestroyImmediate(nodes[i].gameObject);
                    nodes.RemoveAt(i);
                }
            }
        }
        Connect(road);
        //road.Connect(this);
        //connectedRoads.Add(road);
        AttachNodesToRoad(road);
        BuildMergeNode(intersectionStyle);
        BuildIntersectionTarmac(SimpleIntersectionStyle);
    }
    public void AttachToNearbyRoads()
    {
        List<Vector3> intersectionVertices = new List<Vector3>();
        // Detect all road endpoints within a certain radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        var newConnectedRoads = new List<Vector3>();
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<RoadCollider>(out RoadCollider roadCollider))
            {
                var road = roadCollider.road;
                
                if (road != null && !connectedRoads.Contains(road))
                {
                    Connect(road);
                    //road.Connect(this);
                }
            }
        }
        BuildMergeNode(intersectionStyle);
        BuildIntersectionTarmac(SimpleIntersectionStyle);
    }
    private void AttachNodesToRoad(Road road)
    {
                //current disconnect logic
                /*
        if (Vector3.Distance(transform.position, road.controlPoints[0] + road.transform.position) < detectionRadius)
        {
        }
        else
        if (Vector3.Distance(transform.position, road.transform.position + road.controlPoints[road.controlPoints.Length - 1]) < detectionRadius)
        {
        }
        else{
            Disconnect(road);
            road.Disconnect(this);
            return;
        }
        */
        var nodesToDelete = gameObject.GetComponentsInChildren<Node>().Where(node=> node.road==road);
        foreach (var node in nodesToDelete){
            DestroyImmediate(node.gameObject);
        }
        foreach (var lane in road.lanes)
        {
            var pointsWithDistance = lane.controlPoints
            .Select(p => new
            {
                Point = p,
                Distance = Vector3.Distance(

            transform.position, p + road.transform.position

            )
            }).ToList();

            pointsWithDistance.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            Vector3[] sortedPoints = pointsWithDistance.Select(p => p.Point).ToArray();
            GameObject node = new GameObject(road.gameObject.name + " " + lane.gameObject.name + " Node");
            var nodeComponent = node.AddComponent<Node>();
            nodeComponent.lane = lane;
            nodeComponent.road = road;
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

    }
    public void BuildMergeNode(MergeBehaviour behaviour)
    {
        PolygonMesh polygonMesh = intersectionTarmac.GetComponent<PolygonMesh>();
        float radius = polygonMesh.FindShortestDistanceAcrossPolygon() / 2;
        mainNode ??= GetComponent<MergeNode>() ?? new MergeNode();
        if (mainNode != null)
        {
            mainNode.mergeBehaviour = behaviour;
            mainNode.intersection = this;
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
            if (verts.Count == 0 || verts.Count == 1) continue;
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
