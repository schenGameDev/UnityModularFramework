using System;
using System.Reflection;
using UnityEngine;

public static class ComponentExtension
{
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    /// <summary>
    /// Find component in parent only, not including self. If you want to include self, use GetComponentInParent&lt;T&gt;() instead.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="includeInactive"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetComponentInParentOnly<T>(this Component c, bool includeInactive = false) where T : Component
    {
        if (c == null) return null;
        
        var parent = c.transform.parent;
        return parent != null ? parent.GetComponentInParent<T>(includeInactive) : null;
    }
    
    /// <summary>
    /// Find component in children only, not including self. If you want to include self, use GetComponentInChildren&lt;T&gt;() instead.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="includeInactive"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetComponentInChildrenOnly<T>(this Component c, bool includeInactive = false) where T : Component
    {
        if (c == null) return null;

        foreach (Transform child in c.transform)
        {
            var component = child.GetComponentInChildren<T>(includeInactive);
            if (component != null) return component;
        }
        return null;
    }

    /// <summary>
    /// Searches for a component across the entire hierarchy: checks parents first (excluding self),
    /// then falls back to self and children. Use this when the component's location in the hierarchy is unknown.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="includeInactive"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetComponentInHierarchy<T>(this Component c, bool includeInactive = false) where T : Component
    {
        if (c == null) return null;
        
        var componentInParent = c.GetComponentInParentOnly<T>(includeInactive);
        if (componentInParent != null) return componentInParent;
        
        return c.GetComponentInChildren<T>(includeInactive);
    }
}