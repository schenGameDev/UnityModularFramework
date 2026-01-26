using UnityEngine;

public class CopyKeywordNode : DecoratorNode
{
    public string fromKeyword;
    public string toKeyword;
    public bool isGameObject;
    
    protected override void OnEnter()
    {
        if (!string.IsNullOrEmpty(fromKeyword) && !string.IsNullOrEmpty(toKeyword))
        {
            if (isGameObject)
            {
                var value = tree.blackboard.Get<Transform>(fromKeyword);
                tree.blackboard.Add(toKeyword, value);
            }
            else
            {
                var value = tree.blackboard.Get(fromKeyword);
                tree.blackboard.Add(toKeyword, value);
            }
            
        }
    }

    protected override State OnUpdate()
    {
        return child.Run();
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as CopyKeywordNode;
        clone.fromKeyword = fromKeyword;
        clone.toKeyword = toKeyword;
        clone.isGameObject = isGameObject;
        return clone;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(fromKeyword) || string.IsNullOrEmpty(toKeyword)? base.ToString() : $"{fromKeyword}=>{toKeyword}";
    }
    public override Color HeaderColor => new Color32(214, 100, 75,  255);
    
    CopyKeywordNode()
    {
        description = "Copy the value of a keyword to another keyword";
        titleCustomizable = false;
    }
}