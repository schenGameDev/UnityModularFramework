
using System;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework.Commons {
    [Serializable]
    public class Keeper {
        [SerializeField] public ValueType type;

        // Storage for different types of values
        [SerializeField,ShowField(nameof(type),ValueType.Bool)] private bool _boolValue;
        [SerializeField,ShowField(nameof(type),ValueType.Int)] private int _intValue;
        [SerializeField,ShowField(nameof(type),ValueType.Float)] private float _floatValue;
        [SerializeField,ShowField(nameof(type),ValueType.String)] private string _stringValue;


        public static Keeper Of<T>(T value) {
            ValueType tp = ValueTypeOf(typeof(T));
            return tp switch {
                ValueType.Bool => new Keeper() {type=tp, _boolValue=(bool)(object)value},
                ValueType.Int => new Keeper() {type=tp, _intValue=(int)(object)value},
                ValueType.Float => new Keeper() {type=tp, _floatValue=(float)(object)value},
                ValueType.String => new Keeper() {type=tp, _stringValue=(string)(object)value},
                _ => throw new InvalidCastException()
            };
        }

        // Implicit conversion operators to convert Keeper to different types
        public static implicit operator bool(Keeper value) => value.ConvertValue<bool>();
        public static implicit operator int(Keeper value) => value.ConvertValue<int>();
        public static implicit operator float(Keeper value) => value.ConvertValue<float>();
        public static implicit operator string(Keeper value) => value.ConvertValue<string>();

        public T ConvertValue<T>() {
            if (typeof(T) == typeof(object)) return CastToObject<T>();
            return type switch {
                ValueType.Int => AsInt<T>(_intValue),
                ValueType.Float => AsFloat<T>(_floatValue),
                ValueType.Bool => AsBool<T>(_boolValue),
                ValueType.String => (T) (object) _stringValue,
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
                ValueType.Int => (T) (object) _intValue,
                ValueType.Float => (T) (object) _floatValue,
                ValueType.Bool => (T) (object) _boolValue,
                ValueType.String => (T) (object) _stringValue,
                _ => throw new InvalidCastException($"Cannot convert Keeper of type {type} to {typeof(T).Name}")
            };
        }

        public void Set<T>(T newValue) {
            ValueType tp = ValueTypeOf(typeof(T));
            if(tp != ValueTypeOf(typeof(T))) {
                throw UnmatchTypeError(typeof(T));
            }
            switch(tp)  {
                case ValueType.Bool:
                    _boolValue=(bool)(object)newValue;
                    break;
                case ValueType.Int:
                    _intValue=(int)(object)newValue;
                    break;
                case ValueType.Float:
                    _floatValue=(float)(object)newValue;
                    break;
                case ValueType.String:
                    _stringValue=(string)(object)newValue;
                    break;
            }

        }

        public T Compute<T>(string operatorStr, T newValue) {
            if(operatorStr == "=") {
                return newValue;
            }

            if(ValueTypeOf(typeof(T)) != type) {
                throw UnmatchTypeError(typeof(T));
            }
            switch(type)  {
                case ValueType.Bool:
                    bool b=(bool)(object)newValue;
                    switch(operatorStr) {
                        case "||":
                            return (T)(object)(_boolValue || b);
                        case "&&":
                            return (T)(object)(_boolValue && b);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.Int:
                    int i=(int)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(_intValue+i);
                        case "-":
                            return (T)(object)(_intValue-i);
                        case "*":
                            return (T)(object)(_intValue*i);
                        case "/":
                            return (T)(object)(_intValue/i);
                        case "%":
                            return (T)(object)(_intValue%i);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.Float:
                    float f=(float)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(_floatValue+f);
                        case "-":
                            return (T)(object)(_floatValue-f);
                        case "*":
                            return (T)(object)(_floatValue*f);
                        case "/":
                            return (T)(object)(_floatValue/f);
                        default:
                            throw NotSupportError(operatorStr);
                    }
                case ValueType.String:
                    string s=(string)(object)newValue;
                    switch(operatorStr) {
                        case "+":
                            return (T)(object)(_stringValue+s);
                        case "-":
                            return (T)(object)_stringValue.Replace(s, "");
                        default:
                            throw NotSupportError(operatorStr);
                    }
            }
            throw NotSupportError(operatorStr);
        }

        public bool Is<T>(string logicOperator, T value) {
            // >/</==/>=/<=
            if(ValueTypeOf(typeof(T)) != type) {
                throw UnmatchTypeError(typeof(T));
            }

            switch(type)  {
                case ValueType.Bool:
                    bool b=(bool)(object)value;
                    switch(logicOperator) {
                        case "==":
                            return _boolValue == b;
                        case "!=":
                            return _boolValue != b;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.Int:
                    int i=(int)(object)value;
                    switch(logicOperator) {
                        case ">":
                            return _intValue>i;
                        case "<":
                            return _intValue<i;
                        case "==":
                            return _intValue==i;
                        case "!=":
                            return _intValue!=i;
                        case ">=":
                            return _intValue>=i;
                        case "<=":
                            return _intValue<=i;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.Float:
                    float f=(float)(object)value;
                    switch(logicOperator) {
                        case ">":
                            return _floatValue>f;
                        case "<":
                            return _floatValue<f;
                        case "==":
                            return _floatValue==f;
                        case "!=":
                            return _floatValue!=f;
                        case ">=":
                            return _floatValue>=f;
                        case "<=":
                            return _floatValue<=f;
                        default:
                            throw NotSupportError(logicOperator);
                    }

                case ValueType.String:
                    string s=(string)(object)value;
                    switch(logicOperator) {
                        case "==":
                            return _stringValue==s;
                        case "!=":
                            return _stringValue!=s;
                        default:
                            throw NotSupportError(logicOperator);
                    }
            }

            return false;

        }

        public override string ToString()
        {
            return type switch {
                ValueType.Int =>  _intValue.ToString(),
                ValueType.Float => _floatValue.ToString(),
                ValueType.Bool => _boolValue.ToString(),
                ValueType.String => _stringValue.ToString(),
                _ => throw new InvalidCastException($"Cannot convert Keeper of type {type} to string")
            };
        }

        InvalidCastException NotSupportError(string unsupported) =>new InvalidCastException(type.ToString() + "don't support " + unsupported);
        InvalidCastException UnmatchTypeError(Type otherType) => new InvalidCastException(type.ToString() + " Unmatched variable type " + otherType.Name);
    }
}
