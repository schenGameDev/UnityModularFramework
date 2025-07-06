using System;
using System.Collections.Generic;
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
    public class SceneFlag : PropertyAttribute
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
            if (GameRunner.Instance) field.SetValue(instance, GameRunner.Instance.GetInSceneFlag(Keyword));
        }
        
        public static List<string> GetAllSceneFlagKeywords(object instance) {
            List<string> keywords = new List<string>();
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(SceneFlag)) is not SceneFlag attribute) continue;
                Type type = field.FieldType;
                if (type != typeof(string))
                {
                    Debug.LogError( $"{attribute.Keyword} cannot be added. SceneFlag can only be used with string. Please refresh modules manually after fixing script.");
                    continue;
                }
                keywords.Add(attribute.Keyword);
            }
            return keywords;
        }
        
        public static void InjectSceneFlags(object instance) {
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(SceneFlag)) is not SceneFlag attribute) continue;
                attribute.Inject(field, instance);
            }
        }
    }
}