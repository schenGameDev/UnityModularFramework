using ModularFramework;
using UnityEngine;

namespace UnityModularFramework.Modules.WorldUI
{
    
    
    public class WorldUIModule : GameModule<WorldUIModule>
    {
        [SceneRef("WORLD_UI_PARENT")] public Transform parent;
        
        [SerializeField] private WorldUIProgressBar progressBarPrefab;

        #region Game Module Lifecycle
        protected override void OnAwake()
        {
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
        

        private void UnregisterProgressBar()
        {
            if (progressBarPrefab == null) return;
            PrefabPool<WorldUIProgressBar>.Clear();
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