namespace ModularFramework.Commons {
    public class OnetimeFlip : Flip {
        public override bool Get() {
            if(value) {
                value = false;
                return true;
            }
            return value;
        }
    }
}