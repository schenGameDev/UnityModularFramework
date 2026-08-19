using EditorAttributes;
using KBCore.Refs;
using Sisus.ComponentNames;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Move", 0), RequireComponent(typeof(BTRunner))]
    public class BTMove : MonoBehaviour, IUniqueIdentifiable, IReady
    {
        public string UniqueId => moveName;

        [SerializeField, OnValueChanged(nameof(RenameComponent))]
        private string moveName;

        public float speed;
        [SerializeField] private AnimationConfig animConfig;
        [SerializeField] private string pathName;

        private CountdownTimer _cooldownTimer;
        [SerializeField] private BTAnimation animation;
        [SerializeField, Self(Flag.Optional)] private WaypointCollection waypointCollection;
        [ShowInInspector, ReadOnly] private bool _running;
        [ReadOnly, ShowInInspector] protected bool isReady = true;
        public bool Ready => isReady;

        private void RenameComponent() => this.SetName($"Move: {moveName}");

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        public Vector3[] Path => string.IsNullOrEmpty(pathName) || !waypointCollection
            ? null
            : waypointCollection?.GetPath(pathName);


        public void Move()
        {
            if (_running) return;
            animation?.SetFlag(animConfig, null);
            _running = true;
        }

        public void Stop()
        {
            if (!_running) return;
            animation?.ReverseFlag(animConfig, false);
            _running = false;
        }
    }
}