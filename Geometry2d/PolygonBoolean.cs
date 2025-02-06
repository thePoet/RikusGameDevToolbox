using System.Collections.Generic;
using Clipper2Lib;


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
            clipper.AddSubject(a.Paths);
            clipper.AddSubject(b.Paths);
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
                clipper.AddSubject(polygon.Paths);
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
            clipper.AddSubject(a.Paths);
            clipper.AddClip(b.Paths);
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
            clipper.AddSubject(poly1.Paths);
            clipper.AddClip(poly2.Paths);
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
                clipper.AddSubject(polygon.Paths);
            }
            foreach (var polygon in poly2)
            {
                clipper.AddClip(polygon.Paths);
            }
            clipper.Execute(ClipType.Difference, FillRule.NonZero, polytree);
            return PolygonTools.ToPolygons(polytree);
        }

        #endregion
    }
}
