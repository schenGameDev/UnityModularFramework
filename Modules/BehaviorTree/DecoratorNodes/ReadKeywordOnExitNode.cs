using UnityEngine;

public class ReadKeywordOnExitNode : DecoratorNode
{
    public string keyword;
    public string successValue;
    public string failValue;
    

    protected override State OnUpdate()
    {
        var res = child.Run();
        if(res == State.Running) return res;
        
        var value = tree.blackboard.Get(keyword);
        if (!string.IsNullOrEmpty(value))
        {
            tree.blackboard.RemoveParameter(keyword);
            if (value == successValue)
            {
                return State.Success;
            }
            if (value == failValue)
            {
                return State.Failure;
            }
        }

        return res;
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as ReadKeywordOnExitNode;
        clone.keyword = keyword;
        clone.successValue = successValue;
        clone.failValue = failValue;
        return clone;
    }

    public override string ToString()
    {
        string successPart = string.IsNullOrEmpty(successValue) ? "" : $"{successValue}: Succeed";
        string failPart = string.IsNullOrEmpty(failValue) ? "" : $"{failValue}: Fail";
        string combined = successPart + (successPart=="" || failPart == "" ? "" : " ") + failPart;
        return combined == "" ? base.ToString() : $"{keyword} ({combined})";
    }

    public override Color HeaderColor => new Color32(214, 100, 75,  255);

    ReadKeywordOnExitNode()
    {
        description = "Read a keyword in the blackboard to alter the result of the child node";
    }
}