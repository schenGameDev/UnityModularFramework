using System.Linq;
using UnityEngine;

/// <summary>
/// Behavior tree node that casts a predetermined ability on targets retrieved from the blackboard.
/// </summary>
public class NamedAbilityNode : ActionNode
{
    private BTAbility _btAbility;
    private State _abilityEndState = State.Running;

    public override void Prepare()
    {
        if (!title.IsEmpty())
        {
            _btAbility = GetComponentInMe<BTAbility>(title);
        }

        if (_btAbility == null)
        {
            Debug.LogError($"BTAbility of {title} component not found on {tree.Me.name}");
        }
    }

    protected override void OnEnter()
    {
        base.OnEnter();
        _abilityEndState = State.Running;
        CastAbility();
    }

    protected override State OnUpdate()
    {
        return _abilityEndState;
    }

    private void CastAbility() {
        var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        _btAbility.Cast(targets?.Select(t => t.GetComponent<IDamageable>()).ToList(), OnCastComplete);
    }
    
    private void OnCastComplete(bool success) {
        _abilityEndState = success ? State.Success : State.Failure;
    }

    public override BTNode Clone() {
        NamedAbilityNode node = Instantiate(this);
        return node;
    }

    NamedAbilityNode()
    {
        description = "Cast ability on target, where the ability is predetermined \n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
        titleName = "Ability Name";
    }
}