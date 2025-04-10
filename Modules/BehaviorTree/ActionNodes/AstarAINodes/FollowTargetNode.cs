using UnityEngine;

public class FollowTargetNode : AstarAINode
{
    [SerializeField] private string _targetName;
    private Transform _target;

    private bool _started = false;
    public override string Description() => "Follow target";

    protected override void OnEnter()
    {
        base.OnEnter();
        _started = false;
        _target = null;
    }


    protected override State OnUpdate()
    {
        if(string.IsNullOrEmpty(_targetName)) return State.Failure;
        if(!_started) {
            _target = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(_target) {
                _started = true;
                tree.AI.SetNewTarget(_target,speed,true);
            }
        } else {
            var newTarget = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(newTarget != _target) {
                _target = newTarget;
                if(_target == null) tree.AI.SetNewTarget(_target.position,speed,true);
                else tree.AI.SetNewTarget(_target,speed,true);
            }
            if(tree.AI.PathNotFound) return State.Failure;
        }
        return State.Running;
    }

    public override BTNode Clone() {
        FollowTargetNode node = Instantiate(this);
        return node;
    }
}
