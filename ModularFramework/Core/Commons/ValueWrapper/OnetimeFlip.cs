namespace ModularFramework.Commons {
    public class OnetimeFlip : Flip {
        public override bool Get() {
            if(!value) {
                value = true;
                return false;
            }
            return value;
        }
    }
}