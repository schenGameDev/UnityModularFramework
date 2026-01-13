public class AnimationNode : ActionNode
{
    public string animationFlag;
    
    private float _timer;
    private bool _animComplete;

    protected override void OnEnter()
    {
        base.OnEnter();
        _animComplete = false;
        if (!string.IsNullOrEmpty(animationFlag))
        {
            tree.runner.PlayAnim(animationFlag, () => _animComplete=true);
        }
    }

    protected override State OnUpdate()
    {
        return _animComplete ? State.Success : State.Running;
    }
    public override BTNode Clone() {
        AnimationNode node = Instantiate(this);
        return node;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(animationFlag) ? "" : " (" + animationFlag + ")");
    }

    AnimationNode()
    {
        description = "Stand still and play animation";
    }
}
