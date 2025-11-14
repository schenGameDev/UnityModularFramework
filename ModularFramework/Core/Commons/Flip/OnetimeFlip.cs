namespace ModularFramework.Commons {
    /// <summary>
    /// set boolean value to target state every time Get() is called
    /// </summary>
    public class OnetimeFlip : Flip
    {
        private readonly bool _target;
        public override bool Get() {
            if(value != _target) {
                value = _target;
                return !_target;
            }
            return value;
        }

        public OnetimeFlip(bool targetState = true)
        {
            _target = targetState;
        }
    }
}