using UnityEngine;
namespace ModularFramework
{
    /// <summary>
    /// Accepts Markers in scene
    /// </summary>
    public interface IRegistrySO {
        public void Register(Transform transform);
        public void Unregister(Transform transform);
    }
}