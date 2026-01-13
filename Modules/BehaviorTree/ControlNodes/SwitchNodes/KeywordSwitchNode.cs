using EditorAttributes;

public class KeywordSwitchNode : SwitchNode
{
    public string keyword;
    public string value;
    [Rename("Not Found = N")] public bool notFoundIsN = true;

    protected override bool Condition()
    {
        var v = tree.blackboard.Get(keyword);
        return (v != null && v == value) || (v == null && !notFoundIsN);
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as KeywordSwitchNode;
        clone.keyword = keyword;
        clone.value = value;
        clone.notFoundIsN = notFoundIsN;
        return clone;
    }
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(value) 
            ? base.ToString() : $"{keyword}={value}?";
    }
    
    KeywordSwitchNode()
    {
        description = "Read a keyword in the blackboard upon entering to determine which child to run";
    }
}