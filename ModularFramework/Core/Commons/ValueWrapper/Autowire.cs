namespace ModularFramework
{
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

        public void Reset() => _instance = null;
    }
}