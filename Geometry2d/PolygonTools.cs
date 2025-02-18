using System;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public static class PolygonTools
    {
        // An intersection of the outlines of two Polygon2D:s.
        private struct OutlineIntersection
        {
            public Vector2 IntersectionPosition;
            public int PointIdx1; // OutlineIntersection is between _points PointIdx1 and PointIdx2
            public int PointIdx2;
            public bool IsStartOfIntersectingArea; // In CCW direction
            public override string ToString() => $"Intersection of {PointIdx1} and {PointIdx2} at {IntersectionPosition} is start: {IsStartOfIntersectingArea}"; 
        }
        
        
        /// <summary>
        /// Creates a regular polygon with the given number of sides and radius.
        /// </summary>
        public static SimplePolygon CreateRegular(int numSides, float radius)
        {
            var points = new Vector2[numSides];
            for (int i = 0; i < numSides; i++)
            {
                points[i] = new Vector2(Mathf.Cos(2 * Mathf.PI * i / numSides), 
                    Mathf.Sin(2 * Mathf.PI * i / numSides)) * radius;
            }
            return new SimplePolygon(points);
        }
        
        
        /// <summary>
        /// Attemps to creates a simple polygon from the given unordered points. Works if the polygon is convex or nearly so.
        /// Return null if it fails.
        /// </summary>
        public static SimplePolygon CreateFromUnorderedPoints(IEnumerable<Vector2> points)
        {
            // TODO: Try gift wrapping algorithm
            var list = points.ToList();
            Vector2 center = new Vector2(list.Average(p => p.x), list.Average(p => p.y));
            
            list.Sort(SortByAngle);
            var path = ToPathD(list);
            if (!Clipper.IsPositive(path)) return null;
          
            return new SimplePolygon( path );
            
            int SortByAngle(Vector2 p1, Vector2 p2)
            {
                float angle1 = Math2d.GetAngle(Vector2.up, p1-center);
                float angle2 = Math2d.GetAngle(Vector2.up, p2-center);
                return angle1.CompareTo(angle2);
            }
   
            PathD ToPathD(IEnumerable<Vector2> points) => new (points.Select(ToPointD)); 
            PointD ToPointD(Vector2 point) => new (point.x, point.y);
        }
        
        
         /// <summary>
        /// Removes some of the common area polygon has with polygon B.
        /// When applied to both polygons, the cease to overlap. 
        /// </summary>
        public static SimplePolygon PartiallySubtract(SimplePolygon a, SimplePolygon simplePolygon)
        {
            var intersections = OutlineIntersections(a, simplePolygon);
            if (intersections.Count == 0) return a.MakeCopy();
            
            // If necessary, reorder the intersection _points so that the first intersection
            // marks the start of the intersecting area.
            if (!intersections[0].IsStartOfIntersectingArea)
            {
                intersections.Insert(0, intersections[^1]);
                intersections.RemoveAt(intersections.Count - 1);
            }

            return CutBetweenIntersections(intersections[0], intersections[1], a);


            SimplePolygon CutBetweenIntersections(OutlineIntersection i1, OutlineIntersection i2, SimplePolygon polygonToBeCut)
            {
                var result = polygonToBeCut.MakeCopy();

                var points = result.Contour.ToList();

                if (AreIntersectionInSameEdge()) return result;
              
                if (AreIntersectionInConsecutiveEdges())
                {
                    points.Insert(i1.PointIdx2, DirtyFix(i1.IntersectionPosition));
                    points[i1.PointIdx2 + 1] = DirtyFix(i2.IntersectionPosition);
                    return new SimplePolygon(points);
                }
                
                points[i1.PointIdx2] = DirtyFix(i1.IntersectionPosition);
                points[i2.PointIdx1] = DirtyFix(i2.IntersectionPosition);
                
                if (i1.PointIdx2 < i2.PointIdx1 )
                {
                    int numPointsBetween = i2.PointIdx1 - i1.PointIdx2 - 1;
                    if (numPointsBetween > 0) points.RemoveRange(i1.PointIdx2 + 1, numPointsBetween);
                }
                else
                {
                    points.RemoveRange(i1.PointIdx2, points.Count - i1.PointIdx2 - 1);
                    points.RemoveRange(0, i2.PointIdx2-1);
                }

                return new SimplePolygon(points);

                bool AreIntersectionInSameEdge() => i1.PointIdx1 == i2.PointIdx1;
                bool AreIntersectionInConsecutiveEdges() =>  i1.PointIdx2 == i2.PointIdx1;

                Vector2 DirtyFix(Vector2 point)
                {
                    return point - (point-polygonToBeCut.AverageOfPoints())*0.01f;
                }
            
            }
        }
         
        internal static List<Polygon> ToPolygons(PolyTreeD tree)
        {
            var result = new List<Polygon>();

            foreach (PolyPathD pp in tree)
            {
                ProcessPolygon(pp);
            }
            return result;


            void ProcessPolygon(PolyPathD polyPath)
            {
                if (polyPath.IsHole) throw new ArgumentException("ProcessPolygon called with a hole.");
                
                if (polyPath.Count==0) // No holes
                {
                    result.Add(new SimplePolygon(polyPath.Polygon));
                    return;
                }
                
                PathsD paths = new();
                paths.Add(polyPath.Polygon);
                foreach (PolyPathD child in polyPath)
                {
                    if (!child.IsHole) throw new ArgumentException("ProcessPolygon called with a non-hole child.");
                    paths.Add(child.Polygon);
                    foreach (PolyPathD grandChild in child) // The islands inside the hole
                    {
                        ProcessPolygon(grandChild);
                    }
                }
                
                result.Add(new PolygonWithHoles(paths));
            }
            
        }

         
        private static List<OutlineIntersection> OutlineIntersections(SimplePolygon a, SimplePolygon b)
        {
            List<OutlineIntersection> intersections = new List<OutlineIntersection>();
            
            foreach ((int a1, int a2) in PointIndicesForEdges(a))
            {
                foreach ((int b1, int b2) in PointIndicesForEdges(b))
                {
                    var result = Math2d.LineSegmentIntersection(a.Contour[a1], a.Contour[a2], b.Contour[b1], b.Contour[b2]);
                    if (result.AreIntersecting)
                    {
                        intersections.Add(new OutlineIntersection
                        {
                            IntersectionPosition = result.intersectionPoint,
                            PointIdx1 = a1,
                            PointIdx2 = a2,
                            IsStartOfIntersectingArea = Math2d.IsPointLeftOfLine(b.Contour[b1], a.Contour[a1], a.Contour[a2])
                        });
                    }
                }
            }
            
            if (intersections.Count % 2 == 1)
            {
                
                Debug.LogError("Odd number of intersection. shared vertices: " + a.NumSharedVerticesWith(b));
                
                intersections.Clear();
            }
     
            for(int i=1; i<intersections.Count; i++)
            {
                if (intersections[i].IsStartOfIntersectingArea == intersections[i - 1].IsStartOfIntersectingArea)
                {
                    Debug.LogError("OutlineIntersection _points are not alternating.");
                    intersections.Clear();
                    return intersections;
                }
            }
            
            return intersections;
        }

        
        // Returns indices for the starts and ends of edges (0,1), (1,2), (2,3), ..., (n-1,0)
        // where n is the number of edges in the polygon.
        private static IEnumerable<(int, int)> PointIndicesForEdges(SimplePolygon polygon)
        {
            for (int i = 0; i < polygon.Contour.Length; i++)
            {
                yield return i == polygon.Contour.Length - 1 ? (i, 0) : (i, i + 1);
            }
        }

    }
}