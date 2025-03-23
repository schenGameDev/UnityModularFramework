using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityTimer;

namespace ModularFramework {
    /// <summary>
    /// Life cycle: OnAwake(Reset,Load Ref Keywords) -> OnStart (link to GameManager) -> OnUpdate -> OnDestroy
    /// </summary>
    public abstract class GameModule : ScriptableObject {
        /// <summary>
        /// Trigger in <c>GameRunner</c> Awake method for sequential boot-up
        /// </summary>
        public virtual void OnAwake(Dictionary<string, string> flags, Dictionary<string,GameObject> references){
            Reset();
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
            RuntimeObject.CleanRuntimeVars(this);
        }
        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDrawGizmos method
        /// </summary>
        public virtual void OnGizmos() {}

        protected virtual void Reset() {
            OperateEveryFrame = (updateMode == UpdateMode.EVERY_N_FRAME && _everyNFrame == 1) || (updateMode == UpdateMode.EVERY_N_SECOND && _everyNSecond == 0);
            RuntimeObject.InitializeRuntimeVars(this);
        }

        // Handled in GameRunner
        public string[] Keywords {get; protected set;}
        public string[] RefKeywords {get; protected set;}

        public Timer Timer {get; private set;}

        [Header("Execution")]
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

    
    }
}