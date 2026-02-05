using ModularFramework.Utility;
using UnityEngine;

public class TestKeywordOnExitNode : DecoratorNode
{
    public string keyword;
    public BoolExpressionEvaluator.ValueType dataType;
    public string successCondition;
    public string failCondition;
    public bool removeParameterOnRead = true;
    private IBoolExprCondition _successEvaluator;
    private IBoolExprCondition _failEvaluator;

    public override void Prepare()
    {
        base.Prepare();
        _successEvaluator = BoolExpressionEvaluator.Get(dataType, successCondition);
        _failEvaluator = BoolExpressionEvaluator.Get(dataType, failCondition);
    }

    protected override State OnUpdate()
    {
        var res = child.Run();
        if(res == State.Running) return res;
        
        var value = tree.blackboard.Get(keyword);
        if (!string.IsNullOrEmpty(value))
        {
            if(removeParameterOnRead) tree.blackboard.RemoveParameter(keyword);
            if (_successEvaluator.Evaluate(value, dataType))
            {
                return State.Success;
            }
            if (_failEvaluator.Evaluate(value, dataType))
            {
                return State.Failure;
            }
        }

        return res;
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as TestKeywordOnExitNode;
        clone.keyword = keyword;
        clone.successCondition = successCondition;
        clone.failCondition = failCondition;
        clone.dataType = dataType;
        clone.removeParameterOnRead = removeParameterOnRead;
        return clone;
    }

    public override string ToString()
    {
        string successPart = string.IsNullOrEmpty(successCondition) ? "" : $"{successCondition}: Succeed";
        string failPart = string.IsNullOrEmpty(failCondition) ? "" : $"{failCondition}: Fail";
        string combined = successPart + (successPart=="" || failPart == "" ? "" : " ") + failPart;
        return combined == "" ? base.ToString() : $"{keyword} ({combined})";
    }

    public override Color HeaderColor => new Color32(214, 100, 75,  255);

    TestKeywordOnExitNode()
    {
        description = "Read a keyword in the blackboard to alter the result of the child node on exit";
        titleCustomizable = false;
    }
}