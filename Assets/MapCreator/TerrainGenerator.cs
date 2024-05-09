using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class TerrainGenerator : MonoBehaviour
{
    public Texture2D heightMap;
    private Terrain generatedTerrain;
    public Vector3 terrainSize; // Width, Height, Length in meters
    public TerrainJSONData terrain;
    public Texture2D defaultTexture;
    public Texture2D defaultNormalMap;
    public Material defaultMaterial;
    
    public float defaultTextureTileSize = 10f;
    

    void Start()
    {
    }

    public void LoadDownloadedTerrain(string jsonString)
    {
        terrain = JsonConvert.DeserializeObject<TerrainJSONData>(jsonString);
        terrainSize = new Vector3((float)(terrain.Width*terrain.PixelWidth),terrain.Height,(float)(terrain.PixelHeight*terrain.Length));
        //LoadHeightMap(terrain.heightMap);
    }
    private void LoadHeightMap(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        heightMap = new Texture2D(2, 2);
        heightMap.LoadImage(fileData);  // This replaces the texture size by the size of the image
    }
    public void GenerateTerrain()
    {
        if(generatedTerrain == null){
            generatedTerrain = this.GetComponent<Terrain>();
            if(generatedTerrain == null){
                generatedTerrain = this.gameObject.AddComponent<Terrain>();
            }
        }
        if(generatedTerrain.terrainData == null){
            generatedTerrain.terrainData = new TerrainData();
        }
        generatedTerrain.terrainData.size = terrainSize;
        float[,] heights = new float[heightMap.width,heightMap.height];
        for(int x = 0; x < heightMap.width; x++){for(int y = 0;y<heightMap.height;y++){
            Debug.Log("height["+ x+","+y+"]");
            heights[x,y]=heightMap.GetPixel(x,y).grayscale;
        }}
        generatedTerrain.terrainData.SetHeights(0,0,heights);

        // Check if the Terrain Collider is present
        TerrainCollider terrainCollider = gameObject.GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            // No Terrain Collider found, so add one
            terrainCollider = gameObject.AddComponent<TerrainCollider>();
            terrainCollider.terrainData = GetComponent<Terrain>().terrainData;
            Debug.Log("Terrain Collider was added dynamically.");
        }
        else
        {
            Debug.Log("Terrain Collider already exists.");
        }

        // Add Material
        if(generatedTerrain.materialTemplate==null && defaultMaterial!=null){
            generatedTerrain.materialTemplate = defaultMaterial;
        }
        // Check if there's a default texture
        if (generatedTerrain != null && defaultTexture != null)
        {
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = defaultTexture;
            layer.tileSize = new Vector2(defaultTextureTileSize, defaultTextureTileSize);

    
            
            if (defaultNormalMap != null)
            {
                layer.normalMapTexture = defaultNormalMap;
            }
            // Apply the layer to the terrain
            generatedTerrain.terrainData.terrainLayers = new TerrainLayer[] { layer };
            Debug.Log(generatedTerrain.terrainData.terrainLayers.Length);
        }
    }

    float[,] ConvertHeightMap(Texture2D heightMap)
    {
        float[,] heights = new float[heightMap.width, heightMap.height];
        for (int x = 0; x < heightMap.width; x++)
        {
            for (int y = 0; y < heightMap.height; y++)
            {
                // Normalize the height values between 0.0 and 1.0
                heights[x, y] = heightMap.GetPixel(x, y).grayscale;
            }
        }
        return heights;
    }
}
    public class TerrainJSONData{
        public int Height { get; set; }
        
        [JsonProperty("pixel_width")]
        public double PixelWidth { get; set; }
        [JsonProperty("pixel_height")]
        public double PixelHeight { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        [JsonProperty("half_side_length")]
        public float HalfSideLength { get; set; }
        public string heightMap { get; set; }
    }
