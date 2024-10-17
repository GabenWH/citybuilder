using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Connectable : MonoBehaviour
{
    public List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>{null ,null};

    public abstract void Connect(Connectable other);
    public abstract void Disconnect(Connectable other);
}