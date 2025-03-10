using System;
using UnityEngine;

namespace ModularFramework.Utility {
    public static class MathUtil {
        public static readonly float TOLERANCE = 0.001f;
        public static readonly float SQUARE_TOLERANCE = 0.000001f;


        public static bool IsOdd(int num) => num % 2 != 0;

        public static int IntPow(int x, uint pow)
        {
            int ret = 1;
            while ( pow != 0 )
            {
                if ( (pow & 1) == 1 )
                    ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }
        public static float RoundTo(float value, uint decimalPlaces) {
            if(decimalPlaces == 0) return Mathf.RoundToInt(value);

            var tens = IntPow(10, decimalPlaces);
            return Mathf.RoundToInt(value * tens) / (float)tens;
        }

        public static float RoundTo(double value, uint decimalPlaces) {
            return RoundTo((float)value, decimalPlaces);
        }

        public static int Round(float value) => (int) RoundTo(value,0);

        private static int RandomIndexExcept(int maxExclusive, int except) {
            int res = except;
            if(maxExclusive !=1) {
                while(res==except) {
                res = UnityEngine.Random.Range(0,maxExclusive);
                }
            }
            return res;
        }

        /// <summary>
        /// get a signed difference between 2 angle
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>angle difference, positive sign indicates clockwise</returns>
        public static float AngleDifference(float a, float b) {
            float dif = Math.Abs(a - b) % 360;
            if (dif > 180) dif = 360 - dif;
            return (NormalizeAngle(a + dif)- NormalizeAngle(b) < TOLERANCE)? dif : -dif;
        }

        /// <summary>
        /// get a signed difference between 2 angle
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>angle difference, positive sign indicates clockwise</returns>
        public static int AngleDifference(int a, int b) {
            int dif = Math.Abs(a - b) % 360;
            if (dif > 180) dif = 360 - dif;
            return (NormalizeAngle(a + dif) == NormalizeAngle(b))? dif : -dif;
        }

        public static bool WithinRange(float value, float a, float b) => (a<=b && value>=a && value<=b) || (a>b && value>=b && value<=a);

        public static bool WithinAngleRange(float value, float min, float max) {
            if(min < 0 && max > 0) {
                return value>=min && value<=max;
            }

            value = NormalizeAngle(value);
            var normMin = NormalizeAngle(min);
            var normMax = NormalizeAngle(max);

            return value>=normMin && value<=normMax;
        }

        public static bool BetweenClockwiseAngles(float angle, float a, float b) {
            angle = NormalizeAngle(angle);
            a = NormalizeAngle(a);
            b = NormalizeAngle(b);

            if(a <= b) return angle>=a && angle<=b;
            return angle >= a || angle <= b;
        }

        public static float NormalizeAngle(float value) {
            value %= 360;
            if(value < 0) value += 360;
            return value;
        }
        public static int NormalizeAngle(int value) {
            value %= 360;
            if(value < 0) value += 360;
            return value;
        }

        public static void Repeat(int count, Action action) {
            for (int i = 0;i<count;i++) action();
        }
    }
}