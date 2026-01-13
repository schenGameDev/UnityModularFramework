using ModularFramework.Utility;

namespace ModularFramework
{
    /// <summary>
    /// A lazy-loading wrapper that automatically retrieves and caches singleton instances of modules or systems.<br/>
    /// Must be initialized with new() and used with [RuntimeObject] attribute in GameModule classes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Autowire<T> : IResetable where T : class
    {
        private T _instance;
        public T Get()
        {
            if (_instance != null)
            {
                _instance = SingletonRegistry<T>.Instance;
            }

            return _instance;
        }

        public static implicit operator T(Autowire<T> instance) => instance.Get();

        public void ResetState() => _instance = null;
    }
}