using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoadConfig
{
    public bool AffixToTerrain = false;
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
        }
        else
        {
            for (int i = 0, j = 0; i < controlPoints.Length; i++, j += 2)
            {
                Vector3 pointOnTerrain = controlPoints[i] + transform.position; // Adjust for object position
                float terrainHeight = terrain.SampleHeight(pointOnTerrain);
                pointOnTerrain.y = terrainHeight; // Set y to terrain height

                Vector3 forward = Vector3.forward;
                // Your existing direction and width calculations here...

                Vector3 left = new Vector3(-forward.z, 0, forward.x);
                vertices[j] = pointOnTerrain + left * roadWidth * 0.5f; // Adjust left vertex
                vertices[j + 1] = pointOnTerrain - left * roadWidth * 0.5f; // Adjust right vertex

                // Your existing triangle and UV setup here...
            }
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

        CalculateVertices();

        if (config.AffixToTerrain && Terrain.activeTerrain != null)
        {
            AffixToTerrain(Terrain.activeTerrain);
        }

        // Set mesh properties and rebuild colliders
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        BuildColliders();
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
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (IsExistingEndCollider(child))
            {
                childrenToDestroy.Add(child.gameObject);
            }
        }
        foreach (GameObject child in childrenToDestroy)
        {
            DestroyImmediate(child);
        }
        AddEndColliders();
    }
    void AddEndColliders()
    {
        if (controlPoints.Length < 2) return; // Ensure there are enough points to define the road
        if (endColliderPrefab == null)
        {
            Debug.Log("Collider not found for Road!");
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
}
