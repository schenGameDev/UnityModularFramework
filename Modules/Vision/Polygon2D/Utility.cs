using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModularFramework.Utility.GeometryUtil;
using System;
using ModularFramework.Utility;

namespace Polygon2D {
    public static class Utility {
        public static bool _isTest = true;
        private static bool _isError = false;

        public static List<Vector2> GetVerticesFromEdge(List<Edge> edges, bool isReverse, bool allowDupEnd) {
            List<Vector2> result = new();
            foreach(var e in edges) {
                result.Add(e.A);
            }
            if(allowDupEnd || edges[^1].B != result[0]) result.Add(edges[^1].B);
            if(isReverse) result.Reverse();
            return result;
        }

        public static HashSet<Vector2> GetVerticesFromBrokenEdge(List<Edge> edges) {
            HashSet<Vector2> result = new();
            foreach(var e in edges) {
                result.Add(e.A);
                result.Add(e.B);
            }
            return result;
        }


        private static List<List<Edge>> Divide(List<Edge> edges, HashSet<Vector2> intersectionPoints) {
            List<List<Edge>> result = new();
            List<Edge> temp = new();
            foreach(Edge v in edges) {
                if(temp.IsEmpty() ||
                (SamePoint(temp[^1].B, v.A) && !intersectionPoints.Contains(v.A))) {
                    temp.Add(v);
                }else {
                    result.Add(temp);
                    temp = new() {v};
                }
            }

            if(temp.NonEmpty()) result.Add(temp);

            if(result.Count >=2 &&
            SamePoint(edges[0].A,edges[^1].B) &&
            !intersectionPoints.Contains(edges[^1].B)) {
                result[0].InsertRange(0,result[^1]);
                result.RemoveAt(result.Count - 1);
                // var t = result[^1];
                // t.AddRange(result.RemoveAtAndReturn(0));
            }
            return result;
        }

        private static bool IsSegmentLoop(List<List<Edge>> segment) {
            return segment.Count == 1 && segment[0].Count > 1 && segment[0][0] == segment[0][^1];
        }

        private static void PurgeIntersection(List<List<Edge>> edges, List<Edge> edge, HashSet<Vector2> intersections) {
            if(edges.IsEmpty() || edge.IsEmpty()) return;
            HashSet<Vector2> e1 = new();
            edges.Select(e => GetVerticesFromBrokenEdge(e)).ForEach(l => e1.AddRange(l));
            HashSet<Vector2> e2 = GetVerticesFromBrokenEdge(edge);
            intersections.RemoveWhere(i => !e1.Contains(i) || !e2.Contains(i));
        }

        private static void PurgeIntersection(List<Edge> edgeA, List<Edge> edgeB, HashSet<Vector2> intersections) {
            if(edgeA.IsEmpty() || edgeB.IsEmpty()) return;
            HashSet<Vector2> e1 = GetVerticesFromBrokenEdge(edgeA);
            HashSet<Vector2> e2 = GetVerticesFromBrokenEdge(edgeB);
            intersections.RemoveWhere(i => !e1.Contains(i) || !e2.Contains(i));
        }

        private static void AddIntersections(List<Edge> edgesA, List<Edge> edgesB,
                                             HashSet<Vector2> insidePointSet, HashSet<Vector2> intersectionSet) {
            Vector2 intersection = new();
            int i=0,j=0;
            while(i<edgesA.Count && j<edgesB.Count) {
                var edgeA = edgesA[i];
                var edgeB = edgesB[j];

                if(LineSegmentIntersection(edgeA.A, edgeA.B, edgeB.A, edgeB.B, ref intersection)) {
                    if(SamePoint(edgeA.A, intersection)) {
                        insidePointSet.Remove(edgeA.A);
                    } else if (SamePoint(edgeA.B, intersection)) {
                        insidePointSet.Remove(edgeA.B);
                    } else {
                        edgesA[i] = new (edgeA.A, intersection);
                        edgesA.Insert(i+1, new (intersection, edgeA.B));
                    }
                    if(SamePoint(edgeB.A, intersection)) {
                        insidePointSet.Remove(edgeB.A);
                    } else if(SamePoint(edgeB.B, intersection)) {
                        insidePointSet.Remove(edgeB.B);
                    } else {
                        edgesB[j] = new (edgeB.A, intersection);
                        edgesB.Insert(j+1, new (intersection, edgeB.B));
                        j+=1;
                    }
                    intersectionSet.Add(intersection);
                }

                if(++j >= edgesB.Count) {
                    j=0;
                    i+=1;
                }
            }
        }

        private static void AddEdge(List<Edge> edges, Dictionary<Edge, List<Vector2>> intersections) {
            intersections.RemoveWhere((k,v) => {
                        int i = edges.IndexOf(k);
                        if(i != -1) {
                            var p1 = edges[i].A;
                            var p2 = edges[i].B;
                            edges[i] = new(p1, v[0]);
                            edges.Insert(i + 1, new(v[^1], p2));
                            for(int offset = 0; offset < v.Count -1; offset ++) {
                                edges.Insert(++i, new(v[offset], v[offset + 1]));
                            }
                            return true;
                        }
                        return false;
                    });
        }

#region Connect
        private static List<List<Vector2>> Connect(List<List<Edge>> edges, List<Edge> bridge, HashSet<Vector2> intersections) {
            PurgeIntersection(edges, bridge, intersections);

            List<List<Vector2>> polygonVertices = new();
            List<List<Edge>> bridgeSegments = Divide(bridge, intersections);
            List<List<List<Edge>>> segments = edges.Select(e => Divide(e, intersections))
                                            .Where(seg => {
                                                if(IsSegmentLoop(seg)) {
                                                    if(seg.Count>2) {
                                                        polygonVertices.Add(GetVerticesFromEdge(seg[0], false, false));
                                                    }
                                                    return false;
                                                }
                                                return seg.NonEmpty();
                                            }).ToList();
            if(IsSegmentLoop(bridgeSegments)) {
                if(bridgeSegments.Count>2) polygonVertices.Add(GetVerticesFromEdge(bridgeSegments[0], false, false));
                return polygonVertices;
            }
            if(segments.IsEmpty()) {
                return polygonVertices;
            }
            edges.Clear();
            bridge.Clear();
            intersections.Clear();

            int curIdx = 0; // -1 random, -2 bridge
            bool isReverse = false;
            List<Edge> seg = segments[curIdx].RemoveAtAndReturn(0);
            if(segments[curIdx].IsEmpty()) {
                segments.RemoveAt(curIdx);
                curIdx = -1;
            }
            List<Vector2> points = new();
            int maxCount = 100;
            do {

                List<Vector2> vertices = GetVerticesFromEdge(seg, isReverse, true);
                bool ignoreFirst = points.Count > 0 && SamePoint(points[^1], vertices[0]);
                points.AddRange(ignoreFirst? vertices.GetRange(1,vertices.Count - 1) : vertices);

                var lastPointInSegment = vertices[^1];

                if(SamePoint(points[0], lastPointInSegment) ||
                   (segments.Count + bridgeSegments.Count == 0 && SamePoint(points[0], lastPointInSegment,10))) {
                    // loop
                    if(points.Count > segments.Select(s=>s.Select(edges=>edges.Count + 1).Sum()).Sum()) {
                        polygonVertices.Add(points.GetRange(0,points.Count-1));
                        break;
                    } else if(segments.IsEmpty()) {
                        break;
                    } else {
                        // hole
                        DebugUtil.Log("Hole");
                        points.Clear();
                        curIdx = 0;
                        isReverse = false;
                        seg = segments[curIdx].RemoveAtAndReturn(0);
                        if(segments[curIdx].IsEmpty()) {
                            segments.RemoveAt(curIdx);
                            curIdx = -1;
                        }
                        continue;
                    }
                }

                bool found = false;
                if(curIdx != -2) {
                    // find in Bridge
                    foreach(var s in bridgeSegments) {
                        if(SamePoint(s[0].A, lastPointInSegment)) {
                            seg = s;
                            isReverse = false;
                            bridgeSegments.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                            seg = s;
                            isReverse = true;
                            bridgeSegments.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        }
                    }
                }

                if(!found) {
                    for(int i=0; i<segments.Count; i++) {
                        if(i == curIdx) continue;
                        List<List<Edge>> nextSegList = segments[i];
                        foreach(var s in nextSegList) {
                            if(SamePoint(s[0].A, lastPointInSegment)) {
                                seg = s;
                                isReverse = false;
                                nextSegList.Remove(s);
                                found = true;
                                break;
                            } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                                seg = s;
                                isReverse = true;
                                nextSegList.Remove(s);
                                found = true;
                                break;
                            }
                        }
                        if(found) {
                            if(nextSegList.IsEmpty()) {
                                curIdx = -1;
                                segments.RemoveAt(i);
                            } else {
                                curIdx = i;
                            }
                            break;
                        }
                    }
                }


                if(!found && curIdx!=-1 && !(curIdx == -2 && bridgeSegments.IsEmpty())) {
                    // caused by tiny gap on line due to rounding
                    // Debug.LogWarning("Gap found in line");
                    List<List<Edge>> nextSegList =curIdx == -2? bridgeSegments : segments[curIdx];
                    float minDist = float.PositiveInfinity;
                    float dist;
                    foreach(var s in nextSegList) {
                        dist = Vector2.SqrMagnitude(s[0].A- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = false;
                        }
                        dist = Vector2.SqrMagnitude(s[^1].B- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = true;
                        }
                    }
                    if(minDist<=10) {
                        found = true;
                        nextSegList.Remove(seg);
                        if(nextSegList.IsEmpty()) {
                            if(curIdx!=-2) {
                                segments.RemoveAt(curIdx);
                                curIdx = -1;
                            }
                        }
                    }
                }

                if(!found) {
                    DebugUtil.Error("Point "+ lastPointInSegment + " not found, Polygon merge failed");
                    _isError = true;
                    if(polygonVertices.NonEmpty())
                        DebugUtil.Error(polygonVertices[0].Select(t=>t.ToString()));
                    break;
                } else {
                    _isError = false;
                }

            } while(bridgeSegments.Count + segments.Count >= 0 && maxCount-->0);
            return polygonVertices;
        }



#endregion

        #region merge
        public static List<Polygon> Merge(List<Polygon> polygons, Polygon bridge, bool mergeClosebyPoints) {
            // polygons not overlap, bridge might overlap with multiple polygons and connect them
            HashSet<Vector2> insidePointSet = new();
            HashSet<Vector2> intersectionSet = new();
            Dictionary<Edge,List<Vector2>> polygonsEdgeIntersect = new();
            Dictionary<Edge,List<Vector2>> bridgeEdgeIntersect = new();
            List<Tuple<Polygon,Polygon.OverlapResult>> candidates = new();
            List<Polygon> results = new();

            string logA = _isTest? string.Join("\n", polygons.Select(pol => "A: " + pol)) : "";

            while(polygons.NonEmpty()) {
                Polygon p = polygons.RemoveAtAndReturn(0);
                var overlapRes = p.IsOverlap(bridge, insidePointSet, intersectionSet, polygonsEdgeIntersect, bridgeEdgeIntersect);
                if(overlapRes == Polygon.OverlapResult.INSIDE || overlapRes == Polygon.OverlapResult.SAME) {
                    continue;
                } else if(overlapRes == Polygon.OverlapResult.CONTAIN || overlapRes == Polygon.OverlapResult.SHARE_EDGE_CONTAIN) {
                    results.Add(p);
                    results.AddRange(polygons);
                    return results;
                } else if (overlapRes == Polygon.OverlapResult.SEPARATE) {
                    results.Add(p);
                } else {
                    candidates.Add(new(p,overlapRes));
                }
            }

            if(candidates.NonEmpty()) {
                // merge w/ bridge into one
                List<Tuple<Polygon,List<Edge>>> outsideEdgesList = new();
                List<Edge> edgesB = bridge.Edges;
                AddEdge(edgesB, bridgeEdgeIntersect);
                while(candidates.NonEmpty()) {
                    var tup = candidates.RemoveAtAndReturn(0);
                    Polygon a = tup.Item1;
                    List<Edge> edgesA = a.Edges;
                    AddEdge(edgesA, polygonsEdgeIntersect);
                    int intersectionCount = intersectionSet.Count;
                    AddIntersections(edgesA, edgesB, insidePointSet, intersectionSet);

                    // no new intersection = separate or only 1 edge/point touching
                    if(intersectionSet.Count == intersectionCount &&
                       tup.Item2 != Polygon.OverlapResult.INTERSECT &&
                       tup.Item2 != Polygon.OverlapResult.SHARE_EDGE_INTERSECTABLE) {
                        results.Add(a);
                        continue;
                    }

                    outsideEdgesList.Add(new(a, edgesA));
                }

                if(outsideEdgesList.NonEmpty()) {
                    string logAAfter = _isTest? string.Join(",", outsideEdgesList[0].Item2.Select(x=>x.A)) : "";
                    string logBAfter = _isTest? string.Join(",", edgesB.Select(x=>x.A)) : "";
                    HashSet<Edge> toRemoveFromB = edgesB.Where(e => insidePointSet.Contains(e.A) ||insidePointSet.Contains(e.B) ||
                                    (intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B) &&
                                     outsideEdgesList.Any(t => {
                                        var ce = t.Item1.ContainEdge(t.Item2,e);
                                        return ce == Polygon.ContainEdgeResult.INSIDE || ce == Polygon.ContainEdgeResult.SHARE_EDGE;
                                    }))).ToHashSet();

                    HashSet<Edge> overlapEdges = new();
                    foreach(var t in outsideEdgesList) {
                        var toRemoveFromA = t.Item2.Where(e => {
                            if(insidePointSet.Contains(e.A) || insidePointSet.Contains(e.B)) return true;
                            if(intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B)) {
                                var ce = bridge.ContainEdge(edgesB,e);
                                if(ce == Polygon.ContainEdgeResult.SHARE_EDGE) {
                                    overlapEdges.Add(e);
                                    return true;
                                }
                                return ce == Polygon.ContainEdgeResult.INSIDE;
                            }
                            return false;
                        }).ToList();
                        t.Item2.RemoveAll(e=>toRemoveFromA.Contains(e));
                    }

                    edgesB.RemoveAll(e=>toRemoveFromB.Contains(e) && !overlapEdges.Contains(e));
                    overlapEdges.Clear();
                    var edges = outsideEdgesList.Select(e => e.Item2).Where(e=>e.NonEmpty()).ToList();
                    outsideEdgesList.Clear();

                    List<List<Vector2>> points = Connect(edges, edgesB, intersectionSet);

                    if(_isTest && _isError) {
                        DebugUtil.Log(logA);
                        DebugUtil.Log("B: " + bridge);
                        DebugUtil.Log("A After: " + logAAfter);
                        DebugUtil.Log("B After: " + logBAfter);
                        _isError = false;
                    }

                    points.Where(ps => ps.Count>=3)
                        .Select(ps => new Polygon(ps, mergeClosebyPoints))
                        .Where(pol => pol.IsValid)
                        .Select(pol => CheckSelfCross(pol))
                        .ForEach(pols => results.AddRange(pols));
                } else {
                    results.Add(bridge);
                }
            } else {
                results.Add(bridge);
            }

            return results;
        }

        public static List<Polygon> Merge(List<Polygon> polygons, Polygon bridge) {
            return Merge(polygons, bridge, true);
        }

        public static List<Polygon> Merge(Polygon a, Polygon b) {
            return Merge(new List<Polygon>() {a}, b);
        }
#endregion

#region Merge Connect
        /// <summary>
        /// Merge 2 connected polygons into one
        /// </summary>
        /// <param name="polygonA"></param>
        /// <param name="polygonB"></param>
        /// <param name="mergeClosebyPoints"></param>
        /// <returns>(mergedPolygon, holes)</returns>
        public static List<Polygon> MergeConnectedPolygons(Polygon polygonA, Polygon polygonB, bool mergeClosebyPoints) {
            HashSet<Vector2> insidePointSet = new();
            HashSet<Vector2> intersectionSet = new();
            Dictionary<Edge,List<Vector2>> aEdgeIntersect = new();
            Dictionary<Edge,List<Vector2>> bEdgeIntersect = new();

            string logA = _isTest? ("A: " + polygonA) : "";
            string logB = _isTest? ("B: " + polygonB) : "";

            var overlapRes = polygonA.IsOverlap(polygonB, insidePointSet, intersectionSet, aEdgeIntersect, bEdgeIntersect);
            if(overlapRes == Polygon.OverlapResult.INSIDE || overlapRes == Polygon.OverlapResult.SAME) {
                return new(){polygonB};
            } else if(overlapRes == Polygon.OverlapResult.CONTAIN || overlapRes == Polygon.OverlapResult.SHARE_EDGE_CONTAIN) {
                return new(){polygonA};
            } else if (overlapRes == Polygon.OverlapResult.SEPARATE) {
                throw new System.Exception("Separate");
            }

            List<Edge> edgesB = polygonB.Edges;
            AddEdge(edgesB, bEdgeIntersect);
            List<Edge> edgesA = polygonA.Edges;
            AddEdge(edgesA, aEdgeIntersect);
            int intersectionCount = intersectionSet.Count;
            AddIntersections(edgesA, edgesB, insidePointSet, intersectionSet);

            // no new intersection = separate or only 1 edge/point touching
            if(intersectionSet.Count == intersectionCount &&
                overlapRes != Polygon.OverlapResult.INTERSECT &&
                overlapRes != Polygon.OverlapResult.SHARE_EDGE_INTERSECTABLE) {
                throw new System.Exception("Separate");
            }


            string logAAfter = _isTest? string.Join(",", edgesA.Select(x=>x.A)) : "";
            string logBAfter = _isTest? string.Join(",", edgesB.Select(x=>x.A)) : "";
            HashSet<Edge> overlapEdges = new();
            edgesA.RemoveWhere(e => {
                    if(insidePointSet.Contains(e.A) || insidePointSet.Contains(e.B)) return true;
                    if(intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B)) {
                        var ce = polygonB.ContainEdge(edgesB,e);
                        if(ce == Polygon.ContainEdgeResult.SHARE_EDGE) {
                            overlapEdges.Add(e);
                            return true;
                        }
                        return ce == Polygon.ContainEdgeResult.INSIDE;
                    }
                    return false;
                });

            edgesB.RemoveWhere(e => !overlapEdges.Contains(e) && (insidePointSet.Contains(e.A) ||insidePointSet.Contains(e.B) ||
                            (intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B) &&
                             IsEdgeInOrOnPolygon(polygonA,edgesA,e))));
            overlapEdges.Clear();

            List<List<Vector2>> points = Connect(edgesA, edgesB, intersectionSet);

            if(_isTest && _isError) {
                DebugUtil.Log(logA);
                DebugUtil.Log("B: " + polygonB);
                DebugUtil.Log("A After: " + logAAfter);
                DebugUtil.Log("B After: " + logBAfter);
                _isError = false;
            }

            return points.Select(ps => new Polygon(ps, mergeClosebyPoints))
                            .Where(pol => pol.IsValid)
                            .Select(pol => CheckSelfCross(pol)[0])
                            .ToList();
        }

        private static bool IsEdgeInOrOnPolygon(Polygon pol, List<Edge> edges, Edge e) {
            var res = pol.ContainEdge(edges,e);
            return res == Polygon.ContainEdgeResult.INSIDE || res == Polygon.ContainEdgeResult.SHARE_EDGE;
        }
        private static List<List<Vector2>> Connect(List<Edge> edgesA, List<Edge> edgesB, HashSet<Vector2> intersections) {
            PurgeIntersection(edgesA, edgesB, intersections);

            List<List<Edge>> segmentA = Divide(edgesA, intersections);
            if(IsSegmentLoop(segmentA) && segmentA.Count>2) {
                return new() {GetVerticesFromEdge(segmentA[0], false, false)};
            }
            List<List<Edge>> segmentB = Divide(edgesB, intersections);
            if(IsSegmentLoop(segmentB) && segmentB.Count>2) {
                return new() {GetVerticesFromEdge(segmentB[0], false, false)};
            }

            edgesA.Clear();
            edgesB.Clear();
            intersections.Clear();

            bool isA = true;
            bool isReverse = false;
            List<Edge> seg = segmentA.RemoveAtAndReturn(0);
            List<List<Vector2>> result = new();
            List<Vector2> points = new();
            int maxCount = 100;
            do {

                List<Vector2> vertices = GetVerticesFromEdge(seg, isReverse, true);
                bool ignoreFirst = points.Count > 0 && SamePoint(points[^1], vertices[0]);
                points.AddRange(ignoreFirst? vertices.GetRange(1,vertices.Count - 1) : vertices);

                var lastPointInSegment = vertices[^1];

                if(SamePoint(points[0], lastPointInSegment) ||
                   (segmentA.Count + segmentB.Count == 0 && SamePoint(points[0], lastPointInSegment,10))) {
                    // loop
                    result.Add(new(points));
                    if(segmentA.IsEmpty() && segmentB.IsEmpty()) {
                        break;
                    } else {
                        points.Clear();
                        isReverse = false;
                        isA = !segmentA.IsEmpty();
                        seg = isA? segmentA.RemoveAtAndReturn(0) : segmentB.RemoveAtAndReturn(0);
                        continue;
                    }
                }

                bool found = false;
                List<List<Edge>> nextSegList = isA? segmentB : segmentA;

                foreach(var s in nextSegList) {
                    if(SamePoint(s[0].A, lastPointInSegment)) {
                        seg = s;
                        isReverse = false;
                        nextSegList.Remove(s);
                        found = true;
                        isA = !isA;
                        break;
                    } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                        seg = s;
                        isReverse = true;
                        nextSegList.Remove(s);
                        found = true;
                        isA = !isA;
                        break;
                    }
                }

                if(!found) {
                    nextSegList = isA? segmentA : segmentB;
                    foreach(var s in nextSegList) {
                        if(SamePoint(s[0].A, lastPointInSegment)) {
                            seg = s;
                            isReverse = false;
                            nextSegList.Remove(s);
                            found = true;
                            break;
                        } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                            seg = s;
                            isReverse = true;
                            nextSegList.Remove(s);
                            found = true;
                            break;
                        }
                    }
                }


                if(!found) {
                    // caused by tiny gap on line due to rounding
                    // Debug.LogWarning("Gap found in line");
                    nextSegList = isA? segmentA : segmentB;
                    float minDist = float.PositiveInfinity;
                    float dist;
                    foreach(var s in nextSegList) {
                        dist = Vector2.SqrMagnitude(s[0].A- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = false;
                        }
                        dist = Vector2.SqrMagnitude(s[^1].B- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = true;
                        }
                    }
                    if(minDist<=10) {
                        found = true;
                        nextSegList.Remove(seg);
                    }
                }

                if(!found) {
                    DebugUtil.Error("Point "+ lastPointInSegment + " not found, Polygon merge failed");
                    _isError = true;
                    break;
                } else {
                    _isError = false;
                }

            } while(segmentB.Count + segmentA.Count >= 0 && maxCount-->0);
            return result.OrderByDescending(r=>r.Count).ToList();
        }
#endregion
#region cut
        public static List<Polygon> Cut(Polygon main, List<Polygon> polygons) {
            // polygons not overlap
            HashSet<Vector2> insidePointSet = new();
            HashSet<Vector2> intersectionSet = new();
            List<Tuple<Polygon,Polygon.OverlapResult>> candidates = new();
            Dictionary<Edge,List<Vector2>> polygonsEdgeIntersect = new();
            Dictionary<Edge,List<Vector2>> mainEdgeIntersect = new();
            List<Polygon> results = new();

            string logB = _isTest? ("B: " + string.Join("\n", polygons.Select(pol => pol))) : "";

            while(polygons.NonEmpty()) {
                Polygon p = polygons.RemoveAtAndReturn(0);
                var overlapRes = main.IsOverlap(p, insidePointSet, intersectionSet, mainEdgeIntersect, polygonsEdgeIntersect);
                if(overlapRes == Polygon.OverlapResult.INSIDE || overlapRes == Polygon.OverlapResult.SAME) {
                    return new();
                } else if (overlapRes == Polygon.OverlapResult.SEPARATE) {
                    continue;
                } else {
                    candidates.Add(new(p, overlapRes));
                    if(overlapRes == Polygon.OverlapResult.CONTAIN) {
                        polygons.Remove(p);
                    }
                }
            }

            if(candidates.NonEmpty()) {
                List<Tuple<Polygon,List<Edge>>> insideEdgesList = new();
                List<Edge> edgesA = main.Edges;
                AddEdge(edgesA, mainEdgeIntersect);
                while(candidates.NonEmpty()) {
                    var tup = candidates.RemoveAtAndReturn(0);
                    Polygon b = tup.Item1;
                    List<Edge> edgesB = b.Edges;
                    AddEdge(edgesB, polygonsEdgeIntersect);
                    int intersectionCount = intersectionSet.Count;
                    AddIntersections(edgesA, edgesB, insidePointSet, intersectionSet);

                    // no new intersection = separate or only 1 edge/point touching
                    if(intersectionSet.Count == intersectionCount && tup.Item2 != Polygon.OverlapResult.INTERSECT) {
                        continue;
                    }

                    insideEdgesList.Add(new(b, edgesB));
                }

                if(insideEdgesList.NonEmpty()) {
                    string logAAfter = _isTest? ("A: "+string.Join(",", edgesA.Select(t=>t.A.ToString()))) : "";
                    string logBAfter =_isTest? "B:" + string.Join(" || ", insideEdgesList.Select(t=>t.Item2).Select(edges => string.Join(",", edges.Select(t=>t.A.ToString())))) : "";

                    List<List<Edge>> overlapEdges = new();
                    HashSet<Edge> overlapEdgesNeeded = new();
                    HashSet<Edge> toRemoveFromA = edgesA.Where(e => insidePointSet.Contains(e.A) ||insidePointSet.Contains(e.B) ||
                                    (intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B) &&
                                     insideEdgesList.Any(t => {
                                        var ce = t.Item1.ContainEdge(t.Item2,e);
                                        return ce == Polygon.ContainEdgeResult.INSIDE || ce == Polygon.ContainEdgeResult.SHARE_EDGE;
                                }))).ToHashSet();
                    List<List<Edge>> edges = new();
                    foreach(var t in insideEdgesList) {
                        List<Edge> edgeB = new();
                        List<Edge> bEdgesUncut = t.Item2;
                        bool[] isEdgeOutside = new bool[bEdgesUncut.Count];
                        List<int> sharedEdges = new();
                        for(int i=0;i<bEdgesUncut.Count;i++) {
                            Edge e = bEdgesUncut[i];

                            if(insidePointSet.Contains(e.A) || insidePointSet.Contains(e.B)) edgeB.Add(e);
                            else if(intersectionSet.Contains(e.A) && intersectionSet.Contains(e.B)) {
                                var ce = main.ContainEdge(edgesA,e);
                                if(ce == Polygon.ContainEdgeResult.SHARE_EDGE) {
                                    sharedEdges.Add(i);
                                } else if(ce == Polygon.ContainEdgeResult.INSIDE) {
                                    edgeB.Add(e);
                                } else {
                                    isEdgeOutside[i] = true;
                                }
                            } else {
                                isEdgeOutside[i] = true;
                            }
                        }

                        List<List<int>> overlapIndex = new();
                        sharedEdges.ForEach(s => {
                            int prev = s==0? bEdgesUncut.Count-1 : (s-1);
                            int next = s+1==bEdgesUncut.Count? 0 : (s+1);
                            if(isEdgeOutside[prev] || isEdgeOutside[next]) {
                                foreach(var ix in overlapIndex) {
                                    if(ix[0]==next) {
                                        ix.Insert(0,s);
                                        return;
                                    }
                                    if(ix[^1]==prev) {
                                        ix.Add(s);
                                        return;
                                    }
                                }
                                overlapIndex.Add(new() {s});

                            } else overlapEdgesNeeded.Add(bEdgesUncut[s]);
                        });

                        overlapIndex.Select(ix=> ix.Select(i=>bEdgesUncut[i]).ToList()).ForEach(s=>overlapEdges.Add(s));
                        if(edgeB.NonEmpty()) edges.Add(edgeB);
                    }
                    edgesA.RemoveWhere(e => toRemoveFromA.Contains(e) && !overlapEdgesNeeded.Contains(e));
                    insideEdgesList.Clear();
                    List<List<Vector2>> points = ConnectCut(edgesA, edges, intersectionSet, overlapEdges);

                    if(points == null) return new() {main};

                    if(_isTest && _isError) {
                        DebugUtil.Log("A: " + main);
                        DebugUtil.Log(logB);
                        DebugUtil.Log(logAAfter);
                        DebugUtil.Log(logBAfter);
                        _isError = false;
                    }

                    points.Where(ps => ps.Count>=3)
                        .Select(ps => new Polygon(ps))
                        .Where(pol => pol.IsValid)
                        .Select(pol => CheckSelfCross(pol))
                        .ForEach(pols => results.AddRange(pols));
                } else {
                    results.Add(main);
                }
            } else {
                results.Add(main);
            }

            return results;
        }
        public static List<Polygon> Cut(Polygon a, Polygon b) {
            return Cut(a, new List<Polygon>() {b});
        }
#endregion
#region Connect Cut
        private static List<List<Vector2>> ConnectCut(List<Edge> main, List<List<Edge>> otherEdges, HashSet<Vector2> intersections, List<List<Edge>> overlapEdges) {
            PurgeIntersection(otherEdges, main, intersections);

            List<List<Vector2>> polygonVertices = new();

            List<List<Edge>> mainSegments = Divide(main, intersections);
            if(IsSegmentLoop(mainSegments)) {
                if(mainSegments.Count>2) polygonVertices.Add(GetVerticesFromEdge(mainSegments[0], false, false));
                return polygonVertices;
            }
            List<List<List<Edge>>> segments = otherEdges.Select(e => Divide(e, intersections))
                                            .Where(seg => {
                                                if(IsSegmentLoop(seg)) {
                                                    if(seg.Count>2) {
                                                        polygonVertices.Add(GetVerticesFromEdge(seg[0], false, false));
                                                    }
                                                    return false;
                                                }
                                                return seg.NonEmpty();
                                            }).ToList();

            if(segments.IsEmpty()) {
                DebugUtil.Warn("Cut other edges empty");
                return null;
            }

            main.Clear();
            otherEdges.Clear();
            intersections.Clear();

            int curIdx = 0; // -1 random, -2 main
            bool isReverse = false;
            List<Edge> seg = segments[curIdx].RemoveAtAndReturn(0);
            if(segments[curIdx].IsEmpty()) {
                segments.RemoveAt(curIdx);
                curIdx = -1;
            }

            List<Vector2> points = new();
            int maxCount = 100;
            do {
                List<Vector2> vertices = GetVerticesFromEdge(seg, isReverse, true);
                bool ignoreFirst = points.NonEmpty() && SamePoint(points[^1], vertices[0]);
                points.AddRange(ignoreFirst? vertices.GetRange(1,vertices.Count - 1) : vertices);

                var lastPointInSegment = vertices[^1];

                if(SamePoint(points[0], lastPointInSegment) ||
                   (segments.Count + mainSegments.Count == 0 && SamePoint(points[0], lastPointInSegment,10))) {
                    // loop
                    polygonVertices.Add(points.GetRange(0,points.Count-1));
                    if(segments.IsEmpty() && mainSegments.IsEmpty()) break;
                    points.Clear();
                    if(segments.NonEmpty()) {
                        curIdx = 0;
                        isReverse = false;
                        seg = segments[curIdx].RemoveAtAndReturn(0);
                        if(segments[curIdx].IsEmpty()) {
                            segments.RemoveAt(curIdx);
                            curIdx = -1;
                        }
                    } else {
                        curIdx = -2;
                        isReverse = false;
                        seg = mainSegments.RemoveAtAndReturn(0);
                    }


                    continue;
                }

                bool found = false;
                if(curIdx != -2) {
                    // find in Main
                    foreach(var s in mainSegments) {
                        if(SamePoint(s[0].A, lastPointInSegment)) {
                            seg = s;
                            isReverse = false;
                            mainSegments.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                            seg = s;
                            isReverse = true;
                            mainSegments.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        }
                    }
                    if(!found) {
                        foreach(var s in overlapEdges) {
                            if(SamePoint(s[0].A, lastPointInSegment)) {
                                seg = s;
                                isReverse = false;
                                overlapEdges.Remove(s);
                                found = true;
                                curIdx = -2;
                                break;
                            } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                                seg = s;
                                isReverse = true;
                                overlapEdges.Remove(s);
                                found = true;
                                curIdx = -2;
                                break;
                            }
                        }
                    }
                }

                if(!found) {
                    for(int i=0; i<segments.Count; i++) {
                        if(i == curIdx) continue;
                        List<List<Edge>> nextSegList = segments[i];
                        foreach(var s in nextSegList) {
                            if(SamePoint(s[0].A, lastPointInSegment)) {
                                seg = s;
                                isReverse = false;
                                nextSegList.Remove(s);
                                found = true;
                                break;
                            } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                                seg = s;
                                isReverse = true;
                                nextSegList.Remove(s);
                                found = true;
                                break;
                            }
                        }
                        if(found) {
                            if(nextSegList.IsEmpty()) {
                                curIdx = -1;
                                segments.RemoveAt(i);
                            } else {
                                curIdx = i;
                            }
                            break;
                        }
                    }
                }

                if(!found) {
                    foreach(var s in overlapEdges) {
                        if(SamePoint(s[0].A, lastPointInSegment)) {
                            seg = s;
                            isReverse = false;
                            overlapEdges.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        } else if(SamePoint(s[^1].B, lastPointInSegment)) {
                            seg = s;
                            isReverse = true;
                            overlapEdges.Remove(s);
                            found = true;
                            curIdx = -2;
                            break;
                        }
                    }
                }


                if(!found && curIdx!=-1 && !(curIdx == -2 && mainSegments.IsEmpty())) {
                    // caused by tiny gap on line due to rounding
                    // Debug.LogWarning("Gap found in line");
                    List<List<Edge>> nextSegList =curIdx == -2? mainSegments : segments[curIdx];
                    float minDist = float.PositiveInfinity;
                    float dist;
                    foreach(var s in nextSegList) {
                        dist = Vector2.SqrMagnitude(s[0].A- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = false;
                        }
                        dist = Vector2.SqrMagnitude(s[^1].B- lastPointInSegment);
                        if(dist < minDist) {
                            minDist = dist;
                            seg = s;
                            isReverse = true;
                        }
                    }
                    if(minDist<=10) {
                        found = true;
                        nextSegList.Remove(seg);
                        if(nextSegList.IsEmpty()) {
                            if(curIdx!=-2) {
                                segments.RemoveAt(curIdx);
                                curIdx = -1;
                            }
                        }
                    }
                }

                if(!found) {
                    DebugUtil.Error("Point "+ lastPointInSegment + " not found, Polygon cut failed");
                    _isError = true;
                    break;
                } else {
                    _isError = false;
                }

            } while(mainSegments.Count + segments.Count >= 0 && maxCount-->0);

            return polygonVertices;
        }
#endregion

#region self cross
        public static List<Polygon> CheckSelfCross(Polygon polygon) {
            if(!polygon.IsValid) return new();
            List<Edge> edges = polygon.Edges;
            bool isSelfCross = false;
            int i = 0;
            while(i<edges.Count) {
                Edge e1=edges[i];
                List<Vector2> e1Points = new() {e1.A, e1.B};
                Dictionary<Edge,(Edge,Edge)> replace = new();
                Vector2 intersection = Vector2.zero;
                edges.Skip(i + 1)
                    .ForEach(e2 => {
                        bool intersect = LineSegmentIntersection(e1.A, e1.B, e2.A, e2.B, ref intersection);
                        if(!intersect) return;
                        if (!e1Points.Contains(intersection)) {
                            if(e1Points.Count == 2) e1Points.Insert(1,intersection);
                            else {
                                for(int j = 0;j<e1Points.Count-1;j++) {
                                    if(PointOnLineSegment(intersection,e1Points[j],e1Points[j+1])) {
                                        e1Points.Insert(j+1,intersection);
                                        break;
                                    }
                                }
                            }
                        }
                        if (intersection!=e2.A && intersection!=e2.B) {
                            replace.Add(e2, (new Edge(e2.A,intersection), new Edge(intersection,e2.B)));
                        }
                    });
                isSelfCross = isSelfCross || e1Points.Count>2 || replace.NonEmpty();

                if(e1Points.Count>2) {
                    edges[i] = new(e1Points[0],e1Points[1]);
                    for(int j=1;j<e1Points.Count-1;j++) {
                        edges.Insert(++i, new(e1Points[j],e1Points[j+1]));
                    }
                }
                replace.ForEach(r => {
                    int index = edges.IndexOf(r.Key);
                    edges[index] = r.Value.Item1;
                    edges.Insert(index + 1, r.Value.Item2);
                });
                i+=1;
            }
            HashSet<Vector2> points = polygon.Vertices.ToHashSet();
            isSelfCross = isSelfCross || polygon.Vertices.Count > points.Count; // duplicate points

            if(isSelfCross) {
                var vertices = GetVerticesFromEdge(edges, false, false);
                HashSet<Vector2> crossPoints = vertices.Where(v => !points.Remove(v)).ToHashSet();

                List<List<Vector2>> polygonVertices = new() {vertices};
                crossPoints.ForEach(cp => {
                    int idx = -1;
                    Dictionary<List<Vector2>,List<List<Vector2>>> replace = new();
                    polygonVertices.ForEach(verticeList => {
                        idx++;
                        List<int> occurances = verticeList.FindAllIndex(cp);
                        if(occurances.Count < 2) return;
                        List<List<Vector2>> temp = new();

                        List<Vector2> n = verticeList.GetRange(occurances[^1], verticeList.Count - occurances[^1]);
                        if(occurances[0] > 0) n.AddRange(verticeList.GetRange(0, occurances[0]));
                        temp.Add(n);

                        for(int j = 1; j < occurances.Count; j++) {
                            temp.Add(verticeList.GetRange(occurances[j-1], occurances[j] - occurances[j-1]));
                        }

                        replace.Add(verticeList, temp);
                    });

                    replace.ForEach(r => {
                        polygonVertices.Remove(r.Key);
                        polygonVertices.AddRange(r.Value);
                    });
                });

                return polygonVertices.Select(ps => new Polygon(ps))
                        .Where(pol => pol.IsValid)
                        .OrderByDescending(pol=>pol.Vertices.Count)
                        .ToList();
            }
            return new() {polygon};
        }
#endregion
#region Smooth
        public static Polygon Smooth(Polygon polygon, float minTriangleArea) {
            CircularList<Vector2> vertices = new(polygon.Vertices);
            Dictionary<Vector2,float> areas = new();
            List<Vector2> sortedByArea = new();
            for(int i = 0; i<vertices.Count; i+=1) {
                var prev = vertices[i-1];
                var cur = vertices[i];
                var next = vertices[i+1];

                var area = TriangleArea(prev,cur,next);
                int idx = sortedByArea.FindIndex(p => areas[p]>area);
                if(idx == -1) sortedByArea.Add(cur);
                else sortedByArea.Insert(idx, cur);

                areas.Add(cur,area);

            }

            while(areas[sortedByArea[0]]<minTriangleArea && sortedByArea.Count>3) {
                var p = sortedByArea.RemoveAtAndReturn(0);
                areas.Remove(p, out var a);

                var idx = vertices.IndexOf(p);
                var prevPrev = vertices[idx-2];
                var prev = vertices[idx-1];
                var next = vertices[idx+1];
                var nextNext = vertices[idx+2];
                vertices.RemoveAt(idx);

                var aPrev = TriangleArea(prevPrev, prev, next);
                var aNext = TriangleArea(prev, next, nextNext);

                areas[prev] = aPrev;
                areas[next] = aNext;

                sortedByArea.Remove(prev);
                idx = sortedByArea.FindIndex(x => areas[x]>aPrev);
                if(idx == -1) sortedByArea.Add(prev);
                else sortedByArea.Insert(idx, prev);
                sortedByArea.Remove(next);
                idx = sortedByArea.FindIndex(x => areas[x]>aNext);
                if(idx == -1) sortedByArea.Add(next);
                else sortedByArea.Insert(idx, next);
            }

            return new Polygon(vertices, false);
        }

#endregion

    }
}