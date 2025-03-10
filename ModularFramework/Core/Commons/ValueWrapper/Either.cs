using System;

namespace ModularFramework.Commons {
    public struct Either<TLeft, TRight> {
        readonly TLeft left;
        readonly TRight right;
        readonly bool isRight;

        Either(TLeft left, TRight right, bool isRight) {
            this.left = left;
            this.right = right;
            this.isRight = isRight;
        }

        public bool IsLeft => !isRight;
        public bool IsRight => isRight;

        public TLeft Left {
            get {
                if (IsRight) throw new InvalidOperationException("No Left value present.");
                return left;
            }
        }

        public TRight Right {
            get {
                if (IsLeft) throw new InvalidOperationException("No Right value present.");
                return right;
            }
        }

        public static Either<TLeft, TRight> FromLeft(TLeft left) => new (left, default, false);
        public static Either<TLeft, TRight> FromRight(TRight right) => new (default, right, true);

        public TResult Match<TResult>(Func<TLeft, TResult> leftFunc, Func<TRight, TResult> rightFunc) {
            return IsRight ? rightFunc(right) : leftFunc(left);
        }

        public Either<TLeft, TResult> Select<TResult>(Func<TRight, TResult> map) {
            return IsRight ? Either<TLeft, TResult>.FromRight(map(right)) : Either<TLeft, TResult>.FromLeft(left);
        }

        public Either<TLeft, TResult> SelectMany<TResult>(Func<TRight, Either<TLeft, TResult>> bind) {
            return IsRight ? bind(right) : Either<TLeft, TResult>.FromLeft(left);
        }

        public static implicit operator Either<TLeft, TRight>(TLeft left) => FromLeft(left);
        public static implicit operator Either<TLeft, TRight>(TRight right) => FromRight(right);

        public override string ToString() => IsRight ? $"Right({right})" : $"Left({left})";
    }
}