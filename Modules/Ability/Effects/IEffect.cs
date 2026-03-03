using System;

namespace ModularFramework.Modules.Ability
{
    public interface IEffect<TTarget>
    {
        void Apply(TTarget target);
        void Cancel();
        DamageTarget ApplyTarget { get; }
        event Action<IEffect<TTarget>> OnCompleted;
    }

    public interface IEffectFactory<TTarget>
    {
        IEffect<TTarget> Create();
        bool IsTargetValid(TTarget target);
        DamageTarget ApplyTarget { get; }
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
    public enum DamageTarget : byte
    {
        None = 0,
        Player = 1 << 0,
        Equipment = 1 << 1,
        NPC = 1 << 2,
        Monster = 1 << 3,
        All = Player | Equipment | NPC | Monster
    }
}