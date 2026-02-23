using System.Collections.Generic;
using ModularFramework.Commons;

namespace ModularFramework.Modules.Ink
{
    public interface ISavable
    {
        public string Id { get; }

        public Dictionary<string, AnyValue> GetState()
        {
            return SavableState.GetSavableStates(this);
        }

        public void RestoreState(Dictionary<string, AnyValue> savedStates)
        {
            SavableState.SetSavableStates(this, savedStates);
        }

        public void Load();
    }
}

