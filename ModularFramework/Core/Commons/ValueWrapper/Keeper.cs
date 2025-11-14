
using System;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework.Commons {
    [Serializable]
    public class Keeper {
        [SerializeField] public ValueType type;

        // Storage for different types of values
        [SerializeField,ShowField(nameof(type),ValueType.Bool)] private bool boolValue;
        [SerializeField,ShowField(nameof(type),ValueType.Int)] private int intValue;
        [SerializeField,ShowField(nameof(type),ValueType.Float)] private float floatValue;
        [SerializeField,ShowField(nameof(type),ValueType.String)] private string stringValue;


        public static Keeper Of<T>(T value) {
            ValueType tp = ValueTypeOf(value.GetType());
            return tp switch {
                ValueType.Bool => new Keeper() {type=tp, boolValue=(bool)(object)value},
                ValueType.Int => new Keeper() {type=tp, intValue=(int)(object)value},
                ValueType.Float => new Keeper() {type=tp, floatValue=(float)(object)value},
                ValueType.String => new Keeper() {type=tp, stringValue=(string)(object)value},
                _ => throw new InvalidCastException()
            };
        }

        // Implicit conversion operators to convert Keeper to different types
        public static implicit operator bool(Keeper value) => value.ConvertValue<bool>();
        public static implicit operator int(Keeper value) => value.ConvertValue<int>();
        public static implicit operator float(Keeper value) => value.ConvertValue<float>();
        public static implicit operator string(Keeper value) => value.ConvertValue<string>();

        public AnyValue ConvertToAnyValue() {
            return new AnyValue() {type=this.type, boolValue = boolValue, stringValue = stringValue, floatValue = floatValue, intValue=intValue};
        }

        public T ConvertValue<T>() {
            if (typeof(T) == typeof(object)) return CastToObject<T>();
            return type switch {
                ValueType.Int => AsInt<T>(intValue),
                ValueType.Float => AsFloat<T>(floatValue),
                ValueType.Bool => AsBool<T>(boolValue),
                ValueType.String => (T) (object) stringValue,
                _ => throw new InvalidCastException($"Cannot convert Keeper of type {type} to {typeof(T).Name}")
            };
        }

        T AsBool<T>(bool value) => typeof(T) == typeof(bool) && value is T correctType ? correctType : default;
        T AsInt<T>(int value) => typeof(T) == typeof(int) && value is T correctType ? correctType : default;
        T AsFloat<T>(float value) => typeof(T) == typeof(float) && value is T correctType ? correctType : default;

        public static Type TypeOf(ValueType valueType) {
            return valueType switch {
                ValueType.Bool => typeof(bool),
                ValueType.Int => typeof(int),
                ValueType.Float => typeof(float),
                ValueType.String => typeof(string),
                ValueType.Vector3 => typeof(Vector3),
                _ => throw new NotSupportedException($"Unsupported ValueType: {valueType}")
            };
        }

        public static ValueType ValueTypeOf(Type type) {
            return type switch {
                _ when type == typeof(bool) => ValueType.Bool,
                _ when type == typeof(int) => ValueType.Int,
                _ when type == typeof(float) => ValueType.Float,
                _ when type == typeof(string) => ValueType.String,
                _ when type == typeof(Vector3) => ValueType.Vector3,
                _ => throw new NotSupportedException($"Unsupported type: {type}")
            };
        }

        T CastToObject<T>() {
            return type switch {
                ValueType.Int => (T) (object) intValue,
                ValueType.Float => (T) (object) floatValue,
                ValueType.Bool => (T) (object) boolValue,
                ValueType.String => (T) (object) stringValue,
                _ => throw new InvalidCastException($"Cannot convert Keeper of type {type} to {typeof(T).Name}")
            };
        }

        public void Set<T>(T newValue) {
            ValueType tp = ValueTypeOf(newValue.GetType());
            if(tp != ValueTypeOf(newValue.GetType())) {
                throw UnmatchTypeError(newValue.GetType());
            }
            switch(tp)  {
                case ValueType.Bool:
                    boolValue=(bool)(object)newValue;
                    break;
                case ValueType.Int:
                    intValue=(int)(object)newValue;
                    break;
                case ValueType.Float:
                    floatValue=(float)(object)newValue;
                    break;
                case ValueType.String:
                    stringValue=(string)(object)newValue;
                    break;
            }

        }

        public T Compute<T>(string operatorStr, T newValue) {
            if(operatorStr == "=") {
                return newValue;
            }

            if(ValueTypeOf(newValue.GetType()) != type) {
                throw UnmatchTypeError(newValue.GetType());
            }
            switch(type)  {
                case ValueType.Bool:
                    bool b=(bool)(object)newValue;
                    switch(operatorStr) {
                        case "||":
                            return (T)(object)(boolValue || b);
                        case "&&":
                            return (T)(object)(boolValue && b);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.Int:
                    int i=(int)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(intValue+i);
                        case "-":
                            return (T)(object)(intValue-i);
                        case "*":
                            return (T)(object)(intValue*i);
                        case "/":
                            return (T)(object)(intValue/i);
                        case "%":
                            return (T)(object)(intValue%i);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.Float:
                    float f=(float)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(floatValue+f);
                        case "-":
                            return (T)(object)(floatValue-f);
                        case "*":
                            return (T)(object)(floatValue*f);
                        case "/":
                            return (T)(object)(floatValue/f);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.String:
                    string s=(string)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(stringValue+s);
                        case "-":
                            return (T)(object)stringValue.Replace(s, "");
                        default:
                            throw NotSupportError(operatorStr);
                    }
            }
            throw NotSupportError(operatorStr);
        }

        public bool Is<T>(string logicOperator, T value) {
            // >/</==/>=/<=
            if(ValueTypeOf(value.GetType()) != type) {
                throw UnmatchTypeError(value.GetType());
            }

            switch(type)  {
                case ValueType.Bool:
                    bool b=(bool)(object)value;
                    switch(logicOperator) {
                        case "==":
                            return boolValue == b;
                        case "!=":
                            return boolValue != b;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.Int:
                    int i=(int)(object)value;
                    switch(logicOperator) {
                        case ">":
                            return intValue>i;
                        case "<":
                            return intValue<i;
                        case "==":
                            return intValue==i;
                        case "!=":
                            return intValue!=i;
                        case ">=":
                            return intValue>=i;
                        case "<=":
                            return intValue<=i;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.Float:
                    float f=(float)(object)value;
                    switch(logicOperator) {
                        case ">":
                            return floatValue>f;
                        case "<":
                            return floatValue<f;
                        case "==":
                            return floatValue==f;
                        case "!=":
                            return floatValue!=f;
                        case ">=":
                            return floatValue>=f;
                        case "<=":
                            return floatValue<=f;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.String:
                    string s=(string)(object)value;
                    switch(logicOperator) {
                        case "==":
                            return stringValue==s;
                        case "!=":
                            return stringValue!=s;
                        default:
                            throw NotSupportError(logicOperator);
                    }
            }

            return false;

        }

        public override string ToString()
        {
            return type switch {
                ValueType.Int =>  intValue.ToString(),
                ValueType.Float => floatValue.ToString(),
                ValueType.Bool => boolValue.ToString(),
                ValueType.String => stringValue,
                _ => throw new InvalidCastException($"Cannot convert Keeper of type {type} to string")
            };
        }

        InvalidCastException NotSupportError(string unsupported) =>new InvalidCastException(type.ToString() + "don't support " + unsupported);
        InvalidCastException UnmatchTypeError(Type otherType) => new InvalidCastException(type.ToString() + " Unmatched variable type " + otherType.Name);
    }
}
