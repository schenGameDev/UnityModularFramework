using System.Linq;
using EditorAttributes;
using UnityEngine;
using UnityModularFramework;

public class AbilityNode : ActionNode,IReady
{
    [ShowInInspector,ReadOnly] private string _abilityName;
    
    private EnemyAbility _enemyAbility;
    private State _abilityEndState = State.Running;
    public bool Ready => _enemyAbility is null || _enemyAbility.Ready;

    protected override void OnEnter()
    {
        base.OnEnter();
        _abilityName ??= tree.blackboard.Get(BTBlackboard.KEYWORD_ABILITY_NAME);
        _enemyAbility ??= GetComponentInMe<EnemyAbility>(_abilityName);
        if (_abilityName == null)
        {
            Debug.LogError($"AbilityName not found in blackboard for {title} Node");
        }
        _abilityEndState = State.Running;
        CastAbility();
    }

    protected override State OnUpdate()
    {
        return _abilityEndState;
    }

    private void CastAbility() {
        var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        _enemyAbility.Cast(targets?.Select(t => t.GetComponent<IDamageable>()).ToList(), OnCastComplete);
    }
    
    private void OnCastComplete(bool success) {
        _abilityEndState = success ? State.Success : State.Failure;
    }

    public override BTNode Clone() {
        AbilityNode node = Instantiate(this);
        return node;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(_abilityName)? "" : $" ({_abilityName})");
    }

    AbilityNode()
    {
        description = "Cast ability on player \n\n" +
                      "<b>Requires</b>: FindTargetInAbilityRangeNode/TestTargetInAbilityRangeNode as parent node\n" +
                      "<b>Requires</b>: 'AbilityName' in blackboard";
    }
}