using System;
using System.Collections.Generic;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

[RequireComponent(typeof(Marker))]
public class InkPrinter : TextPrinter,IMark
{
    #region IRegistrySO
    public virtual List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        
        if (alreadyRegisteredTypes.Contains(typeof(InkUIIntegrationSO))) return new ();
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Register(transform);
        return new () {typeof(InkUIIntegrationSO)};
    }

    public virtual void UnregisterSelf()
    {
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Unregister(transform);
    }
    #endregion
}