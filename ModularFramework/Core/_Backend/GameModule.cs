using EditorAttributes;
using UnityEngine;
using UnityTimer;

namespace ModularFramework {
    /// <summary>
    /// Life cycle: Awake(Reset,Load Ref Keywords) -> Start (link to GameManager) -> Update -> Destroy
    /// </summary>
    public abstract class GameModule<T> : GameModule where T : GameModule<T> {
        public override void SceneAwake()
        {
            base.SceneAwake();
            ((T)this).OnAwake();
        }
        
        public override void Start()
        {
            base.Start();
            ((T)this).OnStart();
        }

        public override void Tick(float deltaTime)
        {
            DeltaTime = deltaTime;
            ((T)this).OnUpdate();
        }

        public override void Destroy()
        {
            ((T)this).OnSceneDestroy();
            base.Destroy();
        }

        public override void Draw()
        {
            ((T)this).OnDraw();
        }
        
        
        protected float DeltaTime;
        
        /// <summary>
        /// Trigger in <c>GameRunner</c> Awake method for sequential boot-up
        /// </summary>
        protected abstract void OnAwake();
        /// <summary>
        /// Trigger in <c>GameRunner</c> Start method
        /// </summary>
        protected abstract void OnStart();
        /// <summary>
        /// Trigger in <c>GameRunner</c> Update method
        /// </summary>
        protected abstract void OnUpdate();
        /// <summary>
        /// Trigger in <c>GameRunner</c> Destroy method
        /// </summary>
        protected abstract void OnSceneDestroy();

        /// <summary>
        /// Trigger in <c>GameRunner</c> OnDrawGizmos method
        /// </summary>
        protected abstract void OnDraw();
    }
    
    public abstract class GameModule : GameSystem
    { 
        [Header("Execution")]
        [SerializeField] protected UpdateMode updateMode;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_FRAME)] protected int everyNFrame = 1;
        [SerializeField,ShowField(nameof(updateMode), UpdateMode.EVERY_N_SECOND)] protected float everyNSecond = 0;
        /// <summary>
        /// only one of centerally managed modules will be executed at one frame
        /// </summary>
        [SerializeField,HideField(nameof(updateMode), UpdateMode.NONE)] public bool CentrallyManaged;
        public bool OperateEveryFrame {get; private set;}
        // Handled in GameRunner
        public Timer Timer {get; private set;}

        protected enum UpdateMode {
            NONE,EVERY_N_FRAME,EVERY_N_SECOND
        }

        public override void SceneAwake()
        {
            base.SceneAwake();
            OperateEveryFrame = (updateMode == UpdateMode.EVERY_N_FRAME && everyNFrame == 1) || (updateMode == UpdateMode.EVERY_N_SECOND && everyNSecond == 0);
        }
        
        public override void Start()
        {
            base.Start();
            if(updateMode == UpdateMode.NONE || OperateEveryFrame) return;
            if(updateMode == UpdateMode.EVERY_N_FRAME)
            {
                var timer = new RepeatFrameCountdownTimer(everyNFrame, 2);
                timer.OnTick += () =>  {
                    if(CentrallyManaged) GameRunner.Instance?.AddToExecQueue(this, timer.DeltaTime);
                    else Tick(timer.DeltaTime);
                };
                Timer = timer;
            } else {
                var timer = new RepeatCountdownTimer(everyNSecond, 2);
                timer.OnTick += () =>  {
                    if(CentrallyManaged) GameRunner.Instance?.AddToExecQueue(this, timer.DeltaTime);
                    else Tick(timer.DeltaTime);
                };
                Timer = timer;
            }

            Timer.Start();
        }

        public abstract void Tick(float deltaTime);
        public abstract void Draw();
    }
}