using UnityEngine;

public enum PIVOT_TYPE
{
    NO_CHANGE,
    CENTER,
    CENTER_TOP,
    CENTER_BOTTOM,
    CENTER_RIGHT,
    CENTER_LEFT,
    CENTER_FRONT,
    CENTER_BACK
}

public class ModelUtils : MonoBehaviour
{
    public static Bounds CalculateBounds(GameObject obj, bool withCollider = false)
    {
        Bounds res;

        //obj.transform.position = Vector3.zero;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (withCollider)
        {
            var tempCol = renderers[0].GetComponent<BoxCollider>();

            if (tempCol == null)
            {
                tempCol = renderers[0].gameObject.AddComponent<BoxCollider>();
                tempCol.isTrigger = true;
            }

            res = tempCol.bounds;
            DestroyImmediate(tempCol);
        }
        else
        {
            res = renderers[0].bounds;
        }

        for (int i = 1; i < renderers.Length; i++)
        {
            Bounds b;

            if (withCollider)
            {
                var tempCol = renderers[i].GetComponent<BoxCollider>();

                if (tempCol == null)
                {
                    tempCol = renderers[i].gameObject.AddComponent<BoxCollider>();
                    tempCol.isTrigger = true;
                }

                b = tempCol.bounds;

                DestroyImmediate(tempCol);
            }
            else
            {
                b = renderers[i].bounds;
            }

            var max = Vector3.Max(res.max, b.max);
            var min = Vector3.Min(res.min, b.min);

            res = new Bounds((max + min) * 0.5f, max - min);
        }

        return res;
    }

    public static Vector3 GetCenter(GameObject obj, bool withCollider = false)
    {
        Vector3 res = CalculateBounds(obj, withCollider).center;

        return res;
    }

    public static Vector3 GetBottomCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.y -= bounds.size.y / 2;

        return res;
    }

    public static Vector3 GetTopCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.y += bounds.size.y / 2;

        return res;
    }

    public static Vector3 GetRightCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.x += bounds.size.x / 2;

        return res;
    }

    public static Vector3 GetLeftCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.x -= bounds.size.x / 2;

        return res;
    }

    public static Vector3 GetFrontCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.z -= bounds.size.z / 2;

        return res;
    }

    public static Vector3 GetBackCenter(GameObject obj, bool withCollider = false)
    {
        var bounds = CalculateBounds(obj, withCollider);

        Vector3 res = bounds.center;
        res.z += bounds.size.z / 2;

        return res;
    }

    public static GameObject CreateNewRoot(GameObject obj, PIVOT_TYPE pivot, bool withCollider = false)
    {
        GameObject o = Instantiate<GameObject>(obj, Vector3.zero, Quaternion.identity);
        o.name = obj.name;
        GameObject res = null;
        switch (pivot)
        {
            case PIVOT_TYPE.NO_CHANGE:
                {
                    o.transform.position = Vector3.zero;
                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);
                    o.transform.SetParent(o2.transform);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER:
                {
                    Vector3 pivotPos = GetCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_TOP:
                {
                    Vector3 pivotPos = GetTopCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_BOTTOM:
                {
                    Vector3 pivotPos = GetBottomCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_LEFT:
                {
                    Vector3 pivotPos = GetLeftCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_RIGHT:
                {
                    Vector3 pivotPos = GetRightCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_FRONT:
                {
                    Vector3 pivotPos = GetFrontCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            case PIVOT_TYPE.CENTER_BACK:
                {
                    Vector3 pivotPos = GetBackCenter(o, withCollider);

                    var o1 = new GameObject("ROOT");
                    var o2 = new GameObject(o.name + " ROOT");
                    o2.transform.SetParent(o1.transform);

                    o2.transform.position = pivotPos;

                    o.transform.SetParent(o2.transform);

                    o2.transform.Translate(o1.transform.position - o2.transform.position, Space.World);

                    res = o1;
                }
                break;
            default:
                break;
        }

        return res;
    }
}