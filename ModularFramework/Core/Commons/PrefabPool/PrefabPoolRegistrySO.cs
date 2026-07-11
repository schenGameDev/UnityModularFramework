using UnityEngine;

namespace ModularFramework
{
    public class PrefabPoolRegistrySO : GameModule<PrefabPoolRegistrySO>
    {
        [SerializeField] private AssetIdentity[] assets;
        
        protected override void OnAwake()
        {
            RegisterPrefabPool();
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
            UnregisterPrefabPool();
        }

        protected override void OnDraw()
        {
        }
        
        
        private void RegisterPrefabPool()
        {
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset.TryGetComponent<IPrefabPoolEntry>(out var poolEntry))
                    {
                        poolEntry.RegisterPrefabToPool();
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab {asset.name} does not implement IPrefabPoolEntry, cannot register to pool.");
                    }
                }
            }
        }
        
        private void UnregisterPrefabPool()
        {
            if (assets != null)
            {
                assets.ForEach(asset =>
                {
                    if (asset.TryGetComponent<IPrefabPoolEntry>(out var poolEntry))
                    {
                        poolEntry.ClearPool();
                    }
                });
            }
        }
    }
}