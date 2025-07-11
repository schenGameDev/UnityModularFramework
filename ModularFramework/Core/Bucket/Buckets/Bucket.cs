using UnityEngine;

namespace ModularFramework {
    using System.Collections.Generic;
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
                if(value is T) {
                    return new Optional<T> ((T) (object) value);
                }
                if(int.TryParse(value, out int res)) {
                    return new Optional<T> ((T) (object) res);
                } 
                if(float.TryParse(value, out float res1)) {
                    return new Optional<T> ((T) (object) res1);
                } 
                if(bool.TryParse(value, out bool res2)) {
                    return new Optional<T> ((T) (object) res2);
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

        public Dictionary<string,string> GetDictionary() {
            return dictionary;
        }
    }
}
