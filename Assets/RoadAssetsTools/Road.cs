using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoadConfig
{
    public bool AffixToTerrain = false;
    public Terrain terrain;
    public Material RoadMaterial;
    // Add more configuration options as needed
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Road : MonoBehaviour
{
    public Vector3[] controlPoints; // Control points defining the path of the road
    public float roadWidth = 8.0f; // Width of the road
    public int lanes = 2;
    public bool oneLane = false;
    public Material roadMaterial;
    public Intersection startIntersection;  // Intersection at the start of the road
    public Intersection endIntersection;
    public GameObject endColliderPrefab;
    private string colliderTag = "GenCollider";
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    public bool enableColliders = false;


    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        if (controlPoints == null || controlPoints.Length < 2)
            return;

        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 point = controlPoints[i] + this.transform.position;
            Vector3 nextPoint = controlPoints[i + 1] + this.transform.position;

            Vector3 direction = (nextPoint - point).normalized;
            Vector3 left = new Vector3(-direction.z, 0, direction.x) * roadWidth * 0.5f;

            // Draw the road segment
            Gizmos.DrawLine(point + left, nextPoint + left);
            Gizmos.DrawLine(point - left, nextPoint - left);
            Gizmos.DrawLine(point + left, point - left);
            Gizmos.DrawLine(nextPoint + left, nextPoint - left);
        }
    }
    void Start()
    {
        //BuildRoad();
    }
    public void AffixToTerrain(Terrain terrain)
    {
        if (terrain == null)
        {
            Debug.LogError("No Terrain!" + this.gameObject.name);
            return;
        }
        // Adjust only the y-coordinate of the control points based on the terrain height
        for (int i = 0; i < controlPoints.Length; i++)
        {
            // Compute the world position of each control point
            Vector3 worldPoint = transform.TransformPoint(controlPoints[i]);

            // Sample the height at the given world position
            float terrainHeight = terrain.SampleHeight(worldPoint) + terrain.transform.position.y;

            // Adjust only the y-component of the control points
            controlPoints[i].y = terrainHeight - transform.position.y; // Adjust for the object's local position
        }
    }
    public void BuildRoad()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        vertices = new Vector3[controlPoints.Length * 2];
        triangles = new int[(controlPoints.Length - 1) * 6];
        uvs = new Vector2[vertices.Length];

        // Apply the material
        mr.material = roadMaterial;

        // I'll remove this when I confirm I haven't regressioned, honestly I should just upload this to github and be done with it
        CalculateVertices();
        // Assign vertices, triangles, and uvs to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals(); // Recalculate normals for proper lighting
        mf.mesh = mesh;
        BuildColliders();
    }
    public void BuildRoad(RoadConfig config)
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        vertices = new Vector3[controlPoints.Length * 2];
        triangles = new int[(controlPoints.Length - 1) * 6];
        uvs = new Vector2[vertices.Length];

        mr.material = config.RoadMaterial ?? roadMaterial;

        if (config.AffixToTerrain && config.terrain != null)
        {
            AffixToTerrain(config.terrain);
        }
        CalculateVertices();
        // Set mesh properties and rebuild colliders
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        if (enableColliders) BuildColliders();
    }
    private void CalculateVertices()
    {
        for (int i = 0, j = 0; i < controlPoints.Length; i++, j += 2)
        {
            Vector3 forward = Vector3.forward;
            float centerWidth = roadWidth * 0.5f;
            if (i == 0)
                forward = (controlPoints[i + 1] - controlPoints[i]).normalized;
            else if (i < controlPoints.Length - 1)
                forward = (((controlPoints[i] - controlPoints[i - 1]) + (controlPoints[i + 1] - controlPoints[i]))).normalized;
            else
                forward = (controlPoints[i] - controlPoints[i - 1]).normalized;
            Vector3 left = new Vector3(-forward.z, 0, forward.x);
            vertices[j] = controlPoints[i] + left * centerWidth; // Left vertex
            vertices[j + 1] = controlPoints[i] - left * centerWidth; // Right vertex

            if (i < controlPoints.Length - 1)
            {
                int baseIndex = i * 6;
                triangles[baseIndex + 0] = j;
                triangles[baseIndex + 1] = j + 2;
                triangles[baseIndex + 2] = j + 1;
                triangles[baseIndex + 3] = j + 1;
                triangles[baseIndex + 4] = j + 2;
                triangles[baseIndex + 5] = j + 3;
            }

            uvs[j] = new Vector2(0, i);
            uvs[j + 1] = new Vector2(1, i);
        }
    }
    public bool IsExistingEndCollider(Transform obj)
    {
        return obj.CompareTag(colliderTag);
    }
    float CalculateIntersectionDistance(Vector3 A, Vector3 B, Vector3 C, float width)
    {
        Vector3 AB = (B - A).normalized;
        Vector3 BC = (C - B).normalized;

        // Calculate the angle in radians between AB and BC
        float angle = Vector3.Angle(AB, BC) * Mathf.Deg2Rad;

        // Calculate the intersection distance based on the sine of the angle
        float distance = width / Mathf.Sin(angle);

        return distance;
    }
    public void ConnectToIntersection(Intersection intersection, bool isStart)
    {
        if (isStart)
            startIntersection = intersection;
        else
            endIntersection = intersection;
    }
    public void BuildColliders()
    {
        RemoveAllColliders(); // Remove all existing colliders before building new ones

        // Create new colliders
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 point = controlPoints[i] + this.transform.position;
            Vector3 nextPoint = controlPoints[i + 1] + this.transform.position;

            Vector3 direction = (nextPoint - point).normalized;
            Vector3 left = new Vector3(-direction.z, 0, direction.x) * roadWidth * 0.5f;

            // Calculate the center and size of the collider
            Vector3 center = (point + nextPoint) / 2;
            Vector3 size = new Vector3(Vector3.Distance(point + left, point - left), 0.05f, Vector3.Distance(point, nextPoint));

            // Create a new child GameObject to hold the BoxCollider
            GameObject colliderObject = new GameObject("RoadCollider");
            colliderObject.transform.SetParent(this.transform);

            // Set the position and rotation of the collider object
            colliderObject.transform.position = center;
            colliderObject.transform.rotation = Quaternion.LookRotation(direction);

            // Add the BoxCollider to the new GameObject
            BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
            boxCollider.size = size;
            boxCollider.center = Vector3.zero; // Center is zero because the GameObject is already positioned correctly

            // Add the RoadCollider script to reference the Road script
            RoadCollider roadCollider = colliderObject.AddComponent<RoadCollider>();
            roadCollider.road = this;
        }

        AddEndColliders();
    }

    public void RemoveAllColliders()
    {
        // Destroy all child GameObjects holding the colliders
        foreach (Transform child in transform)
        {
            if (child.name == "RoadCollider")
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
    void AddEndColliders()
    {
        if (controlPoints.Length < 2) return; // Ensure there are enough points to define the road
        if (endColliderPrefab == null)
        {
            return;
        } // Ensure that theres actual colliders to provide

        // Add collider to the start of the road
        GameObject startCollider = Instantiate(endColliderPrefab, transform);
        startCollider.transform.localPosition = controlPoints[0]; // Position at the start point
        startCollider.transform.localScale = new Vector3(roadWidth, 1, 1); // Scale according to road width

        // Add collider to the end of the road
        GameObject endCollider = Instantiate(endColliderPrefab, transform);
        endCollider.transform.localPosition = controlPoints[controlPoints.Length - 1]; // Position at the end point
        endCollider.transform.localScale = new Vector3(roadWidth, 1, 1); // Scale according to road width


        // Add tags to both colliders
        endCollider.tag = colliderTag;
        startCollider.tag = colliderTag;
        // Optionally set rotations based on road orientation
        Vector3 roadDirectionStart = (controlPoints[1] - controlPoints[0]).normalized;
        startCollider.transform.forward = roadDirectionStart;

        Vector3 roadDirectionEnd = (controlPoints[controlPoints.Length - 1] - controlPoints[controlPoints.Length - 2]).normalized;
        endCollider.transform.forward = roadDirectionEnd;
    }
    public void SplitRoad(Vector3 splitPoint)
    {

        // Check if the split point is within the bounds of the road
        if (!IsPointWithinRoadBounds(splitPoint))
        {
            Debug.Log(splitPoint);
            Debug.LogWarning("Split point is not within the bounds of the road.");
            return;
        }
        else{
            Debug.Log("Within bounds, split proceeding");
        }
        int splitIndex = -1;
        float minDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;

        // Find the closest segment to the split point
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 segmentStart = controlPoints[i];
            Vector3 segmentEnd = controlPoints[i + 1];

            // Calculate the closest point on the segment to the split point
            Vector3 point = ClosestPointOnSegment(segmentStart, segmentEnd, splitPoint);

            float distance = Vector3.Distance(splitPoint, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                splitIndex = i;
                closestPoint = point;
            }
        }

        if (splitIndex == -1) return;

        // Calculate the direction of the road segment
        Vector3 segmentDirection = (controlPoints[splitIndex + 1] - controlPoints[splitIndex]).normalized;
        Vector3 roadLeft = new Vector3(-segmentDirection.z, 0, segmentDirection.x) * roadWidth * 0.5f;
        Vector3 newPoint1 = closestPoint + roadLeft;
        Vector3 newPoint2 = closestPoint - roadLeft;

        // Insert the split points into the control points array
        List<Vector3> newControlPoints = new List<Vector3>(controlPoints);
        newControlPoints.Insert(splitIndex + 1, closestPoint);
        controlPoints = newControlPoints.ToArray();

        // Create two sets of control points
        List<Vector3> controlPoints1 = new List<Vector3>();
        List<Vector3> controlPoints2 = new List<Vector3>();

        for (int i = 0; i <= splitIndex + 1; i++)
        {
            controlPoints1.Add(controlPoints[i]);
        }

        for (int i = splitIndex + 1; i < controlPoints.Length; i++)
        {
            controlPoints2.Add(controlPoints[i]);
        }

        // Create new road segments
        CreateNewRoadSegment(controlPoints1.ToArray(), transform.gameObject.name + "(segment)");
        CreateNewRoadSegment(controlPoints2.ToArray(), transform.gameObject.name + "(segment)");

        // Optionally, destroy the original road segment if needed
        Destroy(this.gameObject);
    }

private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
{
    // Ignore the y axis by projecting onto the XZ plane
    Vector3 aXZ = new Vector3(a.x, 0, a.z);
    Vector3 bXZ = new Vector3(b.x, 0, b.z);
    Vector3 pXZ = new Vector3(p.x, 0, p.z);

    Vector3 ab = bXZ - aXZ; // Direction vector from point A to point B
    float t = Vector3.Dot(pXZ - aXZ, ab) / Vector3.Dot(ab, ab); // Project point P onto the line defined by A and B, normalize to the segment length
    t = Mathf.Clamp01(t); // Clamp t to the range [0, 1] to ensure the point is within the segment
    Vector3 closestPointXZ = aXZ + t * ab; // Calculate the closest point on the segment in XZ plane

    // Return the closest point with the original y value of p
    return new Vector3(closestPointXZ.x, p.y, closestPointXZ.z);
}


    private void CreateNewRoadSegment(Vector3[] newControlPoints, string segmentName)
    {
        GameObject newRoadObject = new GameObject(segmentName);
        newRoadObject.transform.position = this.transform.position; // Ensure new segment is positioned correctly
        Road newRoad = newRoadObject.AddComponent<Road>();
        newRoad.controlPoints = newControlPoints;
        newRoad.roadWidth = this.roadWidth;
        newRoad.lanes = this.lanes;
        newRoad.oneLane = this.oneLane;
        newRoad.roadMaterial = this.roadMaterial;
        newRoad.endColliderPrefab = this.endColliderPrefab;

        newRoad.BuildRoad();
    }

    private bool IsPointWithinRoadBounds(Vector3 point)
    {
        // Check if the point is within the bounds of the road's control points with width consideration
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            if (IsPointWithinSegmentWidth(controlPoints[i]+transform.position, controlPoints[i + 1]+transform.position, point))
            {
                return true;
            }
        }
        return false;
    }

private bool IsPointWithinSegmentWidth(Vector3 a, Vector3 b, Vector3 p)
{
    Vector3 closestPoint = ClosestPointOnSegment(a, b, p);

    // Check if the closest point is exactly at a or b
    if (Vector3.Distance(closestPoint, a) < Mathf.Epsilon || Vector3.Distance(closestPoint, b) < Mathf.Epsilon)
    {
        return false;
    }

    // Calculate the distance in the XZ plane
    float distance = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(closestPoint.x, 0, closestPoint.z));
    return distance <= roadWidth * 0.5f;
}
}