using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// A Container for UI Items
    /// </summary>
    public abstract class UIItemCarrier<TData, TUIItem> : MonoBehaviour
    {
        [SerializeField] protected TUIItem entryPrefab;
        [SerializeField] protected Transform parent;

        public void Show()
        {
            Setup(GetEntries());
        }

        protected abstract List<TData> GetEntries();

        protected abstract void Setup(List<TData> entries);
    }
}