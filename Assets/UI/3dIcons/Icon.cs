using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Icon : MonoBehaviour
{
    // Start is called before the first frame update    public float amplitude = 0.5f; // Height of the sine wave
    public float amplitude = 0.5f; // Height of the sine wave
    public float frequency = 1f; // Speed of the sine wave
    public Object data;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        Vector3 tempPos = startPosition;
        tempPos.y += Mathf.Sin(Time.time * Mathf.PI * frequency) * amplitude;
        transform.position = tempPos;
    }
}
