using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using Random = System.Random;

// Based on https://github.com/RafaelKuebler/DelaunayVoronoi

// No vertex lies inside the circumcircle of any triangle in the triangulation

namespace RikusGameDevToolbox.Geometry2d.DelaunayVoronoi
{
    public class DelaunayTriangulator
    {

        public IEnumerable<Triangle> BowyerWatson(IEnumerable<Vector2> points)
        {
            var internalPoints = (points.Select(p => new Point(p.x, p.y))).ToList();
            return BowyerWatson(internalPoints);
        }

        public IEnumerable<Triangle> BowyerWatson(List<Point> points)
        {
        
            
            var superTriangle = GenerateSuperTriangle(points);

            var triangulation = new HashSet<Triangle> { superTriangle };

            foreach (var point in points)
            {
                var badTriangles = FindBadTriangles(point, triangulation);
                var polygon = FindHoleBoundaries(badTriangles);

                foreach (var triangle in badTriangles)
                {
                    foreach (var vertex in triangle.Vertices)
                    {
                        vertex.AdjacentTriangles.Remove(triangle);
                    }
                }
                triangulation.RemoveWhere(o => badTriangles.Contains(o));

                foreach (var edge in polygon.Where(possibleEdge => possibleEdge.Point1 != point && possibleEdge.Point2 != point))
                {
                    var triangle = new Triangle(point, edge.Point1, edge.Point2);
                    triangulation.Add(triangle);
                }
            }

     
            
            triangulation.RemoveWhere(o => o.Vertices.Any(v => superTriangle.Vertices.Contains(v)));
            
          
            
            return triangulation;
        }

        private List<Edge> FindHoleBoundaries(ISet<Triangle> badTriangles)
        {
            var edges = new List<Edge>();
            foreach (var triangle in badTriangles)
            {
                edges.Add(new Edge(triangle.Vertices[0], triangle.Vertices[1]));
                edges.Add(new Edge(triangle.Vertices[1], triangle.Vertices[2]));
                edges.Add(new Edge(triangle.Vertices[2], triangle.Vertices[0]));
            }
            var grouped = edges.GroupBy(o => o);
            var boundaryEdges = edges.GroupBy(o => o).Where(o => o.Count() == 1).Select(o => o.First());
            return boundaryEdges.ToList();
        }

        private Triangle GenerateSuperTriangle(IEnumerable<Point> points)
        {
            Rect bounds = Bounds(points);
            bounds = bounds.Grow(bounds.size.magnitude*2f);
       
            var point1 = new Point(bounds.min.x, bounds.min.y);
            var point2 = new Point(bounds.max.x, bounds.min.y);
            var point3 = new Point((bounds.min.x+bounds.max.x)/2f, bounds.max.y);

            return new Triangle(point1, point2, point3);
        }
        
     
        
        private Rect Bounds(IEnumerable<Point> points)
        {
            Rect bounds = new Rect();
            bounds.Bound(points.Select(p => p.AsVector2));
            return bounds;
        }

        private ISet<Triangle> FindBadTriangles(Point point, HashSet<Triangle> triangles)
        {
            var badTriangles = triangles.Where(o => o.IsPointInsideCircumcircle(point));
            return new HashSet<Triangle>(badTriangles);
        }
    }
}