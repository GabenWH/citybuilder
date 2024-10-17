using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConnectionPoint : MonoBehaviour
{
    public Connectable connectedObject;  // The object this point connects to
    public ConnectionPoint connectedPoint;  // The corresponding point on the connected object
    public float width = 1f;

    public ConnectionPoint(Connectable connectedObject)
    {
        this.connectedObject = connectedObject;
    }

    public void Connect(ConnectionPoint other)
    {
        connectedPoint = other;
        other.connectedPoint = this;
        connectedObject.Connect(other.connectedObject);
    }

    public void Disconnect()
    {
        if (connectedPoint != null)
        {
            connectedPoint.connectedPoint = null;
            connectedObject.Disconnect(connectedPoint.connectedObject);
            connectedPoint = null;
        }
    }
}