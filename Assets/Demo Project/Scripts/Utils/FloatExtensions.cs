using UnityEngine;

public static class FloatExtensions
{
    public static float Difference(this float value, float target)
    {
        return Mathf.Abs(value - target);
    }
    public static float Sign(this float value)
    {
        return Mathf.Sign(value);
    }

    public static Vector2 XVector(this float value)
    {
        return Vector2.right * value;
    }
    public static Vector2 YVector(this float value)
    {
        return Vector2.up * value;
    }
}

public static class StringExtensions
{
    public static void Print(this string value)
    {
        Debug.Log(value);
    }
}