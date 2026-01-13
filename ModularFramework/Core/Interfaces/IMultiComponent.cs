using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// marks the components that can have multiple instances on the same GameObject, which can be identified by UniqueId
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMultiComponent<T> where T : Component  {
        string UniqueId { get;  }
    }
}