using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    
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
                sys.InjectRegistry();
                sys.Start();
            }
        }

        public void UnregisterAll()
        {
            if (systems == null) return;
            foreach(var sys in systems) {
                sys.Destroy();
                sys.ClearRegistry();
            }
            Registry<GameSystem>.Clear();
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