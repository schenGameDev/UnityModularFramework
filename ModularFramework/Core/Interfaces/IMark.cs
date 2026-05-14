using System;
using System.Collections.Generic;

namespace ModularFramework
{
    /// <summary>
    /// To be used together with Marker.cs. Its host should have [RequireComponent(typeof(Marker))]
    /// </summary>
    public interface IMark
    {
        public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes);
        public void UnregisterSelf();
    }
}