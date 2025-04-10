using UnityEngine;

public abstract class ActionNode : BTNode
{
    [SerializeField] private string _animFlags;

    protected BTMarker runner = null;

    private void GetRunner() {
        runner = tree.Me.GetComponent<BTMarker>();
    }

    protected override void OnEnter()
    {
        if(!runner) GetRunner();
        if(!string.IsNullOrWhiteSpace(_animFlags)) runner.PlayAnim(_animFlags);
    }

}
