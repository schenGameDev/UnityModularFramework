using System.Collections.Generic;
using ModularFramework.Utility;
using UnityEngine;

public class DynamicPathNode : AstarAINode
{
    [SerializeField] private bool recalculateAtEachWaypoint;


    private Vector3 _finalTarget;
    private int _currentIndex = -1;
    private bool _started = false;
    private Vector3[] _currentPath;
    private List<Transform> _targets = new();
    
    protected override void OnEnter()
    {
        base.OnEnter();
        _started = false;
        _currentIndex = -1;
        _currentPath = enemyMove.Path;
        _targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        
    }


    protected override State OnUpdate()
    {
        if(_targets == null || _targets.Count == 0) return State.Failure;

        if(!_started) {
            var targetTf = _targets[0];
            if(targetTf) {
                _started = true;
                _finalTarget = targetTf.position;
            }
        } else if(recalculateAtEachWaypoint && (tree.AI.TargetReached || tree.AI.PathNotFound)) {
            var targetTf = _targets[0];
            if(targetTf) {
                _finalTarget = targetTf.position;
            }

        }

        if(_started) {
            if(_currentIndex==-1 || tree.AI.TargetReached || tree.AI.PathNotFound) {
                _currentIndex++;
                if(_currentIndex==_currentPath.Length) {
                    return tree.AI.TargetReached ? State.Success : State.Failure;
                }
                tree.AI.SetNewTarget(GetWaypoint(_finalTarget, _currentPath[_currentIndex]), enemyMove.speed, false);
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
        } else if(recalculateAtEachWaypoint) {
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

    DynamicPathNode()
    {
        description = "Follow target with a path relative to target";
    }
}
