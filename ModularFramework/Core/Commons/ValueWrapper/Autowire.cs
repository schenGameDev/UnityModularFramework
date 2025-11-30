namespace ModularFramework
{
    /// <summary>
    /// automatically retrieve module/system. It must be initialized with new().
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Autowire<T> : IResetable where T : GameSystem
    {
        private T _instance;
        public T Get()
        {
            if (!_instance)
            {
                _instance = GameRunner.Instance?.GetModuleOrSystem<T>().OrElse(null);
            }

            return _instance;
        }

        public static implicit operator T(Autowire<T> instance) => instance.Get();

        public void ResetState() => _instance = null;
    }
}