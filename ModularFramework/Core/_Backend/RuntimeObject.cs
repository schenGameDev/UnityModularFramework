using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ModularFramework.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModularFramework
{
    /// <summary>
    /// Field will be reset at Reset() and cleaned at OnDestory().
    /// Collections and IResetable must be initialized w/ new()
    /// Do not use with SceneRef or SceneFlag!!
    /// </summary>
    public class RuntimeObject : GameSystemAttribute {
        #region Static Public
        public static void CleanRuntimeVars(object instance) {
            foreach(var (field, attr) in GetAttributes(instance.GetType(), typeof(RuntimeObject)))
            {
                ((RuntimeObject)attr).CleanField(field,instance);
            }
        }

        public static void InitializeRuntimeVars(object instance) {
            foreach(var (field, attr) in GetAttributes(instance.GetType(), typeof(RuntimeObject)))
            {
                ((RuntimeObject)attr).InitializeField(field,instance);
            }
        }
        #endregion

        /// <summary>
        /// the function name to initialize the object
        /// </summary>
        readonly string _initializer;
        /// <summary>
        /// the function name to clean up the object
        /// </summary>
        readonly string _cleaner;
        /// <summary>
        /// do not initialize the object
        /// </summary>
        readonly bool _noInitialize;
        

        public RuntimeObject(string initializer = "", string cleaner = "") {
            _initializer = initializer;
            _cleaner = cleaner;
        }
        public RuntimeObject(bool noInitialize) {
            _noInitialize = noInitialize;
        }

        private void InitializeField(FieldInfo field, object instance)
        {
            if(_noInitialize) return;
            Type type = field.FieldType;
            object value = field.GetValue(instance);
            try { 
                if(_initializer.NonEmpty()) 
                {
                    instance.GetType().GetMethod(_initializer, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(instance, null);
                } 
                else if (value is IResetable resettable) 
                {
                    resettable.ResetState();
                } 
                else if(type.InheritsOrImplements(typeof(ICollection<>))) 
                {
                    PurgeCollection(type, value);
                }
                else
                {
                    ResetFieldToDefault(type, field, instance);
                }
            } catch (Exception e) {
                DebugUtil.Error(e.Message);
            }
        }

        private void CleanField(FieldInfo field, object instance)
        {
            if(instance == null) return;
            Type type = field.FieldType;
            object value = field.GetValue(instance);
            try
            {
                if (value == null || (value is Object ua && ua==null)) return;
                
                if(_cleaner.NonEmpty()) {
                    instance.GetType().GetMethod(_cleaner)?.Invoke(instance, null);
                } else if(type.GetTypeInfo().IsValueType) {
                    // no need to clean value type
                } else if(value is IDisposable disposable) {
                    try
                    {
                        if(disposable is CancellationTokenSource cts) cts.Dispose();
                        disposable.Dispose();
                    }
                    catch (Exception)
                    {
                        // nothing
                    }
                    
                } else if (value is IResetable resettable) {
                    resettable.ResetState();
                } else if(type.InheritsOrImplements(typeof(ICollection<>))) {
                    PurgeCollection(type, value);
                } else if(type.InheritsOrImplements(typeof(Object)) || type.BaseType == typeof(object) || type.BaseType?.BaseType == typeof(object)) {
                    field.SetValue(instance, null);
                } else {
                    throw new("Can not purge " + field.Name);
                }
            } catch (Exception e)
            {
                var uo = instance as Object;
                var instanceName = uo == null ? "" : uo.name;
                Debug.LogError($"{instanceName} - {field.Name}: "  + e.Message);
            }
        }
        
        private void ResetFieldToDefault(Type type, FieldInfo field, object instance) {
            if(type == typeof(string)) {
                field.SetValue(instance, "");
                return;
            }
            field.SetValue(instance,GetDefault(type));
        }
        
        private static object GetDefault(Type type)
        {
            if(type.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        private void PurgeCollection(Type collectionType, object collection) {
            if(collectionType.InheritsOrImplements(typeof(IList))) {
                (collection as IList).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(IDictionary))) {
                (collection as IDictionary).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(Queue))) {
                (collection as Queue).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(Stack))) {
                (collection as Stack).Clear();
                return;
            }

            Type elementType = collectionType.GenericTypeArguments[0].BaseType;
            if(elementType == typeof(string)) {
                (collection as ICollection<string>).Clear();
            } else if(elementType == typeof(int)) {
                (collection as ICollection<int>).Clear();
            } else if(elementType == typeof(float)) {
                (collection as ICollection<float>).Clear();
            } else if(elementType == typeof(bool)) {
                (collection as ICollection<bool>).Clear();
            } else if(elementType == typeof(Vector3)) {
                (collection as ICollection<Vector3>).Clear();
            } else if(elementType == typeof(Vector2)) {
                (collection as ICollection<Vector2>).Clear();
            } else if(elementType == typeof(Vector3Int)) {
                (collection as ICollection<Vector3Int>).Clear();
            } else if(elementType == typeof(Vector2Int)) {
                (collection as ICollection<Vector2Int>).Clear();
            }else {
                DebugUtil.Error("Do not support " + collectionType);
            }
        }
    }
}