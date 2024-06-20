using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class Building : MonoBehaviour
{
    public JObject properties;
    public Mesh mesh;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    void OnDrawGizmos()
    {
        if (mesh != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector3 vertex in mesh.vertices)
            {
                Gizmos.DrawSphere(vertex, 0.1f);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
