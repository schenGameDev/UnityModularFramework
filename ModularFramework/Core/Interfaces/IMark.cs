using System;
using System.Collections.Generic;

namespace ModularFramework
{
    public interface IMark
    {
        public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes);
        public void UnregisterSelf();
    }
}