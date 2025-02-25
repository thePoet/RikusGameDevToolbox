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
        internal PathsD Paths;
        
        /// Points in the outline of the polygon in CCW order.
        public Vector2[] Contour => Paths[0].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
     
        public float Area => (float)Clipper.Area(Paths);

        /// <summary>
        /// Is the point inside the polygon but not inside it's holes or on it's edges?
        /// </summary>
        public bool IsPointInside(Vector2 point) => PointInPolygon(point) is PointInPolygonResult.IsInside;
        
        /// <summary>
        /// Is the point on the edge of the polygon or it's holes?
        /// </summary>
        public bool IsPointOnEdge(Vector2 point) => PointInPolygon(point) is PointInPolygonResult.IsOn;
 
        public Rect Bounds()
        {
            return CreateRectToEncapsulate(Contour);
        }
        
        public IEnumerable<Edge> Edges()
        {
            foreach ((int a, int b) in PointIndicesForEdges())
            {
                yield return new Edge(ToVector2(Paths[0][a]), ToVector2(Paths[0][b]));
            }
        }
        
        public void Simplify(float tolerance)
        {
            Paths = Clipper.SimplifyPaths(Paths, tolerance);
        }
        
        /// <summary>
        /// Circumference of the contour.
        /// </summary>
        public float Circumference()
        {
            return Edges().Sum(edge => edge.Length);
        }
        
        /// <summary>
        /// Inflates/deflates the polygon outline/holes by the given amount. 
        /// The resulting list of polygons are sorted by area in descending order.
        /// </summary>
        /// <param name="amount">Distance between old and new outline, positive to grow contour and shrink holes.</param>
        /// <returns>List of resulting polygons descending from the largest by area.</returns>
        public List<Polygon> Inflated(float amount)
        {
            var paths = Clipper.InflatePaths(Paths, amount, JoinType.Miter, EndType.Polygon);
          
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            clipper.AddSubject(paths);
            clipper.Execute(ClipType.Union, FillRule.NonZero, polytree);
            var result = PolygonTools.ToPolygons(polytree);
            SortByAreaDescending(result);
            return result;
            
            
            void SortByAreaDescending(List<Polygon> list)
            {
                list.Sort((a, b) => b.Area.CompareTo(a.Area));
            }

        }
        
        public override string ToString()
        {
            return $"Polygon with {Paths.Count} paths. \n{string.Join("\n", Paths.Select(PathAsString))}\n";
            
            String PathAsString(PathD path) => string.Join(", ", path.Select(p => $"({p.x}, {p.y})"));
        }
        
        public void DrawWithGizmos()
        {
            Gizmos.color = Color.green;

            for (int pathIndex=0; pathIndex < Paths.Count; pathIndex++)
            {
                foreach ((int a, int b) in PointIndicesForEdges(pathIndex))
                {
                    Gizmos.DrawLine(ToVector2(Paths[0][a]), ToVector2(Paths[0][b]));
                }
            }
        }

        private PointInPolygonResult PointInPolygon(Vector2 point)
        {
            var p = ToPointD(point);
            var r = Clipper.PointInPolygon(ToPointD(point), Paths[0]);
            if (r!=PointInPolygonResult.IsInside) return r;
            
            // Holes:
            for (int i=1; i<Paths.Count; i++)
            {
                if (Clipper.PointInPolygon(p, Paths[i]) is PointInPolygonResult.IsInside) return PointInPolygonResult.IsOutside;
                if (Clipper.PointInPolygon(p, Paths[i]) is PointInPolygonResult.IsOn) return PointInPolygonResult.IsOn;
            }
            return PointInPolygonResult.IsInside;
            
        }
        
             
        // Returns indices for the starts and ends of edges (0,1), (1,2), (2,3), ..., (n-1,0)
        // where n is the number of edges in the polygon.
        private IEnumerable<(int, int)> PointIndicesForEdges(int pathIndex = 0)
        {
            for (int i = 0; i < Paths[pathIndex].Count; i++)
            {
                yield return i == Paths[pathIndex].Count - 1 ? (i, 0) : (i, i + 1);
            }
        }

        
        protected static PointD ToPointD(Vector2 point) => new (point.x, point.y);
        
        protected static PathD ToPathD(IEnumerable<Vector2> points) => new (points.Select(ToPointD));
        
        protected static Vector2 ToVector2(PointD point) => new ((float)point.x, (float)point.y);


    }
}