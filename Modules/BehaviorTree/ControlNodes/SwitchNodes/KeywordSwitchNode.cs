using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;
using ValueType = ModularFramework.Utility.BoolExpressionEvaluator.ValueType;

public class KeywordSwitchNode : SwitchNode
{
    public string keyword;
    public bool isTransform;
    [HideField(nameof(isTransform))] public ValueType dataType;
    [HideField(nameof(isTransform))]
    [Tooltip("string/bool must start with \"=/!=\", int/float can also start with \">/</>=/<=\"")] 
    public string yesCondition;
    [Rename("Not Found = No")] public bool notFoundIsN = true;
    private IBoolExprCondition _yesEvaluator;

    public override void Prepare()
    {
        _yesEvaluator= BoolExpressionEvaluator.Get(dataType, yesCondition);
    }
    
    protected override bool Condition()
    {
        if (started && !tree.blackboard.changed) return enterConditionState;
        
        if (isTransform)
        {
            var tf = tree.blackboard.Get<Transform>(keyword);
            return tf is not null && tf.Count>0;
        }
        
        var v = tree.blackboard.Get(keyword);
        if(v == null) return !notFoundIsN;

        return _yesEvaluator.Evaluate(v, dataType);
    }
    
    
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as KeywordSwitchNode;
        clone.keyword = keyword;
        clone.isTransform = isTransform;
        clone.dataType = dataType;
        clone.yesCondition = yesCondition;
        clone.notFoundIsN = notFoundIsN;
        return clone;
    }
    
    public override string ToString()
    {
        if (isTransform)
        {
            return string.IsNullOrEmpty(keyword)? base.ToString() : $"{keyword} Exists";
        }
        
        return string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(yesCondition) 
            ? base.ToString() : $"{keyword}{yesCondition}?";
    }
    
    KeywordSwitchNode()
    {
        description = "Read a keyword in the blackboard upon entering to determine which child to run";
        titleCustomizable = false;
    }
}