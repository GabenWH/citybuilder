using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    // Start is called before the first frame update
    public bool input = false;
    public bool output = false;
    public Lane lane;
    public Road road;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
        void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 worldPosition = transform.position;
        Gizmos.DrawSphere(worldPosition, 0.1f);  // Visualize the slot position
        //Gizmos.DrawLine(worldPosition, worldPosition + rotationOffset * Vector3.forward * 0.5f);  // Visualize the slot orientation get it when I get the lanes
    }
}
