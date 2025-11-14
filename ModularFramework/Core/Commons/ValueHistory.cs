namespace ModularFramework.Commons
{
    /// <summary>
    /// store a history of previous values
    /// </summary>
    /// <typeparam name="T">value</typeparam>
    public class ValueHistory<T>
    {
        private readonly T[] _history;
        private readonly int _slots;
        private int _i;
        private bool _full;
        
        public ValueHistory(int slots)
        {
            _history = new T[slots];
            _slots = slots;
        }
        
        private void Next()
        {
            if (!_full)
            {
                _full = _i + 1==_slots;
            }
            _i = (_i + 1) % _slots;
        }

        public void Add(T value)
        {
            _history[_i] = value;
            Next();
        }

        public bool Any(T value)
        {
            int end = _full ? _slots - 1 : _i;
            for (int i = 0; i < end; i++)
            {
                if(_history[i].Equals(value)) return true;
            }

            return false;
        }
    }
}