
using UnityEngine;

public static class GameObjectExtension {
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component {
        T component = gameObject.GetComponent<T>();
        if(component == null) {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
}