using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;
using ValueType = ModularFramework.Utility.BooleanExpressionEvaluator.ValueType;

public class KeywordSwitchNode : SwitchNode
{
    public string keyword;
    public ValueType dataType;
    [Tooltip("string/bool must start with \"=/!=\", int/float can also start with \">/</>=/<=\"")] 
    public string yesCondition;
    [Rename("Not Found = No")] public bool notFoundIsN = true;

    protected override bool Condition()
    {
        var v = tree.blackboard.Get(keyword);
        if(v == null) return !notFoundIsN;
        return BooleanExpressionEvaluator.Evaluate(v, dataType, yesCondition);
    }
    
    
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as KeywordSwitchNode;
        clone.keyword = keyword;
        clone.dataType = dataType;
        clone.yesCondition = yesCondition;
        clone.notFoundIsN = notFoundIsN;
        return clone;
    }
    
    public override string ToString()
    {
        return string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(yesCondition) 
            ? base.ToString() : $"{keyword}{yesCondition}?";
    }
    
    KeywordSwitchNode()
    {
        description = "Read a keyword in the blackboard upon entering to determine which child to run";
    }
}