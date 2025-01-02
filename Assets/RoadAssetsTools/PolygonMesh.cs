using UnityEngine;
using System.Collections.Generic;
using TessMesh = LibTessDotNet.Mesh;
using Mesh = UnityEngine.Mesh;
using LibTessDotNet;
using System.Linq;

public class PolygonMesh : MonoBehaviour
{
    public List<Vector3> vertices = new List<Vector3>();
    private int[] triangles;
    private Vector3[] meshVertices;
    void Start()
    {
    }

    
    public void OrderVerticesByAngle()
    {
        // Calculate angles and sort vertices based on angle
        vertices = vertices
            .OrderBy(v => Mathf.Atan2(v.z, v.x)) // Sort by angle from the center
            .ToList();
    }
    void OnDrawGizmos()
    {
        if(meshVertices!= null){
        for (int i = 0; i < meshVertices.Length; i++)
        {
            Gizmos.DrawWireSphere(meshVertices[i]+transform.position, 0.05f); // Adjust the radius as needed
        }}
    }
    public void ReplaceVertices(List<Vector3> Vertices)
    {
        vertices = Vertices;
        CreateMesh();
    }
    public void AddVertices(List<Vector3> Vertices)
    {

    }
    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        DestroyImmediate(meshFilter);
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }


        // Use LibTessDotNet to triangulate the unordered vertices
        Tess tess = new Tess();
        ContourVertex[] contour = new ContourVertex[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            contour[i].Position = new Vec3(vertices[i].x, vertices[i].y, vertices[i].z);
        }
        tess.AddContour(contour);
        tess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

        // Retrieve vertices and triangles
        meshVertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < tess.Vertices.Length; i++)
        {
            meshVertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, tess.Vertices[i].Position.Z);
        }

        // Define triangles (assuming the polygon is convex and vertices are ordered clockwise)

        triangles = new int[tess.ElementCount * 3];
        for (int i = 0; i < tess.ElementCount; i++)
        {
            triangles[i * 3 + 0] = tess.Elements[i * 3 + 0];
            triangles[i * 3 + 1] = tess.Elements[i * 3 + 2];
            triangles[i * 3 + 2] = tess.Elements[i * 3 + 1];
        }

        // Assign vertices and triangles to the mesh
        mesh.vertices = meshVertices;
        mesh.triangles = triangles;

        // Recalculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Apply the mesh to the MeshFilter
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            // Add a MeshRenderer to render the mesh
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
    public float FindShortestDistanceAcrossPolygon()
    {
        if (vertices == null || vertices.Count < 3)
        {
            Debug.LogError("Polygon must have at least 3 vertices.");
            return 0f;
        }

        float minDistance = float.MaxValue;

        // Iterate through each edge of the polygon
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 start = vertices[i];
            Vector3 end = vertices[(i + 1) % vertices.Count];

            // Get the direction vector of the edge
            Vector3 edgeDir = (end - start).normalized;

            // Create a perpendicular vector to the edge direction on the XZ plane
            Vector3 perpendicularDir = Vector3.Cross(edgeDir, Vector3.up).normalized;

            // Find distances between this edge and other vertices
            foreach (var vertex in vertices)
            {
                if (vertex != start && vertex != end)
                {
                    float distance = Mathf.Abs(Vector3.Dot(vertex - start, perpendicularDir));
                    minDistance = Mathf.Min(minDistance, distance);
                }
            }
        }

        return minDistance;
    }
}
