using UnityEngine;

public static class TransformExtensions
{
    public static Vector2 Position2D(this Transform t, Vector2 newPosition = default)
    {
        if (newPosition != default)
            t.position = new Vector3(newPosition.x, newPosition.y, t.position.z);
        return new Vector2(t.position.x, t.position.y);
    }

    public static Vector2 Distance(this Transform t, Vector2 targetVector = default)
    {
        var X = t.position.x.Difference(targetVector.x);
        var Y = t.position.y.Difference(targetVector.y);

        return new Vector2(X, Y);
    }

    public static Vector2 Distance(this Transform t, Transform target = default)
    {
        var X = Mathf.Abs(target.position.x - t.position.x);
        var Y = t.position.y.Difference(target.position.y);

        return new Vector2(X, Y);
    }

    public static Vector2 Direction(this Transform t, Vector2 targetVector = default)
    {
        return targetVector - t.Position2D();
    }
    public static Vector2 Direction(this Transform t, Transform targetTransform)
    {
        return targetTransform.Position2D() - t.Position2D();
    }

    public static Vector2 Sign(this Transform t, Transform targetTransform)
    {
        var X = Mathf.Sign(t.Direction(targetTransform).x);
        var Y = Mathf.Sign(t.Direction(targetTransform).y);
        return new Vector2(X, Y);
    }
    public static Vector2 Sign(this Transform t, Vector2 targetTransform)
    {
        var X = Mathf.Sign(t.Direction(targetTransform).x);
        var Y = Mathf.Sign(t.Direction(targetTransform).y);
        return new Vector2(X, Y);
    }

    public static Vector2 OneWay(this Transform t, string direction = "Horizontal")
    {
        if (direction == "Horizontal")
            return Vector2.right * t.position.x;
        return Vector2.up * t.position.y;
    }
    public static Vector2 OneWay(this Vector2 v, string direction = "Horizontal")
    {
        if (direction == "Horizontal")
            return Vector2.right * v.x;
        return Vector2.up * v.y;
    }

    public static float DistanceX(this Transform t, Transform targetTransform)
    {
        return Mathf.Abs(t.position.x - targetTransform.position.x);
    }
    public static float DistanceX(this Transform t, Vector2 targetTransform)
    {
        return Mathf.Abs(t.position.x - targetTransform.x);
    }
    public static float DistanceY(this Transform t, Transform targetTransform)
    {
        return Mathf.Abs(t.position.y - targetTransform.position.y);
    }
    public static float DistanceY(this Transform t, Vector2 targetTransform)
    {
        return Mathf.Abs(t.position.y - targetTransform.y);
    }
}