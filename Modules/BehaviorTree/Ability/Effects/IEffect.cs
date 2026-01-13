using System;

public interface IEffect<TTarget>
{
    void Apply(TTarget target);
    void Cancel();
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