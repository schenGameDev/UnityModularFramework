using UnityEngine;

public class InsertKeywordNode : DecoratorNode
{
    public string keyword;
    public string value;
    
    protected override void OnEnter()
    {
        if (!string.IsNullOrEmpty(keyword) && !string.IsNullOrEmpty(value))
        {
            tree.blackboard.Add(keyword, value);
        }
    }

    protected override State OnUpdate()
    {
        return child.Run();
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as InsertKeywordNode;
        clone.keyword = keyword;
        clone.value = value;
        return clone;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(value)? base.ToString() : $"{keyword}={value}";
    }
    public override Color HeaderColor => new Color32(214, 100, 75,  255);

    InsertKeywordNode()
    {
        description = "Insert a keyword into the blackboard";
        titleCustomizable = false;
    }
}