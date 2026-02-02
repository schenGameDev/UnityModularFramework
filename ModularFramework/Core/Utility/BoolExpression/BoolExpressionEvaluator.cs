using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Utility
{
    /// <summary>
    /// Evaluates boolean expressions given as strings.
    /// </summary>
    public static class BoolExpressionEvaluator
    {
        public enum ValueType { Int, Float, Bool, String, Vector3 }
        private static readonly HashSet<char> OPERATORS = new () { '>', '<', '=', '!' };

        public static IBoolExprCondition Get(ValueType valueType, string expression)
        {
            var conditionStr = TrimOperator(expression, out string op);
            if(conditionStr.IsEmpty()) return BoolExprCondition<bool>.AlwaysFalse();
            return valueType switch
            {
                ValueType.Int => int.TryParse(conditionStr, out int conditionValue)
                    ? new BoolExprCondition<int>(GetIntEvaluator(op, conditionValue)) 
                    : UnableToParse<int>(conditionStr),
                ValueType.Bool => bool.TryParse(conditionStr, out bool conditionValue)
                    ? new BoolExprCondition<bool>(GetBoolEvaluator(op, conditionValue)) 
                    : UnableToParse<bool>(conditionStr),
                ValueType.Float => float.TryParse(conditionStr, out float conditionValue)
                    ? new BoolExprCondition<float>(GetFloatEvaluator(op, conditionValue)) 
                    : UnableToParse<float>(conditionStr),
                ValueType.String => new BoolExprCondition<string>(GetStringEvaluator(op, conditionStr)),
                ValueType.Vector3 => TryParseVector3(conditionStr, out var vector3Condition)
                        ? new BoolExprCondition<Vector3>(GetVector3Evaluator(op, vector3Condition)) 
                        : UnableToParse<Vector3>(conditionStr),
                _ => throw new NotImplementedException("Unsupported value type " + valueType),
            };
        }
        
        public static object CastToObject(ValueType valueType, string valueStr)
        {
            return valueType switch
            {
                ValueType.Int => int.Parse(valueStr),
                ValueType.Bool => bool.Parse(valueStr),
                ValueType.Float => float.Parse(valueStr),
                ValueType.String => valueStr,
                ValueType.Vector3 => ParseVector3(valueStr),
                _ => throw new NotImplementedException("Unsupported value type " + valueType),
            };
        }

        private static BoolExprCondition<T> UnableToParse<T>(string conditionStr)
        {
            Debug.LogError(conditionStr.IsEmpty()
                ? "condition can not be empty for type " + typeof(T).Name
                : $"Cannot parse {conditionStr} into type " + typeof(T).Name);
            return BoolExprCondition<T>.AlwaysFalse();
        }

        private static Func<bool,bool> GetBoolEvaluator(string operatorStr, bool conditionValue)
        {
            if (operatorStr == "!=") return currentValue => currentValue != conditionValue;
            if (operatorStr is "=" or "==" or "") return currentValue => currentValue == conditionValue;
            IncompatibleOperator(operatorStr, ValueType.Bool);
            return _=>false;
        }
        
        private static Func<string,bool> GetStringEvaluator(string operatorStr, string conditionValue)
        {
            if (operatorStr == "!=") return currentValue => currentValue != conditionValue;
            if (operatorStr is "=" or "==" or "") return currentValue => currentValue == conditionValue;
            IncompatibleOperator(operatorStr, ValueType.String);
            return _=>false;
        }
        
        private static Func<Vector3,bool> GetVector3Evaluator(string operatorStr, Vector3 conditionValue)
        {
            if (operatorStr == "!=") return currentValue => currentValue != conditionValue;
            if (operatorStr is "=" or "==" or "") return currentValue => currentValue == conditionValue;
            IncompatibleOperator(operatorStr, ValueType.Vector3);
            return _=>false;
        }
        
        private static Func<int,bool> GetIntEvaluator(string operatorStr, int conditionValue)
        {
            switch (operatorStr)
            {
                case ">=":
                    return currentValue => currentValue >= conditionValue;
                case "<=":
                    return currentValue => currentValue <= conditionValue;
                case ">":
                    return currentValue => currentValue > conditionValue;
                case "<":
                    return currentValue => currentValue < conditionValue;
                case "!=":
                    return currentValue => currentValue != conditionValue;
                case "" or "=" or "==":
                    return currentValue => currentValue == conditionValue;
                default:
                    IncompatibleOperator(operatorStr, ValueType.Int);
                    return _ => false;
            }
        }
        
        private static Func<float,bool> GetFloatEvaluator(string operatorStr, float conditionValue)
        {
            switch (operatorStr)
            {
                case ">=":
                    return currentValue => currentValue >= conditionValue;
                case "<=":
                    return currentValue => currentValue <= conditionValue;
                case ">":
                    return currentValue => currentValue > conditionValue;
                case "<":
                    return currentValue => currentValue < conditionValue;
                case "!=":
                    return currentValue => !Mathf.Approximately(currentValue, conditionValue);
                case "" or "=" or "==":
                    return currentValue => Mathf.Approximately(currentValue, conditionValue);
                default:
                    IncompatibleOperator(operatorStr, ValueType.Float);
                    return _ => false;
            }
        }
        
        public static bool EvaluateStringCondition(string currentValue, string operatorStr, string condition)
        {
            if (operatorStr == "!=") return currentValue != condition;
            if (operatorStr is "=" or "==" or "") return currentValue == condition;
            return IncompatibleOperator(operatorStr, ValueType.Bool);
        }
        
        public static bool EvaluateBoolCondition(bool currentValue, string operatorStr, bool condition)
        {
            if (operatorStr == "!=") return currentValue != condition;
            if (operatorStr is "=" or "==" or "") return currentValue == condition;
            return IncompatibleOperator(operatorStr, ValueType.Bool);
        }
        
        public static bool EvaluateVector3Condition(Vector3 currentValue, string operatorStr, Vector3 condition)
        {
            if (operatorStr == "!=") return currentValue != condition;
            if (operatorStr is "=" or "==" or "") return currentValue == condition;
            return IncompatibleOperator(operatorStr, ValueType.Vector3);
        }
        
        public static bool EvaluateIntCondition(int currentValue, string operatorStr, int condition)
        {
            return operatorStr switch
            {
                ">=" => currentValue >= condition,
                "<=" => currentValue <= condition,
                ">" => currentValue > condition,
                "<" => currentValue < condition,
                "!=" => currentValue != condition,
                "" or "=" or "==" => currentValue == condition,
                _ => IncompatibleOperator(operatorStr, ValueType.Int)
            };
        }
        
        public static bool EvaluateFloatCondition(float currentValue, string operatorStr, float condition)
        {
            return operatorStr switch
            {
                ">=" => currentValue >= condition,
                "<=" => currentValue <= condition,
                ">" => currentValue > condition,
                "<" => currentValue < condition,
                "!=" => !Mathf.Approximately(currentValue, condition),
                "" or "=" or "==" => Mathf.Approximately(currentValue, condition),
                _ => IncompatibleOperator(operatorStr, ValueType.Float)
            };
        }
        
        private static bool IncompatibleOperator(string operatorStr, ValueType valueType)
        {
            Debug.LogError($"{operatorStr} is incompatible with the value type {valueType.ToString()}");
            return false;
        }
        
        private static string TrimOperator(string condition, out string opr)
        {
            condition = condition.Trim();
            opr = "";
            if(condition.IsEmpty()) return string.Empty;
            int i = 0;
            do
            {
                if (OPERATORS.Contains(condition[i]))
                {
                    opr += condition[i];
                }
                else
                {
                    break;
                }
            } while(++i<2 && i<condition.Length);
            
            return condition.Substring(i);
        }
        
        public static Vector3 ParseVector3(string vectorString)
        {
            if (TryParseVector3(vectorString, out Vector3 result))
            {
                return result;
            }
            throw new FormatException("Invalid Vector3 string format: " + vectorString);
        }
        
        public static bool TryParseVector3(string vectorString, out Vector3 result)
        {
            if (vectorString.IsEmpty())
            {
                result = default;
                return false;
            }
            // 1. Remove the parentheses and spaces
            vectorString = vectorString.Replace("(", "").Replace(")", "").Replace(" ", "");

            // 2. Split the string by the comma delimiter
            string[] components = vectorString.Split(',');

            if (components.Length != 3)
            {
                result = default;
                return false;
            }

            // 3. Parse each component to a float, using the invariant culture
            // The invariant culture ensures consistent parsing regardless of the user's region 
            // (e.g., using '.' for decimal points instead of ',').
            if (float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }
            result = default;
            return false;
        }
    } 
}