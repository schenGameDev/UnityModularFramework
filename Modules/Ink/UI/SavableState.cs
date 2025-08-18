using System;
using System.Collections.Generic;
using System.Reflection;
using ModularFramework.Commons;
using UnityEngine;
/// <summary>
/// Identify a property in MonoBehaviour that can be saved and later used to rebuild the GameObject  <br/>
/// To be used together with ISaveable
/// </summary>
public class SavableState : PropertyAttribute
{
    private const string ACTIVE_SELF = "activeSelf";
    
    #region Static Public
    public static Dictionary<string,AnyValue> GetSavableStates(object instance) {
        if(instance is not MonoBehaviour mono) return new Dictionary<string,AnyValue>();
        var states = new Dictionary<string, AnyValue>();
        states.Add(ACTIVE_SELF, AnyValue.Of(mono.gameObject.activeSelf));
        foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
            if (GetCustomAttribute(field, typeof(SavableState)) is not SavableState) continue;
            states.Add(field.Name, ConvertToAnyValue(field, instance));
        }
        return states;
    }

    public static void SetSavableStates(object instance, Dictionary<string,AnyValue> states) {
        if(instance is not MonoBehaviour mono) return;
        mono.gameObject.SetActive(states[ACTIVE_SELF]);
        foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
            if (GetCustomAttribute(field, typeof(SavableState)) is not SavableState) continue;
            SetField(field, instance, states[field.Name]);
        }

    }
    
    private static AnyValue ConvertToAnyValue(FieldInfo field, object instance)
    {
        var type = field.GetType();
        var value = field.GetValue(instance);

        if(type == typeof(int))
        {
            return AnyValue.Of((int)value);
        } 
        if(type == typeof(float))
        {
            return AnyValue.Of((float)value);
        } 
        
        if(type == typeof(bool)) {
            return AnyValue.Of((bool)value);
        }
        
        if(type == typeof(Vector3)) {
            return AnyValue.Of((Vector3)value);
        }
        
        if(type == typeof(string)) {
            return AnyValue.Of((string)value);
        }
        throw new InvalidCastException("Can't convert " + field.FieldType.Name + " to AnyValue");
    }
    
    private static AnyValue SetField(FieldInfo field, object instance, AnyValue value)
    {
        var type = field.GetType();
        if(type == typeof(int))
        {
            field.SetValue(instance, value.ConvertValue<int>());
        } 
        if(type == typeof(float))
        {
            field.SetValue(instance, value.ConvertValue<float>());
        } 
        
        if(type == typeof(bool)) {
            field.SetValue(instance, value.ConvertValue<bool>());
        }
        
        if(type == typeof(Vector3)) {
            field.SetValue(instance, value.ConvertValue<Vector3>());
        }
        
        if(type == typeof(string)) {
            field.SetValue(instance, value.ConvertValue<string>());
        }
        throw new InvalidCastException("Can't set " + field.FieldType.Name + " as " + value);
    }
    #endregion

    
}