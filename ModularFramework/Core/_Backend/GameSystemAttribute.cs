using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModularFramework
{
    public abstract class GameSystemAttribute : PropertyAttribute
    {
        private static readonly Dictionary<Type, Dictionary<Type,(FieldInfo,Attribute)[]>> FIELD_ATTRIBUTES = new ();

        protected static (FieldInfo,Attribute)[] GetAttributes(Type type, Type attributeType)
        {
            if(FIELD_ATTRIBUTES.TryGetValue(type, out var v))
            {
                return v.GetValueOrDefault(attributeType, Array.Empty<(FieldInfo,Attribute)>());
            }
            var dict = Analyze(type);
            FIELD_ATTRIBUTES[type] = dict;
            return dict.GetValueOrDefault(attributeType, Array.Empty<(FieldInfo,Attribute)>());
        }

        private static Dictionary<Type,(FieldInfo,Attribute)[]> Analyze(Type type)
        {
            Dictionary<Type,List<(FieldInfo,Attribute)>> attrDict = new();
            foreach(FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                foreach (var attr in field.GetCustomAttributes())
                {
                    if (attr.GetType().IsSubclassOf(typeof(GameSystemAttribute)))
                    {
                        attrDict.GetOrCreateDefault(attr.GetType()).Add((field, attr));
                    }
                }
            }
            return attrDict.ToDictionary(entry => entry.Key, 
                entry => entry.Value.ToArray());
        }
    }
}