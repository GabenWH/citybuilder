using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
[ExecuteInEditMode]
public class BuildingGenerator : MonoBehaviour
{
    public TextAsset geoJsonFile;
    public GameObject BuildingLayout;

    void Start()
    {

    }

    public void LoadBuildingData(string json)
    {
        // Use a reverse loop to safely remove all children
        for (int i = BuildingLayout.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(BuildingLayout.transform.GetChild(i).gameObject);
        }
        JObject featureCollection = JObject.Parse(json);
        JArray jsonObjects = (JArray)featureCollection["features"]; // Assume top-level array
        foreach (JObject building in jsonObjects)
        {
            JObject properties = (JObject)building["properties"];
            JObject geometry = (JObject)building["geometry"];
            if (!properties.ContainsKey("building")) continue;

            double height = properties.ContainsKey("height") ? (double)properties["height"] : 10; // Default height if not specified
            string name = properties.ContainsKey("name") ? (string)properties["name"] : "Generic Building";
            var built = CreateBuilding(geometry, height, name, properties);

            built.transform.SetParent(BuildingLayout.transform);
        }
    }

    GameObject CreateBuilding(JObject geometry, double height, string name, JObject properties)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        JArray coordinates = (JArray)geometry["coordinates"][0]; // Assuming first array is the outer boundary

        for (int i = 0; i < coordinates.Count; i++)
        {
            JArray coord = (JArray)coordinates[i];
            float x = (float)coord[0];
            float z = (float)coord[1];
            vertices.Add(new Vector3(x, 0, z)); // Ground vertices
            vertices.Add(new Vector3(x, (float)height, z)); // Roof vertices
        }

        int n = coordinates.Count;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;

            // Correcting side triangles to ensure they are counter-clockwise
            indices.Add(i * 2);
            indices.Add(i * 2 + 1);
            indices.Add(next * 2);

            indices.Add(i * 2 + 1);
            indices.Add(next * 2 + 1);
            indices.Add(next * 2);

            // Bottom cap triangles, ensuring they are counter-clockwise
            if (i > 1)
            {
                indices.Add(0);
                indices.Add(next * 2);
                indices.Add(i * 2);
            }
        }

        // Top (roof) cap triangles, ensuring they are counter-clockwise
        for (int i = 2; i < n; i++)
        {
            indices.Add(1);
            indices.Add(i * 2 + 1);
            indices.Add((i - 1) * 2 + 1);
        }

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();

        var building = new GameObject(name);
        var meshFilter = building.AddComponent<MeshFilter>();
        var meshRenderer = building.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        var buildingComp = building.AddComponent<Building>();
        buildingComp.properties = properties;
        buildingComp.mesh = mesh;

        meshRenderer.material = new Material(Shader.Find("Standard"));
        return building;
    }
}