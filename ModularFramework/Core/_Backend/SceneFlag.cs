using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// Allow adding flag keyword onto GameRunner in editor.
    /// The flag needs to set on GameRunner in scene.
    /// The referenced flag will be injected at runtime 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneFlag : GameSystemAttribute
    {
        /// <summary>
        /// The keyword representing the wanted gameObject in scene
        /// </summary>
        public readonly string Keyword;
        public SceneFlag(string keyword)
        {
            Keyword = keyword;
        }

        public void Inject(FieldInfo field, object instance)
        {
            if (!GameRunner.Instance) return;
            string value = GameRunner.Instance.GetInSceneFlag(Keyword);
            Type type = field.FieldType;
            if (type == typeof(string)) field.SetValue(instance, value);
            else if (type == typeof(int)) field.SetValue(instance, int.Parse(value));
            else if (type == typeof(float)) field.SetValue(instance, float.Parse(value));
            else if (type == typeof(bool)) field.SetValue(instance, bool.Parse(value));
            else if (type == typeof(List<string>)) field.SetValue(instance, value.Split(',').ToList());
            else if (type == typeof(List<int>)) field.SetValue(instance, value.Split(',').Select(int.Parse).ToList());
            else if (type == typeof(List<bool>)) field.SetValue(instance, value.Split(',').Select(bool.Parse).ToList());
            else
            {
                Debug.LogError($"Unsupported SceneFlag type: " + type);
            }
        }
        
        public static List<string> GetAllSceneFlagKeywords(object instance) {
            List<string> keywords = new List<string>();
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
                if (GetCustomAttribute(field, typeof(SceneFlag)) is not SceneFlag attribute) continue;
                Type type = field.FieldType;
                if (type != typeof(string) && type != typeof(bool) && type != typeof(int) && type != typeof(float) && 
                    type != typeof(List<string>) && type != typeof(List<int>) && type != typeof(List<bool>)) 
                {
                    Debug.LogError( $"{attribute.Keyword} cannot be added. SceneFlag doesn't support the given type. Please refresh modules manually after fixing script.");
                    continue;
                }
                keywords.Add(attribute.Keyword);
            }
            return keywords;
        }
        
        public static void InjectSceneFlags(object instance) {
            foreach(var (field, attr) in GetAttributes(instance.GetType(), typeof(SceneFlag)))
            {
                ((SceneFlag)attr).Inject(field,instance);
            }
        }
    }
}