using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;

public class BTBlackboard : ScriptableObject
{
    public const string KEYWORD_TARGET = "Target";
    public const string KEYWORD_ABILITY_NAME = "AbilityName";
    
    
    [ReadOnly,ShowInInspector]
    [SerializedDictionary(keyName: "key", valueName: "value")]
    private readonly SerializedDictionary<string, string> _parameters = new ();
    
    [ReadOnly,ShowInInspector]
    [SerializedDictionary(keyName: "key", valueName: "transform")]
    private readonly SerializedDictionary<string, List<Transform>> _inSceneObjects = new ();
    
    public void Add(string key, string value) => _parameters[key] = value;
    
    public void RemoveParameter(string key) => _parameters.Remove(key);

    public void Add(string key, Transform tf)
    {
        if (tf == null) _inSceneObjects.Remove(key);
        else _inSceneObjects[key] = new List<Transform> {tf};
    } 

    public void Add(string key, List<Transform> tfs)
    {
        if (tfs == null || tfs.Count == 0) _inSceneObjects.Remove(key);
        else _inSceneObjects[key] = tfs;
    }
    
    public void Add<T>(string key, List<T> components) where T : Component
    {
        if (components == null || components.Count == 0) _inSceneObjects.Remove(key);
        else _inSceneObjects[key] = components.Select(c => c.transform).ToList();
    }

    public string Get(string key)
    {
        return _parameters.GetValueOrDefault(key);
    }
    
    public List<T> Get<T>(string key) where T : Component
    {
        if (!_inSceneObjects.TryGetValue(key, out var tfs))
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
        _parameters.Clear();
        _inSceneObjects.Clear();
    }
}