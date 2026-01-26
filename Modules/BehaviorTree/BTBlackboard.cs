using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;

public class BTBlackboard : ScriptableObject
{
    public const string KEYWORD_TARGET = "Target";
    public const string KEYWORD_ABILITY_NAME = "AbilityName";
    
    public bool changed = false;
    
    [ReadOnly]
    [SerializeField,SerializedDictionary(keyName: "key", valueName: "value")]
    private SerializedDictionary<string, string> parameters = new ();
    
    [ReadOnly]
    [SerializeField,SerializedDictionary(keyName: "key", valueName: "transform")]
    private SerializedDictionary<string, List<Transform>> inSceneObjects = new ();
    
    public void Add(string key, string value)
    {
        changed = true;
        parameters[key] = value;
    }

    public void RemoveParameter(string key)
    {
        changed = true;
        parameters.Remove(key);
    }

    public void Add(string key, Transform tf)
    {
        if (tf == null) inSceneObjects.Remove(key);
        else inSceneObjects[key] = new List<Transform> {tf};
        changed = true;
    } 

    public void Add(string key, List<Transform> tfs)
    {
        if (tfs == null || tfs.Count == 0) inSceneObjects.Remove(key);
        else inSceneObjects[key] = tfs;
        changed = true;
    }
    
    public void Add<T>(string key, List<T> components) where T : Component
    {
        if (components == null || components.Count == 0) inSceneObjects.Remove(key);
        else inSceneObjects[key] = components.Select(c => c.transform).ToList();
        changed = true;
    }
    
    public void RemoveInSceneObject(string key)
    {
        changed = true;
        inSceneObjects.Remove(key);
    }

    public string Get(string key)
    {
        return parameters.GetValueOrDefault(key);
    }
    
    public List<T> Get<T>(string key) where T : Component
    {
        if (!inSceneObjects.TryGetValue(key, out var tfs))
        {
            return new List<T>();
        }

        if (typeof(T) == typeof(Transform))
        {
            return tfs as List<T>;
        }
        return tfs.Select(tf => tf.GetComponent<T>()).ToList();
    }

    public void Clear()
    {
        parameters.Clear();
        inSceneObjects.Clear();
        changed = false;
    }
}