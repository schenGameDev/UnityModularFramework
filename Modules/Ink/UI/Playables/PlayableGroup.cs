using System.Collections.Generic;

public abstract class PlayableGroup : Playable
{
    [SavableState] protected string CurrentState;

    public abstract IEnumerable<string> GetStates();

}