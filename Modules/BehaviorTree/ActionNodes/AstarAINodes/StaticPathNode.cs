using UnityEngine;

public class StaticPathNode : AstarAINode
{
    [SerializeField] private string _pathName;
    private Vector3[] _currentPath;


    private int _currentIndex = 0;
    private Vector3 _target;

    public override string Description() => "Follow a static path";

    protected override void OnEnter()
    {
        base.OnEnter();
        _currentIndex = 0;
        _currentPath = tree.Me.GetComponent<WaypointCollection>().GetPath(_pathName);
        SetTarget();
    }


    protected override State OnUpdate()
    {
        if(tree.AI.PathNotFound || tree.AI.FixedTargetReached) {
            _currentIndex++;
            if(IsEndOfPath()) return tree.AI.FixedTargetReached? State.Success : State.Failure;
            SetTarget();
        }

        return State.Running;
    }

    private void SetTarget() {
        _target = _currentPath[_currentIndex];
        tree.AI.SetNewTarget(_target, speed, true);
    }

    private bool IsEndOfPath() => _currentIndex ==_currentPath.Length;

    public override BTNode Clone() {
        StaticPathNode node = Instantiate(this);
        return node;
    }
}
