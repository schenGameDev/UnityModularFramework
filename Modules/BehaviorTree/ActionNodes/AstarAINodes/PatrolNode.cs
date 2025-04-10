using UnityEngine;

public class PatrolNode : AstarAINode
{
    private enum Type {BACK_AND_FORTH,LOOP}
    [SerializeField] private Type _type;
    [SerializeField] private string _pathName;
    private Vector3[] _currentPath;


    private int _currentIndex = 0;
    private bool _positiveDirection = true;
    private Vector3 _target;

    public override string Description() => "Patrol along path";

    protected override void OnEnter()
    {
        base.OnEnter();
        _currentIndex = 0;
        _positiveDirection =true;
        _currentPath = tree.Me.GetComponent<WaypointCollection>().GetPath(_pathName);
        SetTarget();
    }


    protected override State OnUpdate()
    {
        switch(_type) {
            case Type.BACK_AND_FORTH:
                if(tree.AI.PathNotFound || tree.AI.FixedTargetReached) {
                    if(_positiveDirection) _currentIndex++;
                    else _currentIndex--;

                    if(IsEndOfPath()) {
                        _positiveDirection = !_positiveDirection;
                        if(_positiveDirection) _currentIndex=1;
                        else _currentIndex -=2;
                    }
                    SetTarget();
                }
                break;
            case Type.LOOP:
                if(tree.AI.PathNotFound || tree.AI.FixedTargetReached) {
                    _currentIndex++;
                    if(IsEndOfPath()) _currentIndex=0;
                    SetTarget();
                }
                break;
        }

        return State.Running;
    }

    private void SetTarget() {
        _target = _currentPath[_currentIndex];
        tree.AI.SetNewTarget(_target, speed, true);
    }

    private bool IsEndOfPath() => (_positiveDirection && _currentIndex ==_currentPath.Length) ||
                                    (!_positiveDirection && _currentIndex <0);

    public override BTNode Clone() {
        PatrolNode node = Instantiate(this);
        return node;
    }
}
