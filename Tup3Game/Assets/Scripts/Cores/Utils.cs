using UnityEngine;

public static class Utils
{
    public static Vector3 ToVector3(this SerializedVector v) => new(v.x, v.y, v.z);

    public static SerializedVector ToSerializedVector(this Vector3 v) => new(v);
}