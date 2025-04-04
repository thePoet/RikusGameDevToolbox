using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public static class PolygonBoolean
    {
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
         /// <summary>
        /// Returns the union (OR) of two polygons. 
        /// </summary>
        public static List<Polygon> Union(Polygon a, Polygon b)
        {
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            clipper.AddSubject(a.PathsD);
            clipper.AddSubject(b.PathsD);
            clipper.Execute(ClipType.Union, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }

        
        /// <summary>
        /// Returns the union (OR) of the list of polygons. 
        /// </summary>
        public static List<Polygon> Union(IEnumerable<Polygon> polygons)
        {
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            foreach (var polygon in polygons)
            {
                clipper.AddSubject(polygon.PathsD);
            }
            clipper.Execute(ClipType.Union, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }
        
        /// <summary>
        /// Returns the intersections (AND) of two polygons i.e. the areas that are common to both polygons.
        /// Intersections are sorted by area in descending order.
        /// </summary>
        public static List<Polygon> Intersection(Polygon a, Polygon b)
        {
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            clipper.AddSubject(a.PathsD);
            clipper.AddClip(b.PathsD);
            clipper.Execute(ClipType.Intersection, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }

        /// <summary>
        /// Returns the polygon that is poly1 NOT poly2 i.e. subtracts poly2 from poly1.
        /// </summary>
        public static List<Polygon> Subtract(Polygon poly1, Polygon poly2)
        {
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            clipper.AddSubject(poly1.PathsD);
            clipper.AddClip(poly2.PathsD);
            clipper.Execute(ClipType.Difference, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }
        
        /// <summary>
        /// Subtracts the second list of polygons from the first list of polygons and returns the result.
        /// </summary>
        public static List<Polygon> Subtract(List<Polygon> poly1, List<Polygon> poly2)
        {
            PolyTreeD polytree = new();
            ClipperD clipper = new();
            foreach (var polygon in poly1)
            {
                clipper.AddSubject(polygon.PathsD);
            }
            foreach (var polygon in poly2)
            {
                clipper.AddClip(polygon.PathsD);
            }
            clipper.Execute(ClipType.Difference, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }

        /// <summary>
        /// If polygon has a point that lies on the edge of the other polygon, this inserts a mathing point to the
        /// edge if one does not already exist.
        /// Horribly inefficient, use only for small polygons!!!
        /// </summary>
        public static void MatchPoints(Polygon poly1, Polygon poly2, float tolerance)
        {
            MatchPaths(poly1.PathsD, poly2.PathsD);
            MatchPaths(poly2.PathsD, poly1.PathsD);

            void MatchPaths(PathsD a, PathsD b)
            {
                foreach (PathD aPath in a)
                {
                    foreach (var aPoint in aPath)
                    {
                        foreach (PathD bPath in b)
                        {
                            Match(aPoint, bPath);
                        }
                    }
                }
            }

            void Match(PointD point, PathD path)
            {
                if (path.Any(dp => DistanceWithinTolerance(dp, point))) return;

                for (int i = 0; i < path.Count; i++)
                {
                    PointD edge1 = path[i];
                    int nextIndex = (i + 1) % path.Count;
                    PointD edge2 = path[nextIndex];

                    if (PolygonTools.IsPointOnEdge(ToVector2(point), ToVector2(edge1), ToVector2(edge2), tolerance))
                    {
                        path.Insert(nextIndex, point);
                        break;
                    }
                }
            }


            bool DistanceWithinTolerance(PointD a, PointD b)
            {
                return Vector2.Distance(ToVector2(a), ToVector2(b)) <= tolerance;
            }

            Vector2 ToVector2(PointD point) => new((float)point.x, (float)point.y);
        }
        
        #endregion
    }
}
