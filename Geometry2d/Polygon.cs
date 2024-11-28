using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    /// A polygon in 2D space
    /// Holes, self-intersections and duplicate points are not allowed.
    [Serializable]
    public struct Polygon : IEquatable<Polygon>
    {
        
        // An intersection of the outlines of two Polygon2D:s.
        struct OutlineIntersection
        {
            public Vector2 IntersectionPosition;
            public int PointIdx1; // OutlineIntersection is between _points PointIdx1 and PointIdx2
            public int PointIdx2;
            public bool IsStartOfIntersectingArea; // In CW direction
            public override string ToString() => $"Intersection of {PointIdx1} and {PointIdx2} at {IntersectionPosition} is start: {IsStartOfIntersectingArea}"; 
        }

        public Vector2[] Points => _points.ToArray();
        
        [SerializeField]
        private List<Vector2> _points;
    

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        /// <summary>
        /// Creates a regular polygon with the given number of sides and radius.
        /// </summary>
        public static Polygon CreateRegular(int numSides, float radius)
        {
            if (numSides < 3)
            {
                throw new ArgumentException("A polygon must have at least 3 sides.");
            }
            
            var points = new Vector2[numSides];
            for (int i = 0; i < numSides; i++)
            {
                points[i] = new Vector2(radius * Mathf.Cos(2 * Mathf.PI * -i / numSides), 
                    radius * Mathf.Sin(2 * Mathf.PI * -i / numSides)) * radius;
            }

            return new Polygon(points);
        }
        
        /// <summary>
        /// Attemps to creates a polygon from the given unordered points. Works if the polygon is convex or nearly so.
        /// </summary>
        public static Polygon FromUnorderedPoints(IEnumerable<Vector2> points)
        {
            var list = points.ToList();
            Vector2 center = new Vector2(list.Average(p => p.x), list.Average(p => p.y));
            
            list.Sort(SortByAngle);
            return new Polygon( list );
            
            int SortByAngle(Vector2 p1, Vector2 p2)
            {
                float angle1 = Math2d.GetAngle(Vector2.up, p1-center);
                float angle2 = Math2d.GetAngle(Vector2.up, p2-center);
                return -angle1.CompareTo(angle2);
            }
   
        }
        
        /// <summary>
        /// Constructor for a polygon with the given points.
        /// </summary>
        /// <param name="points">Points of the of polygon in clockwise order.</param>
        public Polygon(IEnumerable<Vector2> points)
        {
            _points = new List<Vector2>(points);
        }

        /// <summary>
        /// Is a point inside the polygon?
        /// </summary>
        public bool IsPointInside(Vector2 point)
        {
            Vector2 pointFarAway = new Vector2(10001000f, 10003000f);
            int numIntersections = 0;
            foreach ((int a, int b) in PointIndicesForEdges())
            {
                if (Math2d.AreLineSegmentsIntersecting(point, pointFarAway, _points[a], _points[b])) numIntersections++;
            }
            return numIntersections % 2 == 1;
        }


        public override string ToString()
        {
            string result = "Polygon with " + _points.Count + "  points: ";
            foreach (var point in _points)
            {
                result += point + " ";
            }
            return result;
        }
        
        public IEnumerable<Edge> Edges()
        {
            foreach ((int a, int b) in PointIndicesForEdges())
            {
                yield return new Edge(_points[a], _points[b]);
            }
        }

        public bool IsOutlineIntersecting(Polygon other)
        {
            foreach (var e1 in Edges())
            {
                foreach (var e2 in other.Edges())
                {
                    if (Math2d.AreLineSegmentsIntersecting(e1.Point1, e1.Point2, e2.Point1, e2.Point2)) return true;
                }
            }

            return false;
        }
        
        
        public bool IsSharingVerticesWith(Polygon other) => _points.Any(point => other._points.Contains(point));
        


   

        public Polygon Transform(Transform transform)
        {
            return ForEachPoint(p => transform.TransformPoint(p));
          
        }

        public Polygon InverseTransform(Transform transform)
        {
            return ForEachPoint(p => transform.InverseTransformPoint(p));
        }

        public Polygon MakeCopy()
        {
            return new Polygon(_points);
        }

        public Polygon ForEachPoint(Func<Vector2, Vector2> func)
        {
            return new Polygon(_points.Select(func));
        }

        public int NumSharedVerticesWith(Polygon other)
        {
            int result = 0;
            foreach (var myPoint in _points)
            {
                foreach (var theirPoint in other._points)
                {
                    if (myPoint==theirPoint) result++;
                }
            }
            return result;
        }


        /// <summary>
        /// Removes some of the common area polygon has with polygon B.
        /// When applied to both polygons, the cease to overlap. 
        /// </summary>
        public Polygon PartiallySubtract(Polygon polygon)
        {
            var intersections = OutlineIntersections(this, polygon);
            if (intersections.Count == 0) return MakeCopy();
            
            // If necessary, reorder the intersection _points so that the first intersection
            // marks the start of the intersecting area.
            if (!intersections[0].IsStartOfIntersectingArea)
            {
                intersections.Insert(0, intersections[^1]);
                intersections.RemoveAt(intersections.Count - 1);
            }

            return CutBetweenIntersections(intersections[0], intersections[1], this);


            Polygon CutBetweenIntersections(OutlineIntersection i1, OutlineIntersection i2, Polygon polygonToBeCut)
            {
                var result = polygonToBeCut.MakeCopy();

                if (AreIntersectionInSameEdge()) return result;
              
                if (AreIntersectionInConsecutiveEdges())
                {
                    result._points.Insert(i1.PointIdx2, DirtyFix(i1.IntersectionPosition));
                    result._points[i1.PointIdx2 + 1] = DirtyFix(i2.IntersectionPosition);
                    return result;
                }
                
                result._points[i1.PointIdx2] = DirtyFix(i1.IntersectionPosition);
                result._points[i2.PointIdx1] = DirtyFix(i2.IntersectionPosition);
                
                if (i1.PointIdx2 < i2.PointIdx1 )
                {
                    int numPointsBetween = i2.PointIdx1 - i1.PointIdx2 - 1;
                    if (numPointsBetween > 0) result._points.RemoveRange(i1.PointIdx2 + 1, numPointsBetween);
                }
                else
                {
                    result._points.RemoveRange(i1.PointIdx2, result._points.Count - i1.PointIdx2 - 1);
                    result._points.RemoveRange(0, i2.PointIdx2-1);
                }

                return result;

                bool AreIntersectionInSameEdge() => i1.PointIdx1 == i2.PointIdx1;
                bool AreIntersectionInConsecutiveEdges() =>  i1.PointIdx2 == i2.PointIdx1;

                Vector2 DirtyFix(Vector2 point)
                {
                    return point - (point-polygonToBeCut.AverageOfPoints())*0.01f;
                }
            
            }
            
     
        }
        
        public bool Equals(Polygon other)
        {
            bool sameNumberOfPoints = _points.Count == other._points.Count;
            bool samePoints = _points.All(point => other._points.Contains(point));
            return sameNumberOfPoints && samePoints;
        }

        public override bool Equals(object obj) => obj is Polygon other && Equals(other);
        public static bool operator == (Polygon p1, Polygon p2) => p1.Equals(p2);
        public static bool operator != (Polygon p1, Polygon p2) => !p1.Equals(p2);
        
        public override int GetHashCode()
        {
            return (_points != null ? _points.GetHashCode() : 0);
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private static List<OutlineIntersection> OutlineIntersections(Polygon a, Polygon b)
        {
            List<OutlineIntersection> intersections = new List<OutlineIntersection>();
            
            foreach ((int a1, int a2) in a.PointIndicesForEdges())
            {
                foreach ((int b1, int b2) in b.PointIndicesForEdges())
                {
                    var result = Math2d.LineSegmentIntersection(a._points[a1], a._points[a2], b._points[b1], b._points[b2]);
                    if (result.AreIntersecting)
                    {
                        intersections.Add(new OutlineIntersection
                        {
                            IntersectionPosition = result.intersectionPoint,
                            PointIdx1 = a1,
                            PointIdx2 = a2,
                            IsStartOfIntersectingArea = !Math2d.IsPointLeftOfLine(b._points[b1], a._points[a1], a._points[a2])
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
        private IEnumerable<(int, int)> PointIndicesForEdges()
        {
            for (int i = 0; i < _points.Count; i++)
            {
                yield return i == _points.Count - 1 ? (i, 0) : (i, i + 1);
            }
        }


                
        Vector2 AverageOfPoints()
        {
            Vector2 sum = Vector2.zero;
            foreach (var point in _points)
            {
                sum += point;
            }
            return sum / _points.Count;
        }
        

        #endregion

        
        
        
    }
}