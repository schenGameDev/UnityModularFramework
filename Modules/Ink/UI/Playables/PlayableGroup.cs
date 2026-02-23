using System.Collections.Generic;

namespace ModularFramework.Modules.Ink
{
    public abstract class PlayableGroup : Playable
    {
        [SavableState] protected string CurrentState;

        public abstract IEnumerable<string> GetStates();

    }
}