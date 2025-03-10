public class Flip : IResetable {
    protected bool value = false;
    public static implicit operator bool(Flip flip) => flip.Get();
    public virtual bool Get() {
        value = !value;
        return !value;
    }

    public void Set(bool value) {
        this.value = value;
    }

    public void Reset()
    {
        value = false;
    }
}