using System;
using UnityEngine;

[Serializable]
public struct SerializedVector
{
    public float x;
    public float y;
    public float z;

    public SerializedVector(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializedVector(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}