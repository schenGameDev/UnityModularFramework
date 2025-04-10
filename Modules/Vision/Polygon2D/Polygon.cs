using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModularFramework.Utility;
using static ModularFramework.Utility.MathUtil;
using static ModularFramework.Utility.GeometryUtil;

namespace Polygon2D {
    public class Polygon : IEquatable<Polygon> {
        public List<Vector2> Vertices {get; private set;}
        public List<Edge> Edges { get => new List<Edge>(_edges);}

        private List<Edge> _edges;
        public HashSet<Edge> EdgeSet {get; private set;}

        public bool IsValid => Vertices.Count >= 3;

        public Rect BoundBox;

        private static float CLOSE_BY_DIST_SQR = 16;


        public Polygon(List<Vector2> points) {
            Vertices = points;
            CleanVertices(true);
            if(!IsValid) return;
            CalculateEdges();
            CalculateBoundBox();
        }

        public Polygon(List<Vector2> points, bool mergeClosebyPoints) {
            Vertices = points;
            CleanVertices(mergeClosebyPoints);
            if(!IsValid) return;
            CalculateEdges();
            CalculateBoundBox();
        }

        public override string ToString()
        {
            return string.Join(",", Vertices.Select(v=>v.ToString()));
        }

        public override bool Equals(object obj) => obj!=null && obj is Edge other && this.Equals(other);

        public bool Equals(Polygon obj) => EdgeSet == obj.EdgeSet;

        public override int GetHashCode() {
            return EdgeSet.GetHashCode();
        }

        public static bool operator ==(Polygon lhs, Polygon rhs)
                =>(lhs is null && rhs is null) || (lhs is not null && rhs is not null && lhs.Equals(rhs));

        public static bool operator !=(Polygon lhs, Polygon rhs) => !(lhs == rhs);


        #region Edge
        public void CalculateEdges() {
            _edges = new();
            for(int i = 1; i<Vertices.Count; i++) {
                _edges.Add(new Edge(Vertices[i-1], Vertices[i]));
            }
            _edges.Add(new Edge(Vertices[^1], Vertices[0]));
            EdgeSet = new HashSet<Edge>(_edges);
        }

        public ContainEdgeResult ContainEdge(List<Edge> edges, Edge edge) { // already know edge points on polygon
            int crossFound = 0;
            foreach(var e in edges) {
                bool isSameLine = SameLine(e.A,e.B,edge.A, edge.B);
                if(!isSameLine) {
                    continue;
                }
                bool isAOnEdge = PointOnSegmentAssumingPointOnLine(edge.A, e.A, e.B);
                bool isBOnEdge = PointOnSegmentAssumingPointOnLine(edge.B, e.A, e.B);
                if(isAOnEdge && isBOnEdge) {
                    // same or shorter
                    return ContainEdgeResult.SHARE_EDGE;
                }

                if(isAOnEdge || isBOnEdge) {
                    // longer
                    Vector2 x = isAOnEdge? edge.B : edge.A;
                    Vector2 y = (e.A - x).sqrMagnitude > (e.B - x).sqrMagnitude? e.B : e.A;
                    edge = isAOnEdge? new Edge(y,x) : new Edge(x,y);
                    if( ++ crossFound == 2) break;

                }
            }

            var mid = new Vector2((edge.A.x + edge.B.x) / 2, (edge.A.y + edge.B.y) / 2);
            var isInPolygon = PointInPolygon( mid);
            if(!isInPolygon.HasValue || isInPolygon.Value) {
                return ContainEdgeResult.INSIDE;
            }
            return ContainEdgeResult.OUTSIDE;
        }

        public enum ContainEdgeResult {SHARE_EDGE, INSIDE, OUTSIDE}

        public Edge GetFacingDisect(Vector2 target) {
            List<Vector2> sorted = Vertices.OrderBy(v=>Vector2.SignedAngle(v-target, Vector2.up)).ToList();
            return new(sorted[0],sorted[^1]);
        }
#endregion


#region Vertex

        public void CleanVertices(bool mergeClosebyPoints) {
            if(mergeClosebyPoints) MergeClosebyPoints();
            RemoveRedundantPoint(mergeClosebyPoints);

        }

        private void RemoveRedundantPoint(bool isMergeCloseByPoints) {
            if(Vertices.Count < 3) return;
            Vector2 last2= Vertices[^2];
            Vector2 last = Vertices[^1];
            int k=0;
            bool endPopped = false;
            int maxCount = 800;
            Vector2 v;

            while(k<Vertices.Count && Vertices.Count>=3 && maxCount-->0) {
                v = Vertices[k];
                if((!isMergeCloseByPoints && SamePoint(last, v)) ||
                    (endPopped && isMergeCloseByPoints && SamePoint(last, v, CLOSE_BY_DIST_SQR)) ||
                    SameLine(v, last, last, last2)) {

                    endPopped = k == 0;
                    int toRemoveIdx = endPopped? (Vertices.Count - 1) : (k-1);
                    Vertices.RemoveAt(toRemoveIdx);
                    if(endPopped) {
                        // keep poping end until fail
                        last = last2;
                        last2 = Vertices[^2];
                    } else {
                        last = v;
                    }
                } else {
                    last2 = last;
                    last = v;
                    k+=1;
                    endPopped = false;
                }
            }

            if(maxCount <= 0) {
                DebugUtil.Warn("Max count reached");
            }
        }

        private void MergeClosebyPoints() {
            Vector2 last = Round(Vertices[^1]);
            int j=0;
            Vector2 current;
            while(j<Vertices.Count) {
                current = Round(Vertices[j]);
                if(SamePoint(current, last, CLOSE_BY_DIST_SQR)) {
                    Vertices.RemoveAt(j);
                } else {
                    Vertices[j++] = current;
                    last = current;
                }
            }
        }
        public bool? PointInPolygon(Vector2 point) {
            return PointInPolygon(point, out var dontCare);
        }
        public bool? PointInPolygon(Vector2 point, out Edge pointOnEdge) {
            bool inside = false;
            Vector2 p1 = Vertices[0], p2;

            for (int i = 1; i <= Vertices.Count; i++)
            {
                p2 = Vertices[i==Vertices.Count? 0 : i];
                if(Vector2.SqrMagnitude(point - p1) < SQUARE_TOLERANCE) {
                        pointOnEdge = new(p1,p2);
                        return null; // on edge point
                }
                Rect edgeBounds = Rect.MinMaxRect(Math.Min(p1.x, p2.x), Math.Min(p1.y, p2.y), Math.Max(p1.x, p2.x), Math.Max(p1.y, p2.y));
                if(point.y > edgeBounds.yMin && point.y <= edgeBounds.yMax && point.x <= edgeBounds.xMax) {
                    if(Math.Abs(p1.x- p2.x)<TOLERANCE) { // vertical
                        if( Math.Abs(point.x - p1.x) < TOLERANCE) {
                            pointOnEdge = new(p1,p2);
                            return null; // on edge
                        }
                        inside = !inside;
                    } else {
                        double xIntersection = ((double)(point.y - p1.y)) * (p2.x - p1.x) / (p2.y - p1.y) + p1.x;
                        if(Math.Abs(point.x-xIntersection) < TOLERANCE) {
                            pointOnEdge = new(p1,p2);
                            return null; // on edge
                        }
                        if(point.x < xIntersection) inside = !inside; // to the left of the x-intersection
                    }
                }



                p1 = p2;
            }
            pointOnEdge = new();
            return inside;
        }

        public Vector2[] GetSquareBounds() {
            return new Vector2[] {new (BoundBox.xMin,BoundBox.yMin),
                                  new(BoundBox.xMin,BoundBox.yMax),
                                  new(BoundBox.xMax,BoundBox.yMax),
                                  new(BoundBox.xMax,BoundBox.yMin)};
        }

        private void CalculateBoundBox() {
            float left = float.MaxValue, right = float.MinValue,
                    up = float.MinValue, down = float.MaxValue;
            Vertices.ForEach(v => {
                if(v.x < left) left = v.x;
                if(v.x > right) right = v.x;
                if(v.y < down) down = v.y;
                if(v.y > up) up = v.y;
            });
            BoundBox = Rect.MinMaxRect(left, down, right, up);
        }

        public bool IsBoundBoxOverlap(Polygon other) {
            return BoundBox.Overlaps(other.BoundBox);
        }

        public bool IsPointInBoundBox(Vector2 point) {
            return BoundBox.Contains(point);
        }

        public OverlapResult IsOverlap(Polygon other) => IsOverlap(other, new(), new(), new(), new());

        public OverlapResult IsOverlap(Polygon other, HashSet<Vector2> insidePointSet, HashSet<Vector2> intersectionSet,
                                       Dictionary<Edge,List<Vector2>> thisEdgeIntersect, Dictionary<Edge,List<Vector2>> otherEdgeIntersect) {
            if(this == other) return OverlapResult.SAME;
            if(!IsBoundBoxOverlap(other)) return OverlapResult.SEPARATE;

            bool allVertexInOrOnOther = true, containAllOtherVertex = true,
                anyVertexInOther = false, containAnyOtherVertex = false, shareEdge = false;
            HashSet<Vector2> tempInsidePointSet = new();
            HashSet<Vector2> tempIntersectionSet = new();
            Edge? lastSharedPointOnEdge = null;

            foreach(var v in Vertices) {
                bool? isInPolygon = other.PointInPolygon(v, out var pointOnEdge);
                if(!isInPolygon.HasValue) {
                    tempIntersectionSet.Add(v);
                    if(!SamePoint(v, pointOnEdge.A) && !SamePoint(v, pointOnEdge.B)) {
                        otherEdgeIntersect.AddOrCompute(pointOnEdge, new List<Vector2>() {v}, list=>list.Add(v));
                    }
                    if(lastSharedPointOnEdge!=null && lastSharedPointOnEdge==pointOnEdge) {
                        shareEdge = true;
                    }
                    lastSharedPointOnEdge = pointOnEdge;
                } else if(isInPolygon.Value) {
                    tempInsidePointSet.Add(v);
                    anyVertexInOther = true;
                    lastSharedPointOnEdge = null;
                } else {
                    allVertexInOrOnOther = false;
                    lastSharedPointOnEdge = null;
                }
            }

            otherEdgeIntersect.Where(e => e.Value.Count > 1)
                .ForEach(e => {
                    e.Value.Sort((x,y) => Vector2.SqrMagnitude(e.Key.A - x).CompareTo(Vector2.SqrMagnitude(e.Key.A - y)));
                });


            lastSharedPointOnEdge = null;
            foreach(var v in other.Vertices) {
                bool? isInPolygon = PointInPolygon(v, out var pointOnEdge);
                if(!isInPolygon.HasValue) {
                    tempIntersectionSet.Add(v);
                    if(!SamePoint(v, pointOnEdge.A) && !SamePoint(v, pointOnEdge.B)) {
                        thisEdgeIntersect.AddOrCompute(pointOnEdge, new List<Vector2>() {v}, list=>list.Add(v));
                    }
                    if(lastSharedPointOnEdge!=null && lastSharedPointOnEdge==pointOnEdge) {
                        shareEdge = true;
                    }
                    lastSharedPointOnEdge = pointOnEdge;
                }else if(isInPolygon.Value) {
                    tempInsidePointSet.Add(v);
                    containAnyOtherVertex = true;
                    lastSharedPointOnEdge = null;
                } else {
                    containAllOtherVertex = false;
                    lastSharedPointOnEdge = null;
                }
            }

            if(allVertexInOrOnOther && !containAnyOtherVertex) return OverlapResult.INSIDE;

            thisEdgeIntersect.Where(e => e.Value.Count > 1)
                .ForEach(e => {
                    e.Value.Sort((x,y) => Vector2.SqrMagnitude(e.Key.A - x).CompareTo(Vector2.SqrMagnitude(e.Key.A - y)));
                });

            if(containAllOtherVertex && !anyVertexInOther) return shareEdge? OverlapResult.SHARE_EDGE_CONTAIN : OverlapResult.CONTAIN;

            insidePointSet.UnionWith(tempInsidePointSet);
            intersectionSet.UnionWith(tempIntersectionSet);
            if(tempInsidePointSet.NonEmpty()) return OverlapResult.INTERSECT;
            return shareEdge? OverlapResult.SHARE_EDGE_INTERSECTABLE : OverlapResult.INTERSECTABLE;
        }

        public enum OverlapResult {INTERSECTABLE, SHARE_EDGE_INTERSECTABLE, INTERSECT, CONTAIN, SHARE_EDGE_CONTAIN, INSIDE, SEPARATE, SAME}
    }
#endregion
}