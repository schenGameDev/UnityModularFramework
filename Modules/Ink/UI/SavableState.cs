using System;
using System.Collections.Generic;
using System.Reflection;
using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    /// <summary>
    /// Identify a property in MonoBehaviour that can be saved and later used to rebuild the GameObject  <br/>
    /// To be used together with ISaveable
    /// </summary>
    public class SavableState : PropertyAttribute
    {
        private const string ACTIVE_SELF = "activeSelf";

        /// <summary>
        /// the function name to generate the savable string
        /// </summary>
        private readonly string _saveFunc;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveFunc">saveFunc must take no parameter and return the same type as the field</param>
        public SavableState(string saveFunc = "")
        {
            _saveFunc = saveFunc;
        }

        private AnyValue GetSavableValue(FieldInfo field, object instance)
        {
            if(_saveFunc.NonEmpty())
            {
                var savableValue = instance.GetType()
                    .GetMethod(_saveFunc, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
                    .Invoke(instance, null);
                return ConvertToAnyValue(field.GetType(), savableValue);
            } 
            
            return ConvertToAnyValue(field, instance);
        }
        
        #region Static Public

        public static Dictionary<string, AnyValue> GetSavableStates(object instance)
        {
            if (instance is not MonoBehaviour mono) return new Dictionary<string, AnyValue>();
            var states = new Dictionary<string, AnyValue>();
            states.Add(ACTIVE_SELF, AnyValue.Of(mono.gameObject.activeSelf));
            foreach (FieldInfo field in instance.GetType()
                         .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (GetCustomAttribute(field, typeof(SavableState)) is SavableState ss)
                {
                    states.Add(field.Name, ss.GetSavableValue(field, instance));
                }
            }

            return states;
        }

        public static void SetSavableStates(object instance, Dictionary<string, AnyValue> states)
        {
            if (instance is not MonoBehaviour mono) return;
            mono.gameObject.SetActive(states[ACTIVE_SELF]);
            foreach (FieldInfo field in instance.GetType()
                         .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (GetCustomAttribute(field, typeof(SavableState)) is not SavableState) continue;
                SetField(field, instance, states[field.Name]);
            }

        }
        
        private static AnyValue ConvertToAnyValue(Type fieldType, object fieldValue)
        {
            if (fieldType == typeof(int))
            {
                return AnyValue.Of((int)fieldValue);
            }

            if (fieldType == typeof(float))
            {
                return AnyValue.Of((float)fieldValue);
            }

            if (fieldType == typeof(bool))
            {
                return AnyValue.Of((bool)fieldValue);
            }

            if (fieldType == typeof(Vector3))
            {
                return AnyValue.Of((Vector3)fieldValue);
            }

            if (fieldType == typeof(string))
            {
                return AnyValue.Of((string)fieldValue);
            }

            throw new InvalidCastException("Can't convert " + fieldType.Name + " to AnyValue");
        }

        private static AnyValue ConvertToAnyValue(FieldInfo field, object instance)
        {
            var type = field.GetType();
            var value = field.GetValue(instance);

            return ConvertToAnyValue(type, value);
        }

        private static AnyValue SetField(FieldInfo field, object instance, AnyValue value)
        {
            var type = field.GetType();
            if (type == typeof(int))
            {
                field.SetValue(instance, value.ConvertValue<int>());
            }

            if (type == typeof(float))
            {
                field.SetValue(instance, value.ConvertValue<float>());
            }

            if (type == typeof(bool))
            {
                field.SetValue(instance, value.ConvertValue<bool>());
            }

            if (type == typeof(Vector3))
            {
                field.SetValue(instance, value.ConvertValue<Vector3>());
            }

            if (type == typeof(string))
            {
                field.SetValue(instance, value.ConvertValue<string>());
            }

            throw new InvalidCastException("Can't set " + field.FieldType.Name + " as " + value);
        }

        #endregion
    }
}