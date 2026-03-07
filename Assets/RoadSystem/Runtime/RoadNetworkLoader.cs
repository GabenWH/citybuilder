using System.IO;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Loads/Saves a RoadNetworkRuntime to a JSON file (portable, Unity-agnostic).
    /// </summary>
    public class RoadNetworkLoader : MonoBehaviour
    {
        [SerializeField] private RoadNetworkRuntime runtime;
        [SerializeField] private Transform buildingParent;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private GameObject[] buildingPrefabRegistry;
        [SerializeField] private string jsonPath = "Assets/RoadSystem/Runtime/Road Network prefabs/network.json";

        private void Start()
        {
            if (loadOnStart)
            {
                if (runtime != null && File.Exists(jsonPath))
                {
                    Load();
                }
                else
                {
                    Debug.LogWarning($"RoadNetworkLoader could not load on start: missing runtime or json not found at {jsonPath}.");
                }
            }
        }

        public void Load()
        {
            if (runtime == null)
            {
                Debug.LogWarning("RoadNetworkLoader missing runtime.");
                return;
            }
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                Debug.LogWarning($"RoadNetworkLoader json file not found at {jsonPath}");
                return;
            }
            RoadNetworkSerializer.LoadFromJsonFile(runtime, buildingParent != null ? buildingParent : runtime.transform, jsonPath, BuildPrefabLookup());
        }

        public void Save()
        {
            if (runtime == null)
            {
                Debug.LogWarning("RoadNetworkLoader missing runtime.");
                return;
            }
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogWarning("RoadNetworkLoader json path is empty.");
                return;
            }
            var dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            RoadNetworkSerializer.SaveToJsonFile(runtime, buildingParent != null ? buildingParent : runtime.transform, jsonPath);
        }

        private System.Collections.Generic.Dictionary<string, GameObject> BuildPrefabLookup()
        {
            var dict = new System.Collections.Generic.Dictionary<string, GameObject>();
            if (buildingPrefabRegistry == null) return dict;
            foreach (var go in buildingPrefabRegistry)
            {
                if (go == null) continue;
                var name = go.name.Replace("(Clone)", "").Trim();
                if (!dict.ContainsKey(name))
                {
                    dict[name] = go;
                }
            }
            return dict;
        }

        public string JsonPath => jsonPath;
        public void SetJsonPath(string path) => jsonPath = path;
    }
}

