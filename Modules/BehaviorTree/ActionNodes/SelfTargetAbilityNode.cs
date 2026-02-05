using UnityEngine;

public class SelfTargetAbilityNode : ActionNode
{
    [SerializeField] private string abilityName;
    
    private BTAbility _btAbility;
    private State _abilityEndState = State.Running;

    public override void Prepare()
    {
        base.Prepare();
        _btAbility = GetComponentInMe<BTAbility>(abilityName);
        if (!_btAbility.TargetAtSelf)
        {
            Debug.LogError($"{ToString()} requires {abilityName} to target self");
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
        _btAbility.Cast(null, OnCastComplete);
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