using System;
using System.Collections.Generic;
using System.Reflection;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;
using UnityTimer;

namespace ModularFramework {
    /// <summary>
    /// Life cycle: OnAwake(Reset,Load Ref Keywords) -> OnStart (link to GameManager) -> OnUpdate -> OnDestroy
    /// </summary>
    public abstract class GameModule : ScriptableObject {
        /// <summary>
        /// Trigger in <c>GameRunner</c> Awake method for sequencial boot-up
        /// </summary>
        public virtual void OnAwake(Dictionary<string, string> flags, Dictionary<string,GameObject> references){
            Reset();
        }

        /// <summary>
        /// Trigger only for cross scene modules
        /// </summary>
        public virtual void OnFirstStart() {
            InitializeRuntimeVars(true);
        }
        /// <summary>
        /// Trigger only for cross scene modules
        /// </summary>
        public virtual void OnFinalDestroy() {
            CleanRuntimeVars(true);
        }

        /// <summary>
        /// Trigger in <c>GameRunner</c> Start method
        /// </summary>
        public virtual void OnStart(){
            if(updateMode == UpdateMode.NONE || OperateEveryFrame) return;
            if(updateMode == UpdateMode.EVERY_N_FRAME)
            {
                var timer = new RepeatFrameCountdownTimer(_everyNFrame, 2);
                timer.OnTick += () =>  {
                    if(CentrallyManaged) GameRunner.Instance?.AddToExecQueue(this, timer.DeltaTime);
                    else OnUpdate(timer.DeltaTime);
                };
                Timer = timer;
            } else {
                var timer = new RepeatCountdownTimer(_everyNSecond, 2);
                timer.OnTick += () =>  {
                    if(CentrallyManaged) GameRunner.Instance?.AddToExecQueue(this, timer.DeltaTime);
                    else OnUpdate(timer.DeltaTime);
                };
                Timer = timer;
            }

            Timer.Start();
        }
        /// <summary>
        /// Trigger in <c>GameRunner</c> Update method
        /// </summary>
        public virtual void OnUpdate(float deltaTime){}
        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDestroy method
        /// </summary>
        public virtual void OnDestroy() {
            CleanRuntimeVars(false);
        }
        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDrawGizmos method
        /// </summary>
        public virtual void OnGizmos() {}

        protected virtual void Reset() {
            OperateEveryFrame = (updateMode == UpdateMode.EVERY_N_FRAME && _everyNFrame == 1) || (updateMode == UpdateMode.EVERY_N_SECOND && _everyNSecond == 0);

            InitializeRuntimeVars(false);
        }

        // Handled in GameRunner
        public string[] Keywords {get;set;}
        public string[] RefKeywords {get;set;}

        public Timer Timer {get; private set;}

        [Header("Execution")]
        /// <summary>
        /// Once checked, OnAwake() and OnDestroy() will be called only once throughout the game
        /// </summary>
        [HideInInspector] public bool crossScene;
        [SerializeField] protected UpdateMode updateMode;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_FRAME)] private int _everyNFrame = 1;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_SECOND)] private float _everyNSecond = 0;
        /// <summary>
        /// only one of centerally managed modules will be executed at one frame
        /// </summary>
        [SerializeField,HideField(nameof(updateMode), UpdateMode.NONE)] public bool CentrallyManaged;
        public bool OperateEveryFrame {get; private set;}

        protected enum UpdateMode {
            NONE,EVERY_N_FRAME,EVERY_N_SECOND
        }

    #region Runtime Variable
        protected void CleanRuntimeVars(bool moduleFinalEnd) {
            foreach(FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (Attribute.GetCustomAttribute(field, typeof(RuntimeObject)) is not RuntimeObject attribute) continue;
                bool isRightTime = (crossScene && moduleFinalEnd && attribute.lifetime==RuntimeObjectLifetime.MODULE) ||
                                    (!crossScene && !moduleFinalEnd) || (attribute.lifetime==RuntimeObjectLifetime.SCENE && !moduleFinalEnd);
                if(!isRightTime) continue;
                Type type = field.FieldType;
                object value = field.GetValue(this);
                try {
                    if(attribute.cleaner.NonEmpty()) {
                        GetType().GetMethod(attribute.cleaner).Invoke(this, null);
                    } else if(IsValueType(type)) {
                        // no need to clean value type
                        continue;
                    } else if(type.InheritsOrImplements(typeof(IDisposable))) {
                        (value as IDisposable).Dispose();
                    } else if (type.InheritsOrImplements(typeof(IResetable))) {
                        (value as IResetable).Reset();
                    } else if(type.InheritsOrImplements(typeof(ICollection<>))) {
                        PurgeCollection(type, value);
                    } else if(type.BaseType == typeof(object) || type.BaseType == typeof(Component)) {
                        field.SetValue(this, null);
                    } else {
                        throw new("Can not purge " + field.Name);
                    }
                } catch (Exception e) {
                    DebugUtil.Error(e.Message);
                }
            }

        }

        protected void InitializeRuntimeVars(bool moduleFirstStart) {
            foreach(FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (Attribute.GetCustomAttribute(field, typeof(RuntimeObject)) is not RuntimeObject attribute) continue;
                bool isRightTime = (crossScene && moduleFirstStart && attribute.lifetime==RuntimeObjectLifetime.MODULE) ||
                                    (!crossScene && !moduleFirstStart) || (attribute.lifetime==RuntimeObjectLifetime.SCENE && !moduleFirstStart);
                if(!isRightTime) continue;
                Type type = field.FieldType;
                object value = field.GetValue(this);
                try {
                    if(attribute.initializer.NonEmpty()) {
                        GetType().GetMethod(attribute.initializer,
                                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Invoke(this, null);
                    } else if(!ResetValueTypes(type, field)) {
                        if (type.InheritsOrImplements(typeof(IResetable))) {
                            (value as IResetable).Reset();
                        } else if(type.InheritsOrImplements(typeof(ICollection<>))) {
                            PurgeCollection(type, value);
                        } else if(type.BaseType == typeof(UnityEngine.Object) || type.BaseType == typeof(object) || type.BaseType == typeof(Component)) {
                            field.SetValue(this, null);
                        } else {
                            throw new("Do not support " + field.Name);
                        }
                    }
                } catch (Exception e) {
                    DebugUtil.Error(e.Message);
                }
            }

        }

        private bool IsValueType(Type type) {
            return type == typeof(short) || type == typeof(uint) || type == typeof(int) ||
                type == typeof(float) || type == typeof(double) || type == typeof(bool) ||
                type == typeof(byte) || type == typeof(char) || type == typeof(Vector2) ||
                type == typeof(Vector3) || type == typeof(Vector2Int) || type == typeof(Vector3Int);
        }

        private bool ResetValueTypes(Type type, FieldInfo field) {
            if(type == typeof(short)) {
                field.SetValue(this, default(short));
                return true;
            } else if(type == typeof(uint)) {
                field.SetValue(this, default(uint));
                return true;
            } else if(type == typeof(int)) {
                field.SetValue(this, default(int));
                return true;
            } else if(type == typeof(float)) {
                field.SetValue(this, default(float));
                return true;
            } else if(type == typeof(double)) {
                field.SetValue(this, default(double));
                return true;
            } else if(type == typeof(bool)) {
                field.SetValue(this, default(bool));
                return true;
            } else if(type == typeof(byte)) {
                field.SetValue(this, default(byte));
                return true;
            } else if(type == typeof(char)) {
                field.SetValue(this, default(char));
                return true;
            } else if(type == typeof(Vector2)) {
                field.SetValue(this, Vector2.zero);
                return true;
            } else if(type == typeof(Vector3)) {
                field.SetValue(this, Vector3.zero);
                return true;
            } else if(type == typeof(Vector2Int)) {
                field.SetValue(this, Vector2Int.zero);
                return true;
            } else if(type == typeof(Vector3Int)) {
                field.SetValue(this, Vector3Int.zero);
                return true;
            } else if(type == typeof(string)) {
                field.SetValue(this, "");
                return true;
            }
            return false;
        }

        private void PurgeCollection(Type collectionType, object collection) {
            if(collectionType.InheritsOrImplements(typeof(System.Collections.IList))) {
                (collection as System.Collections.IList).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(System.Collections.IDictionary))) {
                (collection as System.Collections.IDictionary).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(System.Collections.Queue))) {
                (collection as System.Collections.Queue).Clear();
                return;
            }
            if(collectionType.InheritsOrImplements(typeof(System.Collections.Stack))) {
                (collection as System.Collections.Stack).Clear();
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

        /// <summary>
        /// Field will be reset at Reset() and cleaned at OnDestory().
        /// Collections and IResetable must be initialized w/ new()
        /// </summary>
        protected class RuntimeObject : PropertyAttribute  {
            /// <summary>
            /// the function name to initialize the object
            /// </summary>
            public readonly string initializer;
            public readonly string cleaner;
            public readonly bool notInitialize;
            public readonly RuntimeObjectLifetime lifetime = RuntimeObjectLifetime.MODULE;

            public RuntimeObject(RuntimeObjectLifetime lifetime) => this.lifetime = lifetime;

            public RuntimeObject(string initializer = "", string cleaner = "",RuntimeObjectLifetime lifetime=RuntimeObjectLifetime.MODULE) {
                this.initializer = initializer;
                this.cleaner = cleaner;
            }
            public RuntimeObject(bool notInitialize) {
                this.notInitialize = notInitialize;
            }
        }

        protected enum RuntimeObjectLifetime {MODULE, SCENE}
    #endregion
    }
}