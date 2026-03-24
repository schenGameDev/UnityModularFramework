
using UnityEngine;

public static class GameObjectExtension {
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component {
        T component = gameObject.GetComponent<T>();
        if(component == null) {
            component = gameObject.AddComponent<T>();
        }
        return component;
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

    public static bool IsInLayer(this GameObject gameObject, int layerMask) // not layer index
     {
         return (layerMask & (1 << gameObject.layer)) != 0;
     }
}