using System.Linq;
using EditorAttributes;
using UnityEngine;

/// <summary>
/// Behavior tree node that casts an ability on targets retrieved from the blackboard.
/// </summary>
public class AnonymousAbilityNode : SingletonNode
{
    [ShowInInspector,ReadOnly] private string _abilityName;
    
    private BTAbility _btAbility;
    private State _abilityEndState = State.Running;

    public override Color HeaderColor => new Color32(55, 171, 173, 255);

    protected override void OnEnter()
    {
        base.OnEnter();
        var abilityName = tree.blackboard.Get(BTBlackboard.KEYWORD_ABILITY_NAME);
        if (abilityName != _abilityName || _btAbility == null)
        {
            _abilityName = abilityName;
            if (_abilityName == null)
            {
                Debug.LogError($"AbilityName not found in blackboard for {title} Node");
            }
            
            _btAbility = GetComponentInMe<BTAbility>(_abilityName);
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
        _btAbility.Cast(targets?.Select(t => t.GetComponent<IDamageable>()).ToList(), OnCastComplete);
    }
    
    private void OnCastComplete(bool success) {
        _abilityEndState = success ? State.Success : State.Failure;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(_abilityName)? base.ToString() : _abilityName;
    }

    AnonymousAbilityNode()
    {
        description = "Cast ability on target, where the ability is not predetermined \n\n" +
                      "<b>Requires</b>: 'AbilityName' in blackboard\n"+
                      "<b>Requires</b>: 'Target' in blackboard";
        titleCustomizable = false;
    }
}