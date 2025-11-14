namespace ModularFramework.Commons {
    /// <summary>
    /// flip boolean value every time Get() is called
    /// </summary>
    public class Flip : IResetable {
        protected bool value = false;
        public static implicit operator bool(Flip flip) => flip.Get();
        public virtual bool Get() {
            value = !value;
            return !value;
        }

        public virtual void Set(bool value) {
            this.value = value;
        }

        public void Reset()
        {
            value = false;
        }
    }
}