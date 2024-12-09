using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using UnityEngine.Assertions;
using Clipper2Lib;

namespace RikusGameDevToolbox.Geometry2d
{
    /// A polygon in 2D space
    /// No holes, self-intersections or duplicate points allowed.
    /// Uses Angus Johnson's awesome Clipper2 library..
    [Serializable]
    public class Polygon : IEquatable<Polygon>
    {
        
        // An intersection of the outlines of two Polygon2D:s.
        private struct OutlineIntersection
        {
            public Vector2 IntersectionPosition;
            public int PointIdx1; // OutlineIntersection is between _points PointIdx1 and PointIdx2
            public int PointIdx2;
            public bool IsStartOfIntersectingArea; // In CW direction
            public override string ToString() => $"Intersection of {PointIdx1} and {PointIdx2} at {IntersectionPosition} is start: {IsStartOfIntersectingArea}"; 
        }

        public List<Vector2> Points
        {
            get => _points;
            set => SetPoints(value);
        }

        [SerializeField]
        private List<Vector2> _points;
        private PathD _pathD;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        /// <summary>
        /// Creates a regular polygon with the given number of sides and radius.
        /// </summary>
        public static Polygon CreateRegular(int numSides, float radius)
        {
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
        /// Return null if it fails.
        /// </summary>
        public static Polygon FromUnorderedPoints(IEnumerable<Vector2> points)
        {
            var list = points.ToList();
            Vector2 center = new Vector2(list.Average(p => p.x), list.Average(p => p.y));
            
            list.Sort(SortByAngle);
            if (!IsInClockwiseOrder(ToPathD(list))) return null;
            return new Polygon( list );
            
            int SortByAngle(Vector2 p1, Vector2 p2)
            {
                float angle1 = Math2d.GetAngle(Vector2.up, p1-center);
                float angle2 = Math2d.GetAngle(Vector2.up, p2-center);
                return -angle1.CompareTo(angle2);
            }
   
        }

        /// <summary>
        /// Returns the intersections (AND) of two polygons i.e. the areas that are common to both polygons.
        /// Intersections are sorted by area in descending order.
        /// </summary>
        public static List<Polygon> Intersection(Polygon poly1, Polygon poly2)
        {
            List<Polygon> intersections = new List<Polygon>();
            PathsD intersection = Clipper.Intersect(poly1.ToPathsD(), poly2.ToPathsD(), FillRule.EvenOdd, 8);
            foreach(var pathd in intersection)
            {
                pathd.Reverse();
                intersections.Add(new Polygon(pathd));
            }
            SortByAreaDescending(intersections);
            return intersections;
        }
        

        
        /// <summary>
        /// Returns the union (OR) of two polygons. Returns null if the polygons do not intersect. 
        /// </summary>
        public static Polygon Union(Polygon poly1, Polygon poly2)
        {
            PathsD union = Clipper.Union(poly1.ToPathsD(), poly2.ToPathsD(), FillRule.EvenOdd, 8);
            if (union.Count != 1) return null;
            union[0].Reverse();
            return new Polygon(union[0]);
        }
        
        public static List<Polygon> Union(List<Polygon> polygons)
        {
            PathsD union = new();
            foreach (var polygon in polygons)
            {
                union = Clipper.Union(union, polygon.ToPathsD(), FillRule.EvenOdd, 8);
            }
 
            List<Polygon> result = new();
            for (int i=0; i<union.Count; i++)
            {
                union[i].Reverse();
                result.Add(new Polygon(union[i]));
            }
            return result;
        }
        
        /// <summary>
        /// Returns the polygon that is poly1 NOT poly2 i.e. subtracts poly2 from poly1.
        /// The resulting polygons are sorted by area in descending order.
        /// </summary>
        public static List<Polygon> Subtract(Polygon poly1, Polygon poly2)
        {
            List<Polygon> result = new List<Polygon>();
            PathsD diffs = Clipper.Difference(poly1.ToPathsD(), poly2.ToPathsD(), FillRule.EvenOdd, 8);
            foreach(var pathd in diffs)
            {
                pathd.Reverse();
                result.Add(new Polygon(pathd));
            }
            SortByAreaDescending(result);
            return result;
        }

        /// <summary>
        /// Inflates/deflates the polygon by the given amount. 
        /// Creates new polygon(s) so that their outline is parallel to original
        /// The resulting list of polygons are sorted by area in descending order.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="amount">Distance between old and new outline.</param>
        /// <returns>List of resulting polygons descending from the largest by area.</returns>
        public static List<Polygon> Inflate(Polygon polygon, float amount)
        {
            var paths = Clipper.InflatePaths(polygon.ToPathsD(), amount, JoinType.Miter, EndType.Polygon);
            List<Polygon> result = new List<Polygon>();
            foreach(var pathd in paths)
            {
                result.Add(new Polygon(pathd));
            }
            SortByAreaDescending(result);
            return result;
        }

        /// <summary>
        /// Constructor for a polygon with the given points. 
        /// </summary>
        /// <param name="points">Points of the of polygon  clockwise order.</param>
        public Polygon(IEnumerable<Vector2> points)
        {
            SetPoints(points);
        }

        /// <summary>
        /// Is the point inside the polygon or on the edge of it?
        /// </summary>
        public bool IsPointInside(Vector2 point)
        {
            return Clipper.PointInPolygon(ToPointD(point), _pathD) is PointInPolygonResult.IsInside;
        }
        
        /// <summary>
        /// Is the point on the edge of the polygon?
        /// </summary>
        public bool IsPointOnEdge(Vector2 point)
        {
            return Clipper.PointInPolygon(ToPointD(point), _pathD) is PointInPolygonResult.IsOn;
        }
        
        public bool IsIntersecting(Polygon other)
        {
            var polygon = this;
            return _points.Any(p => other.IsPointInside(p)) || other._points.Any(p => polygon.IsPointInside(p));
        }



        /// <summary>
        /// Return true if all the points of this polygon are inside or on the edge of the other polygon. 
        /// </summary>
        public bool IsInsideOf(Polygon other)
        {
            return _points.All(p=> other.IsPointInside(p) || other.IsPointOnEdge(p));
        }
  
        public IEnumerable<Edge> Edges()
        {
            foreach ((int a, int b) in PointIndicesForEdges())
            {
                yield return new Edge(_points[a], _points[b]);
            }
        }
        
        public bool IsSharingVerticesWith(Polygon other) => _points.Any(point => other._points.Contains(point));

        public float Area()
        {
            return (float)Clipper.Area(_pathD);
        }
        
        public float Circumference()
        {
            return Edges().Sum(edge => edge.Length);
        }

        public bool IsConvex()
        {
            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 p0 = _points[i];
                Vector2 p1 = _points[(i + 1) % _points.Count];
                Vector2 p2 = _points[(i + 2) % _points.Count];

                Vector2 v1 = p1 - p0;
                Vector2 v2 = p2 - p1;

                float crossProduct = v1.x * v2.y - v1.y * v2.x;
                if (crossProduct > 0f) return false;
            }
            return true;
        }

        public Polygon Translate(Vector2 offset)
        {
            return ForEachPoint(p => p + offset);
        }

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

        /// <summary>
        /// Return new polygon with the given method applied to each point.
        /// </summary>
        public Polygon ForEachPoint(Func<Vector2, Vector2> func)
        {
            return new Polygon(_points.Select(func));
        }

        public int NumSharedVerticesWith(Polygon other)
        {
            //Todo: Make more efficient
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

        public Rect Bounds()
        {
            var bounds = new Rect();
            bounds.Bound(_points);
            return bounds;
        }
        
        public Vector2 AverageOfPoints()
        {
            Vector2 sum = Vector2.zero;
            foreach (var point in _points)
            {
                sum += point;
            }
            return sum / _points.Count;
        }
        
        public Vector2 Centroid()
        {
            // https://stackoverflow.com/a/34732659

            var centroid = Vector2.zero;
            float area = 0;

            for (int i = 0; i < _points!.Count; i++)
            {
                int i2 = i == _points.Count - 1 ? 0 : i + 1;

                float xi = _points[i].x;
                float yi = _points[i].y;
                float xi2 = _points[i2].x;
                float yi2 = _points[i2].y;

                float mult = (xi * yi2 - xi2 * yi) / 3f;
        
                Vector2 add = mult * new Vector2( xi + xi2, yi + yi2 );
             
                float addArea = xi * yi2 - xi2 * yi;

                if (i == 0)
                {
                    centroid = add;
                    area = addArea;
                }
                else
                {
                    centroid += add;
                    area += addArea;
                }
            }
          
            centroid /= area;

            return centroid;
        }
        
        
        // TODO: Make more efficient
        public bool Equals(Polygon other)
        {
            if (other == null) return false;
            bool sameNumberOfPoints = _points.Count == other._points.Count;
            bool samePoints = _points.All(point => other._points.Contains(point));
            return sameNumberOfPoints && samePoints;
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

        public void DrawWithGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var edge in Edges())
            {
                Gizmos.DrawLine(edge.Point1, edge.Point2);
            }
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private static void SortByAreaDescending(List<Polygon> list)
        {
            list.Sort((a, b) => b.Area().CompareTo(a.Area()));
        }

        private void SetPoints(IEnumerable<Vector2> points)
        {
            _points = new List<Vector2>(points);
            if (_points.Count() < 3)
            {
                throw new ArgumentException("A polygon must have at least 3 sides.");
            }
            _pathD = ToPathD(_points);
            Assert.IsTrue(IsInClockwiseOrder(_pathD), "Polygon's points must be given in clockwise order.");
        }
        
        private Polygon(PathD path)
        {
            _pathD = path;
            _points = new List<Vector2>();
            foreach (var point in _pathD)
            {
                _points.Add(new Vector2((float)point.x, (float)point.y));
            }
            Assert.IsTrue(IsInClockwiseOrder(_pathD), "Polygon's points must be given in clockwise order.");
        }

        private static PointD ToPointD(Vector2 point) => new (point.x, point.y);
        
        private static PathD ToPathD(IEnumerable<Vector2> points) => new (points.Select(ToPointD));
        
        private static bool IsInClockwiseOrder(PathD pathD) => !Clipper.IsPositive(pathD);

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

        private PathsD ToPathsD() => new PathsD { _pathD };
        
        // Returns indices for the starts and ends of edges (0,1), (1,2), (2,3), ..., (n-1,0)
        // where n is the number of edges in the polygon.
        private IEnumerable<(int, int)> PointIndicesForEdges()
        {
            for (int i = 0; i < _points.Count; i++)
            {
                yield return i == _points.Count - 1 ? (i, 0) : (i, i + 1);
            }
        }
        


        #endregion

        
        
        
    }
}