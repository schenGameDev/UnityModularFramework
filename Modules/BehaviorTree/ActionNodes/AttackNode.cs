using UnityEngine;

public class AttackNode : ActionNode
{
    [SerializeField] private float _damage;
    [SerializeField] private string _damageType;
    [SerializeField] private float _range;
    [SerializeField] private float _delay;
    [SerializeField] private GameObject _vFX;
    
    private float _endTime;

    public override string Description() => "Melee attack player";


    protected override void OnEnter()
    {
        base.OnEnter();
        _endTime = Time.time + _delay;
        if(_vFX!=null) {
            Instantiate(_vFX, tree.Me.position,Quaternion.identity);
        }
    }

    protected override State OnUpdate()
    {
        if(Time.time > _endTime) {
            Attack();
            return State.Success;
        }
        return State.Running;
    }

    private void Attack() {
        if(Vector3.Distance(tree.Me.position,tree.Manager.player.position) < _range) {
            tree.Manager.playerStats.TakeDamage(_damage, _damageType);
        }
    }

    public override BTNode Clone() {
        AttackNode node = Instantiate(this);
        return node;
    }
}