using System;

namespace ModularFramework.Utility
{
    /// <summary>
    /// Parse string condition to BoolExprCondition struct for repeated use.
    /// </summary>
    public readonly struct BoolExprCondition<T> : IBoolExprCondition
    {
        private readonly Func<T,bool> _evaluator;
        
        private bool Evaluate(T currentValue) => _evaluator(currentValue);
        
        public bool Evaluate(object currentValue) => Evaluate((T)currentValue);
        public bool Evaluate(string currentValue, BoolExpressionEvaluator.ValueType valueType)
            =>Evaluate(BoolExpressionEvaluator.CastToObject(valueType, currentValue));

        public BoolExprCondition(Func<T,bool> evaluator) : this()
        {
            _evaluator = evaluator;
        }

        public static BoolExprCondition<T> AlwaysFalse() => new (_=>false);
        
        public static BoolExprCondition<T> AlwaysTrue() => new (_=>true);
    }
}