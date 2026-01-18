using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Utility
{
    /// <summary>
    /// Evaluates boolean expressions given as strings.
    /// </summary>
    public static class BooleanExpressionEvaluator
    {
        public enum ValueType { Int, Float, Bool, String, Vector3 }
        private static readonly HashSet<char> OPERATORS = new () { '>', '<', '=', '!' };

        public static bool Evaluate(string strValue, ValueType valueType, string expression)
        {

            return valueType switch
            {
                ValueType.Int => Evaluate(int.Parse(strValue), expression),
                ValueType.Bool => Evaluate(bool.Parse(strValue), expression),
                ValueType.Float => Evaluate(float.Parse(strValue), expression),
                ValueType.String => Evaluate(strValue, expression),
                ValueType.Vector3 => Evaluate(ParseVector3(strValue), expression),
                _ => false,
            };
        }
        
        public static bool Evaluate(int currentValue, string expression)
        {
            var conditionStr = TrimOperator(expression, out string op);
            if (int.TryParse(conditionStr, out int conditionValue))
            {
                return EvaluateIntCondition(currentValue, op, conditionValue);
            }
            Debug.LogError($"Cannot parse int condition {conditionStr}");
            return false;
        }
        
        public static bool Evaluate(bool currentValue, string expression)
        {
            var conditionStr = TrimOperator(expression, out string op);
            if (bool.TryParse(conditionStr, out bool conditionValue))
            {
                return EvaluateBoolCondition(currentValue, op, conditionValue);
            }
            Debug.LogError($"Cannot parse bool condition {conditionStr}");
            return false;
        }
        
        public static bool Evaluate(float currentValue, string expression)
        {
            var conditionStr = TrimOperator(expression, out string op);
            if (float.TryParse(conditionStr, out float conditionValue))
            {
                return EvaluateFloatCondition(currentValue, op, conditionValue);
            }
            Debug.LogError($"Cannot parse float condition {conditionStr}");
            return false;
        }

        public static bool Evaluate(string currentValue, string expression)
        {
            var condition = TrimOperator(expression, out string op);
            return EvaluateStringCondition(currentValue, op, condition);
        }
        
        public static bool Evaluate(Vector3 currentValue, string expression)
        {
            var conditionStr = TrimOperator(expression, out string op);
            var conditionValue = ParseVector3(conditionStr);
            return EvaluateVector3Condition(currentValue, op, conditionValue);
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
                "!=" => currentValue != condition,
                "" or "=" or "==" => currentValue == condition,
                _ => IncompatibleOperator(operatorStr, ValueType.Float)
            };
        }
        
        private static bool IncompatibleOperator(string operatorStr, ValueType valueType)
        {
            Debug.LogError($"{operatorStr} is incompatible with the value type {valueType.ToString()}");
            return false;
        }
        
        public static string TrimOperator(string condition, out string opr)
        {
            condition = condition.Trim();
            opr = "";
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
            // 1. Remove the parentheses and spaces
            vectorString = vectorString.Replace("(", "").Replace(")", "").Replace(" ", "");

            // 2. Split the string by the comma delimiter
            string[] components = vectorString.Split(',');

            if (components.Length != 3)
            {
                Debug.LogError("Invalid Vector3 string format: " + vectorString);
                return Vector3.zero; // Or throw an exception
            }

            // 3. Parse each component to a float, using the invariant culture
            // The invariant culture ensures consistent parsing regardless of the user's region 
            // (e.g., using '.' for decimal points instead of ',').
            if (float.TryParse(components[0], out float x) &&
                float.TryParse(components[1], out float y) &&
                float.TryParse(components[2], out float z))
            {
                return new Vector3(x, y, z);
            }
            Debug.LogError("Failed to parse components as floats: " + vectorString);
            return Vector3.zero; // Or throw an exception
        }
    }
}