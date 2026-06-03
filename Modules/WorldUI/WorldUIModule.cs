using ModularFramework;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityModularFramework.Modules.WorldUI
{
    
    
    public class WorldUIModule : GameModule<WorldUIModule>
    {
        [SceneRef("WORLD_UI_PARENT")] public Transform parent;
        
        [SerializeField] private WorldUIProgressBar progressBarPrefab;

        #region Game Module Lifecycle
        protected override void OnAwake()
        {
            RegisterProgressBar();
        }

        protected override void OnStart()
        {
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnLateUpdate()
        {
        }

        protected override void OnSceneDestroy()
        {
            UnregisterProgressBar();
        }

        protected override void OnDraw()
        {
        }
        

        #endregion

        #region Progress Bar

        private void RegisterProgressBar()
        {
            if (progressBarPrefab == null) return;
            PrefabPool<WorldUIProgressBar>.Register(progressBarPrefab, CreateProgressBarPool);
        }

        private void UnregisterProgressBar()
        {
            if (progressBarPrefab == null) return;
            PrefabPool<WorldUIProgressBar>.Clear();
        }
        
        private ObjectPool<WorldUIProgressBar> CreateProgressBarPool(WorldUIProgressBar prefab)
        {
            return new ObjectPool<WorldUIProgressBar>(
                createFunc: () => Instantiate(prefab, Vector3.zero, Quaternion.identity, parent),
                actionOnGet: bar => bar.Enable(),
                actionOnRelease: bar => bar.CleanUp(),
                actionOnDestroy: bar =>
                {
                    if (bar != null) Destroy(bar.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
        
        public static WorldUIProgressBar Get(Vector3 position)
        {
            var bar = PrefabPool<WorldUIProgressBar>.Get();
            bar.transform.position = position;
            return bar;
        } 
        public static void Release(WorldUIProgressBar bar) => PrefabPool<WorldUIProgressBar>.Release(bar);

        #endregion
    }
}