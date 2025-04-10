using UnityEngine;

public class TeleportNode : AstarAINode
{
    [SerializeField] private bool _isTargetMe;
    [SerializeField] private string _targetName;
    [SerializeField] private Vector3[] _relativePositions;
    private bool _started = false;
    public override string Description() => "Teleport around target";

    protected override void OnEnter()
    {
        base.OnEnter();
        if(_isTargetMe) {
            _started = true;
            Teleport(tree.Me.position);
        }
        _started = false;

    }

    protected override State OnUpdate()
    {
        if(_started) return State.Success;
        if(string.IsNullOrEmpty(_targetName)) return State.Failure;

        if(!_started) {
            var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(targetTf) {
                _started = true;
                Teleport(targetTf.position);
            }
        }


        return State.Running;
    }

    private void Teleport(Vector3 target) {
        if(!isActive) return;
        if(!_started) {
            _started = true;
            Vector3 teleportPos = GetTeleportPosition(target);
            tree.AI.Teleport(teleportPos);
        }
    }

    private Vector3 GetTeleportPosition(Vector3 target) {
        return target + _relativePositions[Random.Range(0, _relativePositions.Length)];
    }

    public override BTNode Clone() {
        TeleportNode node = Instantiate(this);
        return node;
    }
}