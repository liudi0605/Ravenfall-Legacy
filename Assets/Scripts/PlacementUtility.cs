﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public static class PlacementUtility
{
    public static void PlaceOnGround(IEnumerable<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            PlaceOnGround(obj);
        }
    }
    public static void PlaceSelectionOnGround()
    {
#if UNITY_EDITOR
        PlaceOnGround(Selection.gameObjects);
#endif
    }

    public static void PlaceChildrenOnGround(IEnumerable<GameObject> objs)
    {
        foreach (var obj in objs)
        {
            PlaceChildrenOnGround(obj.transform);
        }
    }
    public static void PlaceChildrenOnGround(IEnumerable<Transform> objs)
    {
        foreach (var obj in objs)
        {
            PlaceChildrenOnGround(obj);
        }
    }
    public static void PlaceChildrenOnGround(Transform obj)
    {
        for (var i = 0; i < obj.childCount; ++i)
        {
            PlaceOnGround(obj.GetChild(i));
        }
    }
    public static void PlaceOnGround<T>() where T : MonoBehaviour
    {
        PlaceOnGround(GameObject.FindObjectsOfType<T>());
    }

    public static void PlaceOnGround(IEnumerable<MonoBehaviour> objs)
    {
        foreach (var obj in objs)
        {
            PlaceOnGround(obj.gameObject);
        }
    }

    public static void PlaceOnGround(this GameObject obj)
    {
        PlaceOnGround(obj.transform);
    }

    public static void PlaceOnGround(this Transform obj)
    {
        var pos = obj.position += Vector3.up * 10f;
        var ray = new Ray(obj.position, Vector3.down);
        var hits = Physics.RaycastAll(ray, 100f);
        foreach (var hit in hits.OrderBy(x => x.distance))
        {
            var name = hit.collider.name;
            if (Contains(name, "env_ground") ||
                Contains(name, "sm_env_", "crackedrock") ||
                 Contains(name, "env_dirt") ||
                 Contains(name, "env_pond") ||
                 Contains(name, "env_pound") ||
                 Contains(name, "generic_ground") ||
                 Contains(name, "prop_dock") ||
                 Contains(name, "bld_dock") ||
                 Contains(name, "terrain"))
            {
                obj.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Contains(string name, string value)
    {
        return name.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Contains(string name, string value1, string value2)
    {
        return Contains(name, value1) && Contains(name, value2);
    }
}

