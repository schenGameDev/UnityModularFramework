using UnityEngine;

namespace ModularFramework {
    using Commons;
    using Utility;

    /// <summary>
    /// The bucket keeps a &lt;string,string> dictionary, supporting value as int, string and bool.
    /// Upon fetching, the value will be cast into the desired type.
    /// </summary>
    [CreateAssetMenu(fileName = "Bucket_SO", menuName = "Bucket/Value Bucket")]
    public class Bucket : CustomTypeBucket<string> {
        public Optional<T> Get<T> (string name) {
            if(dictionary.TryGetValue(name, out string value)) {
                if(typeof(T) == typeof(string)) {
                    return new Optional<T> ((T) (object) value);
                }
                if(typeof(T) == typeof(int)) {
                    if(int.TryParse(value, out int res)) {
                        return new Optional<T> ((T) (object) res);
                    }
                    return NoMatchType<T>(name);
                }
                if(typeof(T) == typeof(float)) {
                    if(float.TryParse(value, out float res)) {
                        return new Optional<T> ((T) (object) res);
                    }
                    return NoMatchType<T>(name);
                }
                if(typeof(T) == typeof(bool)) {
                    if(bool.TryParse(value, out bool res)) {
                        return new Optional<T> ((T) (object) res);
                    }
                    return NoMatchType<T>(name);
                }
                return NoValue<T>("Unsupported type " + typeof(T));
            }
            return NoValue<T>(name + " not found");
        }
        protected Optional<T> NoValue<T>(string error) {
            DebugUtil.DebugError(error, this.name);
            return Optional<T>.None();
        }
        protected Optional<T> NoMatchType<T>(string fieldName)
            => NoValue<T>(fieldName + " is not " + typeof(T));
    }
}
