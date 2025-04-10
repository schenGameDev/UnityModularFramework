public class ExternalTriggerNode : SwitchNode
{
    public BoolEventChannelSO EventChannel;

    private bool _isTriggered = false;

    protected override void OnEnter()
    {
        _isTriggered = false;

    }
    protected override bool IsSwitchState() {
        return _isTriggered;
    }

    public override string Description() => "Trigger By external event";

    private void SetTrigger(bool isTriggerred) => _isTriggered = isTriggerred;

    private void OnEnable() {
        EventChannel.AddListener(SetTrigger);
    }

    private void OnDisable() {
        EventChannel.RemoveListener(SetTrigger);
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as ExternalTriggerNode;
        clone.EventChannel = EventChannel;
        return clone;
    }
}
