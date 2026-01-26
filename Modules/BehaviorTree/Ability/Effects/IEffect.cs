using System;

public interface IEffect<TTarget>
{
    void Apply(TTarget target);
    void Cancel();
    bool IsTargetValid(TTarget target);
    event Action<IEffect<TTarget>> OnCompleted;
}

public interface IEffectFactory<TTarget>
{
    IEffect<TTarget> Create();
}



public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Electric,
    Poison
}

[Flags]
public enum DamageTarget
{
    None = 0,
    Player = 1,
    Equipment = 2,
    NPC = 4,
    Monster = 8,
    All = Player | Equipment | NPC | Monster
}