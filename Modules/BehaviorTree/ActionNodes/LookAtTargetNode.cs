using UnityEngine;

public class LookAtTargetNode : ActionNode
{
    [SerializeField] private string _targetName;

    private Vector3 _target;
    private bool _started;

    public override string Description() => "Look at target and stand still";

    protected override void OnEnter()
    {
        base.OnEnter();
        _started = false;
    }

    protected override State OnUpdate()
    {
        UpdatePosition();
        if(_started) {
            runner.faceDirection = (_target -tree.Me.position).normalized;
        }
        return State.Running;
    }

    private void UpdatePosition()
    {
        var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
        if(targetTf) {
            if( !_started) _started = true;
            _target = targetTf.position;
        }
    }

    public override BTNode Clone() {
        LookAtTargetNode node = Instantiate(this);
        return node;
    }
}
