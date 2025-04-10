using ModularFramework.Utility;
using UnityEngine;

public class DynamicPathNode : AstarAINode
{
    [SerializeField] private string _targetName;
    [SerializeField] private string _pathName;
    [SerializeField] private bool _recalculateAtEachWaypoint;


    private Vector3 _finalTarget;
    private int _currentIndex = -1;
    private Vector3[] _currentPath;
    private bool _started = false;
    public override string Description() => "Follow target with a path";

    protected override void OnEnter()
    {
        base.OnEnter();
        _started = false;
        _currentIndex = -1;
        _currentPath = tree.Me.GetComponent<WaypointCollection>().GetPath(_pathName);
    }


    protected override State OnUpdate()
    {
        if(string.IsNullOrEmpty(_targetName)) return State.Failure;

        if(!_started) {
            var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(targetTf) {
                _started = true;
                _finalTarget = targetTf.position;
            }
        } else if(_recalculateAtEachWaypoint && (tree.AI.FixedTargetReached || tree.AI.PathNotFound)) {
            var targetTf = tree.Manager.sensorSystem.GetTransform(_targetName, false);
            if(targetTf) {
                _finalTarget = targetTf.position;
            }

        }

        if(_started) {
            if(_currentIndex==-1 || tree.AI.FixedTargetReached || tree.AI.PathNotFound) {
                _currentIndex++;
                if(_currentIndex==_currentPath.Length) {
                    return tree.AI.FixedTargetReached ? State.Success : State.Failure;
                }
                tree.AI.SetNewTarget(GetWaypoint(_finalTarget, _currentPath[_currentIndex]), speed, false);
            }
        }

        return State.Running;
    }


    private Vector3 GetWaypoint(Vector3 refPoint,Vector3 waypoint) {
        // recalculate path
        var newZ = refPoint - tree.Me.position;
        newZ.y = 0;
        Quaternion rot = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, newZ,Vector3.up), Vector3.up);

        return PhysicsUtil.FindGroundPosition(RotatePointBySelf(waypoint, tree.Me.position, rot));
    }

    private void SetTarget(Vector3 target) {
        if(!_started) {
            _started = true;
            _finalTarget = target;
        } else if(_recalculateAtEachWaypoint) {
            _finalTarget = target;
        }
    }

    public override BTNode Clone() {
        DynamicPathNode node = Instantiate(this);
        return node;
    }

    private Vector3 RotatePointBySelf(Vector3 pointRelativeToZ, Vector3 self, Quaternion rot) {
        var v = pointRelativeToZ - self;
        v.y=0;
        v = rot * v;
        return self + v;
    }
}
