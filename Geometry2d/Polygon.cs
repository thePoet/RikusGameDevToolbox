using System;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using UnityEngine;
using static RikusGameDevToolbox.GeneralUse.RectExtensions;

namespace RikusGameDevToolbox.Geometry2d
{
    /// <summary>
    /// A 2d shape with single outline. Superclass for SimplePolygon and PolygonWithHoles.
    /// </summary>
    public abstract class Polygon
    {
        internal PathsD PathsD;
        
        public static List<Polygon> CreateFromPaths(IEnumerable<IEnumerable<Vector2>> paths)
        {
           var pathsD = new PathsD();
           pathsD.AddRange(paths.Select(PathUtils.ToPathD));
           PolyTreeD polyTree = GeometryUtils.ToPolyTree(pathsD);
           return GeometryUtils.ToPolygons(polyTree);
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
        
        public int NumHoles => PathsD.Count(path => !Clipper.IsPositive(path));
        
        /// Points in the outline of the polygon in CCW order.
        public Vector2[] Contour => PathUtils.ToVector2Array(PathsD[0]);
        
  
        /// <summary>
        /// Return the paths of the polygon. The first is the outline/contour of the polygon (in CCW order) and the rest are
        /// the holes (in CW order).
        /// </summary>
        public Vector2[][] Paths => PathsD.Select(PathUtils.ToVector2Array).ToArray();
        

        public float Area => (float)Clipper.Area(PathsD);

        /// <summary>
        /// Is the point inside the polygon but not inside it's holes or on it's edges?
        /// </summary>
        public bool IsPointInside(Vector2 point) => PointInPolygon(point) is PointInPolygonResult.IsInside;
        
        
        
        
        /// <summary>
        /// Is the point on the edge of the polygon including edges of it's holes?
        /// </summary>
        /// <param name="point"></param>
        /// <param name="precision">Max allowed distance from edge</param>
        public bool IsPointOnEdge(Vector2 point, float precision = 0.0001f)
        {
            foreach ( (Vector2 edgeStart, Vector2 edgeEnd) in Edges())
            {
                if (GeometryUtils.IsPointOnEdge(point, edgeStart, edgeEnd, precision)) return true;
            }
            return false;
        }


        public Rect Bounds()
        {
            return CreateRectToEncapsulate(Contour);
        }
        

        
        /// <summary>
        /// Returns edges of the polygon contour and its holes.
        /// </summary>
        public IEnumerable<(Vector2, Vector2)> Edges()
        {
            for (int i = 0; i < PathsD.Count; i++)
            {
                foreach ((int a, int b) in PointIndicesForEdges(i))
                {
                    yield return new (ToVector2(PathsD[i][a]), ToVector2(PathsD[i][b]));
                }
            }
        }

        /// <summary>
        /// Returns edges of on the given path. Path 0 is the contour of the polygon.
        /// </summary>
        public IEnumerable<(Vector2, Vector2)> Edges(int pathIndex)
        {
            foreach ((int a, int b) in PointIndicesForEdges(pathIndex))
            {
                yield return new(ToVector2(PathsD[pathIndex][a]), ToVector2(PathsD[pathIndex][b]));
            }
        }

        
        public void Simplify(float tolerance)
        {
            PathsD = Clipper.SimplifyPaths(PathsD, tolerance);
        }
        
        /// <summary>
        /// Circumference of the contour.
        /// </summary>
        public float Circumference()
        {
            return Edges(0).Sum(edge => Vector2.Distance(edge.Item1, edge.Item2));
        }
        
        
        public SimplePolygon DiscardHoles()
        {
            return new SimplePolygon(PathsD[0]);
        }

        /// <summary>
        /// Inflates/deflates the polygon outline/holes by the given amount. 
        /// The resulting list of polygons are sorted by area in descending order.
        /// </summary>
        /// <param name="amount">Distance between old and new outline, positive to grow contour and shrink holes.</param>
        /// <param name="roundedCorners">See: https://www.angusj.com/clipper2/Docs/Units/Clipper/Types/JoinType.htm</param>
        /// <returns>List of resulting polygons descending from the largest by area.</returns>
        public List<Polygon> Inflated(float amount, bool roundedCorners = false)
        {
            JoinType joinType = JoinType.Miter;
            if (roundedCorners) joinType = JoinType.Round;
            PathsD paths= Clipper.InflatePaths(PathsD, amount, joinType, EndType.Polygon);
          
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            clipper.AddSubject(paths);
            clipper.Execute(ClipType.Union, FillRule.NonZero, polytree);
            var result = GeometryUtils.ToPolygons(polytree);
            SortByAreaDescending(result);
            return result;
            
            
            void SortByAreaDescending(List<Polygon> list)
            {
                list.Sort((a, b) => b.Area.CompareTo(a.Area));
            }

        }
        
        public override string ToString()
        {
            return $"Polygon with {PathsD.Count} paths. \n{string.Join("\n", PathsD.Select(PathAsString))}\n";
            
            String PathAsString(PathD path) => string.Join(", ", path.Select(p => $"({p.x}, {p.y})"));
        }
        
        public void DrawWithGizmos()
        {
            Gizmos.color = Color.green;

            for (int pathIndex=0; pathIndex < PathsD.Count; pathIndex++)
            {
                foreach ((int a, int b) in PointIndicesForEdges(pathIndex))
                {
                    Gizmos.DrawLine(ToVector2(PathsD[0][a]), ToVector2(PathsD[0][b]));
                }
            }
        }

        protected static PointD ToPointD(Vector2 point) => new (point.x, point.y);
        
        protected static Vector2 ToVector2(PointD point) => new ((float)point.x, (float)point.y);



        
        
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        /// <summary>
        /// Tests if the point is inside the polygon or on its edges.
        /// TODO: This does not usually detect correctly if point is on the edges.
        /// </summary>
        private PointInPolygonResult PointInPolygon(Vector2 point)
        {
            var p = ToPointD(point);
            var result = Clipper.PointInPolygon(ToPointD(point), PathsD[0], precision: 2);
            
            if (result!=PointInPolygonResult.IsInside) return result;

            // Holes:
            for (int i=1; i<PathsD.Count; i++)
            {
                if (Clipper.PointInPolygon(p, PathsD[i]) is PointInPolygonResult.IsInside) return PointInPolygonResult.IsOutside;
                if (Clipper.PointInPolygon(p, PathsD[i]) is PointInPolygonResult.IsOn) return PointInPolygonResult.IsOn;
            }
            return PointInPolygonResult.IsInside;

        }
        
             
        // Returns indices for the starts and ends of edges (0,1), (1,2), (2,3), ..., (n-1,0)
        // where n is the number of edges in the polygon.
        private IEnumerable<(int, int)> PointIndicesForEdges(int pathIndex = 0)
        {
            for (int i = 0; i < PathsD[pathIndex].Count; i++)
            {
                yield return i == PathsD[pathIndex].Count - 1 ? (i, 0) : (i, i + 1);
            }
        }

        #endregion



    }
}