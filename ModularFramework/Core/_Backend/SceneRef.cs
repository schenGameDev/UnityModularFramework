using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// Allow adding gameObject reference keyword onto GameRunner in editor.
    /// The gameObject needs to be linked with keyword on GameRunner.
    /// The referenced gameObject will be injected at runtime 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneRef : PropertyAttribute 
    {
        /// <summary>
        /// The keyword representing the wanted gameObject in scene
        /// </summary>
        public readonly string Keyword;
        public SceneRef(string keyword)
        {
            Keyword = keyword;
        }

        public void Inject(FieldInfo field, object instance)
        {
            if (!GameRunner.Instance) return;
            Type type = field.FieldType;
            GameObject go = GameRunner.Instance.GetInSceneGameObject(Keyword);
            field.SetValue(instance, type == typeof(Transform)? go.transform : go);
        }
        
        public static List<string> GetAllSceneReferenceKeywords(object instance) {
            List<string> keywords = new List<string>();
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(SceneRef)) is not SceneRef attribute) continue;
                Type type = field.FieldType;
                if (type != typeof(Transform) && type != typeof(GameObject))
                {
                    Debug.LogError( $"{attribute.Keyword} cannot be added. SceneRef can only be used with Transform or GameObject. Please refresh modules manually after fixing script.");
                    continue;
                }
                
                keywords.Add(attribute.Keyword);
            }
            return keywords;
        }
        
        public static void InjectSceneReferences(object instance) {
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(SceneRef)) is not SceneRef attribute) continue;
                attribute.Inject(field, instance);
            }
        }
    }
}