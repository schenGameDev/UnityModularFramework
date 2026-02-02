namespace ModularFramework.Utility
{
    public interface IBoolExprCondition
    {
        bool Evaluate(object currentValue);
        
        bool Evaluate(string currentValue, BoolExpressionEvaluator.ValueType valueType);
    }
}