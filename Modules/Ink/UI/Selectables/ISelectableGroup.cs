using ModularFramework;

public interface ISelectableGroup : IResetable, IMark
{
    public string ChoiceGroupName { get; }
    public bool EnableOnAwake { get; }
    public void Activate(InkChoice choiceInfo, bool showHiddenChoice);
    public void Select(int index);

}