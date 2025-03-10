using System;
using UnityEngine;

namespace ModularFramework.Utility {
    using static MathUtil;
    public static class GeometryUtil {
        public static bool PointOnSegmentAssumingPointOnLine(Vector2 point, Vector2 linePointA, Vector2 linePointB) { // already know point in on line
            bool perpendicular = Math.Abs(linePointA.x - linePointB.x) < TOLERANCE;
            return (perpendicular && WithinRange(point.y, linePointA.y, linePointB.y)) || (!perpendicular && WithinRange(point.x, linePointA.x, linePointB.x));
        }

        public static bool PointOnLineSegment(Vector2 point, Vector2 linePA, Vector2 linePB) {
            double x = point.x, y = point.y;
            if (y - Math.Min(linePA.y, linePB.y) > -TOLERANCE &&
                Math.Max(linePA.y, linePB.y) - y > -TOLERANCE &&
                x - Math.Min(linePA.x, linePB.x) > -TOLERANCE &&
                Math.Max(linePA.x, linePB.x) - x > -TOLERANCE)
            {
                bool vertical = Math.Abs(linePA.x- linePB.x)<TOLERANCE;
                if(vertical) {
                    return true;
                } else {
                    double xIntersection = (y - linePA.y) * (linePB.x - linePA.x) / (linePB.y - linePA.y) + linePA.x;
                    return Math.Abs(x-xIntersection) < TOLERANCE;
                }
            }
            return false;
        }

        public static bool SameDirectionButLonger(Vector2Int v, Vector2Int compare) {
            return SameDirection(v, compare, 3) && v.sqrMagnitude >= compare.sqrMagnitude;
        }

        public static bool SameLine(Vector2 lineAp1, Vector2 lineAp2, Vector2 lineBp1, Vector2 lineBp2) {
            double deltaXA = lineAp1.x- lineAp2.x;
            double deltaXB = lineBp1.x- lineBp2.x;
            bool vertical = Math.Abs(deltaXA)<TOLERANCE;
            if(vertical) return Math.Abs(deltaXB)<TOLERANCE && Math.Abs(lineAp1.x -lineBp1.x)<TOLERANCE;
            double k1 = (lineAp1.y - lineAp2.y) / deltaXA;
            double b1 = lineAp1.y - k1 * lineAp1.x;

            return Math.Abs(k1 * lineBp1.x + b1 - lineBp1.y) < TOLERANCE && Math.Abs(k1 * lineBp2.x + b1 - lineBp2.y) < TOLERANCE;
        }

        public static bool SameDirection(Vector2 v1, Vector2 v2, float tolerance) {
            bool vertical = Math.Abs(v1.x)<tolerance;
            if(vertical) return Math.Abs(v2.x)<tolerance;
            return Math.Abs(((double)v1.y) / v1.x * v2.x - v2.y) < tolerance;
            //return Vector2.Angle(v1, v2) < tolerance;
        }
        public static bool SameDirection(Vector2 v1, Vector2 v2) => SameDirection(v1, v2, TOLERANCE);
        public static bool SameDirection(Vector3 v1, Vector3 v2) => Vector3.Angle(v1, v2) < TOLERANCE;

        public static bool OppositeDirection(Vector2 v1, Vector2 v2, float tolerance) => 180 - Vector2.Angle(v1, v2) < tolerance;

        public static bool SamePoint(Vector3 p1, Vector3 p2) => Vector3.SqrMagnitude(p1-p2) < SQUARE_TOLERANCE;
        public static bool SamePoint(Vector2 p1, Vector2 p2) => SamePoint(p1, p2, SQUARE_TOLERANCE);
        public static bool SamePoint(Vector2 p1, Vector2 p2, float sqrTolerance) => Vector2.SqrMagnitude(p1-p2) <= sqrTolerance;

        public static bool LineSegmentIntersection( Vector2 lineAp1,Vector2 lineAp2, Vector2 lineBp1, Vector2 lineBp2, ref Vector2 intersection )
        {
            if(lineAp1==lineBp1 || lineAp1==lineBp2) {
                intersection.x = lineAp1.x;
                intersection.y = lineAp1.y;
                return true;
            }

            if(lineAp2==lineBp1 || lineAp2==lineBp2) {
                intersection.x = lineAp2.x;
                intersection.y = lineAp2.y;
                return true;
            }

            double Ax,Bx,Cx,Ay,By,Cy,d,e,f,num/*,offset*/;
            double x1lo,x1hi,y1lo,y1hi;

            Ax = lineAp2.x-lineAp1.x;
            Bx = lineBp1.x-lineBp2.x;
            // X bound box test/
            if(Ax<0) {
                x1lo=lineAp2.x; x1hi=lineAp1.x;
            } else {
                x1hi=lineAp2.x; x1lo=lineAp1.x;
            }

            if(Bx>0) {
                if(x1hi < lineBp2.x || lineBp1.x < x1lo) return false;
            } else {
                if(x1hi < lineBp1.x || lineBp2.x < x1lo) return false;
            }
            Ay = lineAp2.y-lineAp1.y;
            By = lineBp1.y-lineBp2.y;

            // Y bound box test//

            if(Ay<0) {
                y1lo=lineAp2.y; y1hi=lineAp1.y;
            } else {
                y1hi=lineAp2.y; y1lo=lineAp1.y;
            }

            if(By>0) {
                if(y1hi < lineBp2.y || lineBp1.y < y1lo) return false;
            } else {
                if(y1hi < lineBp1.y || lineBp2.y < y1lo) return false;
            }

            Cx = lineAp1.x-lineBp1.x;
            Cy = lineAp1.y-lineBp1.y;

            d = By*Cx - Bx*Cy;  // alpha numerator//
            f = Ay*Bx - Ax*By;  // both denominator//

            // check if they are parallel
            if(Math.Abs(f)<0.000001f) return false;

            // alpha tests//

            if(f>0 && (d<0 || d>f)) return false;
            if(f<=0 && (d>0 || d<f)) return false;


            e = Ax*Cy - Ay*Cx;  // beta numerator//

            // beta tests //
            if(f>0 && (e<0 || e>f)) return false;
            if(f<0 && (e>0 || e<f)) return false;

            // compute intersection coordinates //

            num = d*Ax; // numerator //

        //    offset = same_sign(num,f) ? f*0.5f : -f*0.5f;   // round direction //

        //    intersection.x = p1.x + (num+offset) / f;
            intersection.x =(float) (lineAp1.x + num / f);

                num = d*Ay;
            //    offset = same_sign(num,f) ? f*0.5f : -f*0.5f;
            //    intersection.y = p1.y + (num+offset) / f;
            intersection.y = (float) (lineAp1.y + num / f);

            return true;

        }

        public static Vector2Int Round(Vector2 v) => new (MathUtil.Round(v.x), MathUtil.Round(v.y));
        public static Vector2 Round(Vector2 v, uint precision) => new (RoundTo(v.x, precision), RoundTo(v.y,2));

        public static float TriangleArea(Vector2 p1, Vector2 p2, Vector2 p3) {
            return Math.Abs(p1[0]*(p2[1]-p3[1])+p2[0]*(p3[1]-p1[1])+p3[0]*(p1[1]-p2[1]))/2;
        }

        public static Vector2 Rotate(Vector2 v, float delta) {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }
    }
}