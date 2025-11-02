using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityTimer;

namespace ModularFramework {
    /// <summary>
    /// Life cycle: OnAwake(Reset,Load Ref Keywords) -> OnStart (link to GameManager) -> OnUpdate -> OnDestroy
    /// </summary>
    public abstract class GameModule : GameSystem {
        protected float DeltaTime;
        
        /// <summary>
        /// Trigger in <c>GameRunner</c> Awake method for sequential boot-up
        /// </summary>
        public virtual void OnAwake(){
            Reset();
        }

        /// <summary>
        /// Trigger in <c>GameRunner</c> Start method
        /// </summary>
        public override void OnStart() {
            if(updateMode == UpdateMode.NONE || OperateEveryFrame) return;
            if(updateMode == UpdateMode.EVERY_N_FRAME)
            {
                var timer = new RepeatFrameCountdownTimer(everyNFrame, 2);
                timer.OnTick += () =>  {
                    if(CentrallyManaged) GameRunner.Instance?.AddToExecQueue(this, timer.DeltaTime);
                    else OnUpdate(timer.DeltaTime);
                };
                Timer = timer;
            } else {
                var timer = new RepeatCountdownTimer(everyNSecond, 2);
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
        public void OnUpdate(float deltaTime)
        {
            DeltaTime = deltaTime;
            Update();
        }

        protected virtual void Update() {}

        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDestroy method
        /// </summary>
        public override void OnDestroy() {
            base.OnDestroy();
        }
        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDrawGizmos method
        /// </summary>
        public virtual void OnGizmos() {}

        protected virtual void Reset() {
            OperateEveryFrame = (updateMode == UpdateMode.EVERY_N_FRAME && everyNFrame == 1) || (updateMode == UpdateMode.EVERY_N_SECOND && everyNSecond == 0);
            RuntimeObject.InitializeRuntimeVars(this);
            SceneRef.InjectSceneReferences(this);
            SceneFlag.InjectSceneFlags(this);
        }

        // Handled in GameRunner
        public Timer Timer {get; private set;}

        [Header("Execution")]
        [SerializeField] protected UpdateMode updateMode;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_FRAME)] protected int everyNFrame = 1;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_SECOND)] protected float everyNSecond = 0;
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