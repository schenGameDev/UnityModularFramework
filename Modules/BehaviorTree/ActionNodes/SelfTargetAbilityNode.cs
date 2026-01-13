using UnityEngine;

public class SelfTargetAbilityNode : ActionNode
{
    [SerializeField] private string abilityName;
    
    private EnemyAbility _enemyAbility;
    private State _abilityEndState = State.Running;

    protected override void OnEnter()
    {
        base.OnEnter();
        _enemyAbility ??= GetComponentInMe<EnemyAbility>(abilityName);
        if (!_enemyAbility.TargetAtSelf)
        {
            Debug.LogError($"{ToString()} requires {abilityName} to target self");
        }
        
        _abilityEndState = State.Running;
        CastAbility();
    }

    protected override State OnUpdate()
    {
        return _abilityEndState;
    }

    private void CastAbility() {
        _enemyAbility.Cast(null, OnCastComplete);
    }
    
    private void OnCastComplete(bool success) {
        _abilityEndState = success ? State.Success : State.Failure;
    }

    public override BTNode Clone() {
        SelfTargetAbilityNode node = Instantiate(this);
        return node;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(abilityName)? "" : $" ({abilityName})");
    }
    
    public SelfTargetAbilityNode()
    {
        description = "Cast ability on self or AOE around self, no target will be provided";
    }
}