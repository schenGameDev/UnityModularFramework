using UnityEngine;

public class FleeTargetNode : AstarAINode
{
    [SerializeField] private float _distance;
    [SerializeField] private string _targetName;

    private bool _started = false;
    public override string Description() => "Flee from target";

    protected override void OnEnter()
    {
        base.OnEnter();
        _started = false;
    }


    protected override State OnUpdate()
    {
        if(string.IsNullOrEmpty(_targetName)) return State.Failure;
        if(!_started) {
            var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(targetTf) {
                _started = true;
                tree.AI.SetNewTargetUnFixed(GetFleeTarget(targetTf.position),speed,true);
            }
        } else {
            var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(targetTf) {
                tree.AI.UpdateTarget(GetFleeTarget(targetTf.position));
            }

            if(tree.AI.PathNotFound) return State.Failure;
        }

        return State.Running;
    }

    private Vector3 GetFleeTarget(Vector3 targetToFleeFrom) {
        var dir = (tree.Me.position - targetToFleeFrom).normalized;
        return _distance * dir + tree.Me.position;
    }

    public override BTNode Clone() {
        FleeTargetNode node = Instantiate(this);
        return node;
    }
}
