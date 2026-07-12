using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class GameObjectExtension {
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component {
        T component = gameObject.GetComponent<T>();
        if(component == null) {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
    
    public static T GetOrAdd<T>(this Transform transform) where T : Component {
        return transform.gameObject.GetOrAdd<T>();
    }

    public static void DestroyChildren(this GameObject gameObject)
    {
        DestroyChildren(gameObject.transform);
    }
    
    public static void DestroyChildren(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            Object.Destroy(child.gameObject);
        }
    }
    /// <summary>
    ///  Is the gameobject in layer
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="layerMask">not layer index</param>
    /// <returns></returns>
    public static bool IsInLayer(this GameObject gameObject, int layerMask)
     {
         return (layerMask & (1 << gameObject.layer)) != 0;
     }
    
    /// <summary>
    /// Is the transform in layer
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="layerMask">not layer index</param>
    /// <returns></returns>
    public static bool IsInLayer(this Transform transform, int layerMask)
    {
        return transform.gameObject.IsInLayer(layerMask);
    }

    public static string GetHierarchyPath(this Transform transform)
    {
        var path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }
        return path;
    }
    
    public static string GetHierarchyPath(this GameObject gameObject)
    {
        return gameObject.transform.GetHierarchyPath();
    }

    public static Bounds ExpandToInclude(this Bounds a, Bounds b)
    {
        a.Encapsulate(b);
        return a;
    }

    public static Pose GetPose(this Transform transform)
    {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        return new Pose(position, rotation);
    }

    public static void SetPose(this Transform transform, Pose pose)
    {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
}