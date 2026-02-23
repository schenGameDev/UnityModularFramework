using UnityEngine;

namespace ModularFramework.Modules.Slowdown
{
    [CreateAssetMenu(fileName = "SlowdownManager_SO", menuName = "Game Module/Slowdown")]
    public class SlowdownManagerSO : GameModule<SlowdownManagerSO>, ILive
    {
        [Header("Config")] public EventChannel<float> NPCSpeedChangeEvent;
        public float timeFreezeTime;

        [field: SerializeField] public bool Live { get; set; }

        [Range(0, 2)] public float currentSpeedModifier = 1;
        public float timeFreezeSpeedModifier = 0.1f;
        private float _endTime = 0;

        public SlowdownManagerSO()
        {
            updateMode = UpdateMode.EVERY_N_FRAME;
        }

        protected override void OnAwake()
        {
            Reset();
        }

        protected override void OnStart()
        {
        }

        protected override void OnUpdate()
        {
            if (!Live) return;

            if (_endTime > 0 && Time.time >= _endTime)
            {
                Reset();
            }
        }

        protected override void OnLateUpdate()
        {
        }

        protected override void OnSceneDestroy()
        {
        }

        protected override void OnDraw()
        {
        }

        public void TimeFreeze(float time)
        {
            SlowDown(0, time);
        }

        public void SlowDown(float time)
        {
            currentSpeedModifier = timeFreezeSpeedModifier;
            _endTime = Time.time + time;
            NPCSpeedChangeEvent.Raise(currentSpeedModifier);
        }

        public void SlowDown(float modifier, float time)
        {
            currentSpeedModifier = modifier;
            _endTime = Time.time + time;
            NPCSpeedChangeEvent.Raise(currentSpeedModifier);
        }

        private void Reset()
        {
            currentSpeedModifier = 1;
            _endTime = 0;
            NPCSpeedChangeEvent.Raise(1);
        }
    }
}