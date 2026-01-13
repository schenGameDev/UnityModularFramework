using UnityEngine;

public class StaticPathNode : AstarAINode
{
    [SerializeField] private string pathName;
    private Vector3[] _currentPath;


    private int _currentIndex = 0;
    private Vector3 _target;

    protected override void OnEnter()
    {
        base.OnEnter();
        _currentIndex = 0;
        _currentPath = tree.Me.GetComponent<WaypointCollection>().GetPath(pathName);
        SetTarget();
        BtMove.Move();
    }


    protected override State OnUpdate()
    {
        if(tree.AI.PathNotFound || tree.AI.TargetReached) {
            _currentIndex++;
            if(IsEndOfPath()) return tree.AI.TargetReached? State.Success : State.Failure;
            SetTarget();
        }

        return State.Running;
    }

    private void SetTarget() {
        _target = _currentPath[_currentIndex];
        tree.AI.SetNewTarget(_target, BtMove.speed, true);
    }

    private bool IsEndOfPath() => _currentIndex ==_currentPath.Length;

    public override BTNode Clone() {
        StaticPathNode node = Instantiate(this);
        return node;
    }
    
    StaticPathNode()
    {
        description = "Follow a static path relative to oneself";
    }
}
