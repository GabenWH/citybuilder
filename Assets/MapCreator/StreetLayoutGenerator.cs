using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
[ExecuteInEditMode]
public class StreetLayoutGenerator : MonoBehaviour
{


    [SerializeField]
    public TextAsset geoJsonFile;
    public TextAsset intersectionFile;
    public Terrain topography;
    private string intersectionData = string.Empty;
    private string geoJsonData = string.Empty;
    public Vector2 referencePoint = new Vector2(-124.080f, 40.870f);
    public double sizeConversion = 111320; //controls output size;
    public GameObject RoadLayout;

    void OnEnable()
    {
        EnsureRoadLayoutExists();
    }

    private void EnsureRoadLayoutExists()
    {
        if (RoadLayout == null)
        {
            RoadLayout = new GameObject("RoadLayout");
            RoadLayout.transform.parent = this.transform;  // Set as child of this GameObject
            RoadLayout.transform.localPosition = Vector3.zero;  // Optionally, position it at the parent's origin
        }
    }

    public void CreateIntersections(string geoJsonData){
        GeoJson intersectionData = LoadGeoJson(geoJsonData);
        foreach(Feature feature in intersectionData.features){
            Vector3[] intersection = ConvertCoordinatesToVector3(feature.geometry);
            GameObject intersectionMarker = new GameObject("intersection");
            intersectionMarker.AddComponent<Intersection>();
            intersection[0].y = topography.SampleHeight(intersection[0])+topography.transform.position.y;
            intersectionMarker.transform.position = intersection[0];
        } 
    }
    // Example method to create a street layout
    public void CreateStreets(string geoJsonData)
    {
        // Parse the GeoJSON data
        GeoJson myGeoJson = LoadGeoJson(geoJsonData);
        if (RoadLayout == this.gameObject)
        {
            // Use a reverse loop to safely remove all children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        foreach (Feature feature in myGeoJson.features)
        {
            //if linestring converting to points
            /*
            if (feature.geometry.type == "LineString")
            {
            */
            Vector3[] streetPoints = ConvertCoordinatesToVector3(feature.geometry);
            string name = "Generic Street";
            if (feature.properties.ContainsKey("name"))
                name = (string)feature.properties["name"];
            else if (feature.properties.ContainsKey("highway"))
                name = (string)feature.properties["highway"];
            GameObject streetLine = new GameObject(name);
            Road road = streetLine.AddComponent<Road>();
            road.controlPoints = streetPoints;

            //this gets replaced with something better eventually...
            if (feature.properties.ContainsKey("highway"))
            {
                if ((string)feature.properties["highway"] == "residential")
                {
                    road.roadWidth = 7.0f;
                }
                else
                {
                    road.roadWidth = 1.0f;
                }
            }
            else
            {
                road.roadWidth = 1.0f;
            }
            streetLine.transform.parent = RoadLayout.transform;

            road.BuildRoad();
            //}
        }
    }

    // Convert coordinates to Unity's Vector3 (simplified, assumes flat terrain)
    Vector3[] ConvertCoordinatesToVector3(double[][] coordinates)
    {
        // Check if the coordinates array is null or empty
        if (coordinates == null || coordinates.Length == 0)
        {
            Debug.LogError("Coordinates array is null or empty.");
            Debug.Log(coordinates);
            return new Vector3[0]; // Return an empty array to avoid further processing
        }
        Vector3[] vector3s = new Vector3[coordinates.Length];
        for (int i = 0; i < coordinates.Length; i++)
        {
            // Check if the sub-array is null or does not have at least 2 elements (for longitude and latitude)
            if (coordinates[i] == null || coordinates[i].Length < 2)
            {
                Debug.LogError($"Sub-array at index {i} is null or does not contain at least 2 elements.");
                continue; // Skip this iteration
            }

            // Assuming the first two elements are longitude and latitude, respectively
            // Note: You might need to adjust the order or calculation based on your coordinate system
            vector3s[i] = ConvertToVec3(coordinates[i][0], coordinates[i][1]);
        }
        return vector3s;
    }

    public Vector3 ConvertGeoCoordsToUnity(double longitude, double latitude)
    {
        // Calculate the relative position in meters
        double x = (longitude - referencePoint.x) * GetLongitudeToMeters(latitude);
        double z = (latitude - referencePoint.y) * 110574; // Approximation for meters per degree of latitude

        return new Vector3((float)x, 0, (float)z); // Assuming y is elevation, which you can adjust as needed
    }
    public Vector3 ConvertToVec3(double x, double z)
    {
        return new Vector3((float)x, 0, (float)z);
    }



    private double GetLongitudeToMeters(double latitude)
    {
        // Approximation for converting longitude degrees to meters at a given latitude
        return Math.Cos(latitude * Math.PI / 180) * sizeConversion;
    }

    // Define classes based on your GeoJSON structure
    [System.Serializable]
    public class GeoJson
    {
        public Feature[] features;
    }

    [System.Serializable]
    public class Feature
    {
        //public string type;
        [JsonConverter(typeof(SingleArrayToArrayOfArraysConverter))]
        public double[][] geometry;
        public Dictionary<string, object> properties; // To hold any additional metadata
    }

    [System.Serializable]
    public class Geometry
    {
        public string type;
        [JsonConverter(typeof(SingleArrayToArrayOfArraysConverter))]
        public double[][] coordinates;

    }
    public GeoJson LoadGeoJson(string geoJsonString)
    {
        GeoJson returnJson = null;
        try
        {
            returnJson = JsonConvert.DeserializeObject<GeoJson>(geoJsonString);
            // Process your geoJsonObject as needed
        }
        catch (JsonException e)
        {
            Debug.LogError($"JSON deserialization error: {e.Message}");
        }
        if (returnJson == null)
        {
            Debug.LogError("Something has gone wrong, check for json errors");
        }
        return returnJson;
    }

    //old line renderer
    /*
    public Line CreateLine(GameObject streetLine)
    {
        LineRenderer lineRenderer = streetLine.AddComponent<LineRenderer>();
        streetLine.transform.parent = RoadLayout.transform;

        // Configure the LineRenderer
        lineRenderer.positionCount = streetPoints.Length;
        lineRenderer.SetPositions(streetPoints);
        lineRenderer.material = new Material(Shader.Find("Standard"));
    }
    */
    public Mesh CreateLineMesh(Vector3[] points, float width)
    {
        Mesh mesh = new Mesh();
        var vertices = new List<Vector3>();
        var indices = new List<int>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            // Calculate direction from this point to the next
            Vector3 direction = (points[i + 1] - points[i]).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up) * width;

            vertices.Add(points[i] + side);  // Left vertex of the current segment
            vertices.Add(points[i] - side);  // Right vertex of the current segment
            vertices.Add(points[i + 1] + side);  // Left vertex of the next segment
            vertices.Add(points[i + 1] - side);  // Right vertex of the next segment

            int baseIndex = i * 4;
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals(); // Important for lighting

        return mesh;
    }
    public void AttachToTerrain(){
        foreach(Transform child in RoadLayout.transform){
            Road road = child.GetComponent<Road>();
            if(road!=null){
                RoadConfig config = new RoadConfig();
                config.AffixToTerrain = true;
                config.terrain = topography;
                road.BuildRoad(config);
            }
        }
    }
}

