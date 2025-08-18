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
    /// </summary>
    public class RuntimeObject : PropertyAttribute  {
        #region Static Public
        public static void CleanRuntimeVars(object instance) {
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(RuntimeObject)) is not RuntimeObject attribute) continue;
                attribute.CleanField(field,instance);
            }

        }

        public static void InitializeRuntimeVars(object instance) {
            foreach(FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (GetCustomAttribute(field, typeof(RuntimeObject)) is not RuntimeObject attribute) continue;
                attribute.InitializeField(field, instance);
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
                if(_initializer.NonEmpty()) {
                    instance.GetType().GetMethod(_initializer, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(instance, null);
                } else if(!ResetValueTypes(type, field, instance)) {
                    if (value is IResetable resettable) {
                        resettable.Reset();
                    } else if(type.InheritsOrImplements(typeof(ICollection<>))) {
                        PurgeCollection(type, value);
                    } else if(type.InheritsOrImplements(typeof(Object)) || 
                              type.BaseType == typeof(object) || 
                              type.BaseType?.BaseType == typeof(object)) {
                        field.SetValue(instance, null);
                    } else {
                        throw new("Do not support " + field.Name);
                    }
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
                } else if(IsValueType(type)) {
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
                    resettable.Reset();
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

        private bool IsValueType(Type type) {
            return type == typeof(short) || type == typeof(uint) || type == typeof(int) ||
                type == typeof(float) || type == typeof(double) || type == typeof(bool) ||
                type == typeof(byte) || type == typeof(char) || type == typeof(Vector2) ||
                type == typeof(Vector3) || type == typeof(Vector2Int) || type == typeof(Vector3Int);
        }

        private bool ResetValueTypes(Type type, FieldInfo field, object instance) {
            if(type == typeof(short)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(uint)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(int)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(float)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(double)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(bool)) {
                field.SetValue(instance, false);
                return true;
            }

            if(type == typeof(byte)) {
                field.SetValue(instance, 0);
                return true;
            }

            if(type == typeof(char)) {
                field.SetValue(instance, '\0');
                return true;
            }

            if(type == typeof(Vector2)) {
                field.SetValue(instance, Vector2.zero);
                return true;
            }

            if(type == typeof(Vector3)) {
                field.SetValue(instance, Vector3.zero);
                return true;
            }

            if(type == typeof(Vector2Int)) {
                field.SetValue(instance, Vector2Int.zero);
                return true;
            }

            if(type == typeof(Vector3Int)) {
                field.SetValue(instance, Vector3Int.zero);
                return true;
            }

            if(type == typeof(string)) {
                field.SetValue(instance, "");
                return true;
            }
            return false;
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