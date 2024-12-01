using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Clipper2Lib;

namespace RikusGameDevToolbox.Geometry2d
{
    public static class PolygonBoolean
    {
        /// <summary>
        /// Returns the intersection (AND) of two polygons i.e. the area that is common to both polygons.
        /// The second polygon must be convex.
        /// </summary>
        /// <param name="poly1">Any polygon</param>
        /// <param name="poly2">Any convex polygon</param>
        public static Polygon? Intersection(Polygon poly1, Polygon poly2)
        {
          var pathsA = PolygonToPathsD(poly1);
          var pathsB = PolygonToPathsD(poly2);
          PathsD intersection = Clipper.Intersect(pathsA, pathsB, FillRule.EvenOdd, 6);
          
            if (intersection.Count == 0) return null;
            return PathToPolygon(intersection[0]);
        }

        private static Polygon? PathToPolygon(PathD pathD)
        {
            if (pathD.Count < 3) return null;
            var points = pathD.Select(p => new Vector2((float)p.x, (float)p.y));
            return new Polygon(points.Reverse());
        }


        private static PathsD PolygonToPathsD(Polygon poly)
        {
            PathD path = new PathD();
            foreach (var point in poly.Points)
            {
                path.Add(new PointD(point.x, point.y));
            }

            var result = new PathsD();
            result.Add(path);
            return result;
        }



       
        
        
        
    }
    

}
    