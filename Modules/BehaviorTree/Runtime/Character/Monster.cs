using ModularFramework.Modules.Ability;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("NPC/Monster", 0), DisallowMultipleComponent]
    public class Monster : Npc
    {
        public override DamageTarget TargetType => DamageTarget.Monster;
    }
}