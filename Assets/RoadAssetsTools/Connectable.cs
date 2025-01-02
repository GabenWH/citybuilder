using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Connectable : MonoBehaviour
{
    public List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>{null ,null};
    public float height;
    public abstract float getWidth();
    public abstract void Connect(Connectable other);
    public abstract void Disconnect(Connectable other);
    public abstract void Check(Connectable other);
}