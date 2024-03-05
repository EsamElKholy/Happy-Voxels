using UnityEngine;

public static class GeometryUtils
{
    public static int TriangleBoxIntersection(Vector3[] triangle, Vector3 boxCenter, Vector3 boxSize)
    {
        Vector3 triangleEdge0 = triangle[1] - triangle[0];
        Vector3 triangleEdge1 = triangle[2] - triangle[1];
        Vector3 triangleEdge2 = triangle[0] - triangle[2];

        Vector3 triangleNormal = Vector3.Cross(triangleEdge0, triangleEdge1);

        Vector3 boxNormal0 = new Vector3(1, 0, 0);
        Vector3 boxNormal1 = new Vector3(0, 1, 0);
        Vector3 boxNormal2 = new Vector3(0, 0, 1);

        Vector3[] test =
        {
            boxNormal0,
            boxNormal1,
            boxNormal2,

            triangleNormal,

            new Vector3(0, -triangleEdge0.z, triangleEdge0.y),
            new Vector3(0, -triangleEdge1.z, triangleEdge1.y),
            new Vector3(0, -triangleEdge2.z, triangleEdge2.y),
            new Vector3(triangleEdge0.z, 0, -triangleEdge0.x),
            new Vector3(triangleEdge1.z, 0, -triangleEdge1.x),
            new Vector3(triangleEdge2.z, 0, -triangleEdge2.x),
            new Vector3(-triangleEdge0.y, triangleEdge0.x, 0),
            new Vector3(-triangleEdge1.y, triangleEdge1.x, 0),
            new Vector3(-triangleEdge2.y, triangleEdge2.x, 0)
        };

        for (int i = 0; i < test.Length; i++)
        {
            Vector3 axis = test[i];

            if (OverlapAxis(triangle, boxCenter, boxSize, axis) == 0)
            {
                return 0;
            }
        }

        return 1;
    }

    private static Vector2 GetBoxInterval(Vector3 boxCenter, Vector3 boxSize, Vector3 axis)
    {
        Vector2 interval = Vector2.zero;

        Bounds b = new Bounds(boxCenter, boxSize);

        Vector3 p1 = boxCenter + boxSize / 2;
        Vector3 p2 = boxCenter - boxSize / 2;

        Vector3 min = p2;
        Vector3 max = p1;

        Vector3[] vertex =
        {
            new Vector3(min.x, max.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, min.y, min.z)
        };

        float mn = float.MaxValue, mx = float.MinValue;

        for (int i = 0; i < vertex.Length; i++)
        {
            float projection = Vector3.Dot(axis, vertex[i]);

            mn = Mathf.Min(mn, projection);
            mx = Mathf.Max(mx, projection);
        }

        interval.x = mn;
        interval.y = mx;

        return interval;
    }

    private static Vector2 GetTriangleInterval(Vector3[] triangle, Vector3 axis)
    {
        Vector2 interval = Vector2.zero;

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < 3; i++)
        {
            float projection = Vector3.Dot(triangle[i], axis);

            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }

        interval.x = min;
        interval.y = max;

        return interval;
    }

    private static int OverlapAxis(Vector3[] triangle, Vector3 boxCenter, Vector3 boxSize, Vector3 axis)
    {
        var a = GetBoxInterval(boxCenter, boxSize, axis);
        var b = GetTriangleInterval(triangle, axis);

        if (((b.x <= a.y) && (a.x <= b.y)))
        {
            return 1;
        }

        return 0;
    }
}
