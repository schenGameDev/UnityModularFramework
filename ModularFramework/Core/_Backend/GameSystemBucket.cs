using System;
using System.Collections.Generic;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework
{
    
    [CreateAssetMenu(fileName = "GameSystemBucket_SO", menuName = "Bucket/Game System Bucket")]
    public class GameSystemBucket : ScriptableObject
    {
        [SerializeField] private GameSystem[] systems;

        public void RegisterAll()
        {
            if (systems == null) return;
            
            systems = ValidateSystems(systems);
            
            Registry<GameSystem>.Clear();
            
            foreach (var sys in systems)
            {
                DebugUtil.DebugLog($"Register: {sys.GetType().Name}");
                sys.InjectRegistry();
                DebugUtil.DebugLog($"Start: {sys.GetType().Name}");
                sys.Start();
            }
        }

        public void UnregisterAll()
        {
            if (systems == null) return;
            foreach(var sys in systems) 
            {
                DebugUtil.DebugLog($"Destroy: {sys.GetType().Name}");
                sys.Destroy();
                DebugUtil.DebugLog($"Unregister: {sys.GetType().Name}");
                sys.ClearRegistry();
            }
            Registry<GameSystem>.Clear();
            DebugUtil.DebugLog("GameSystem registry cleared.");
        }
        
        public void ForEach(Action<GameSystem> action)
        {
            if (systems == null) return;
            foreach (var sys in systems) action(sys);
        }

        #region Static

        private static GameSystem[] ValidateSystems(GameSystem[] systems)
        {
            if (systems == null || systems.Length == 0) return systems;
            
            var systemTypes = new HashSet<Type>();
            var newSystems = new List<GameSystem>();
            foreach (var sys in systems)
            {
                if (sys == null) continue;
                Type systemType = sys.GetType();
                if (!systemTypes.Add(systemType))
                {
                    Debug.LogError($"Remove duplicate system: {systemType.Name}.");
                    continue;
                }
                newSystems.Add(sys);
            }
            return newSystems.ToArray();
        }

        #endregion
    }
}