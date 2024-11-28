using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.Geometry2d.DelaunayVoronoi;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace RikusGameDevToolbox.Geometry2d
{
    public class DelaunayTriangulation
    {
        public List<Triangle> Triangles => _triangles;
        public List<Edge> Edges => _edges;
        public List<Vector2> Points => _points;

        private List<Triangle> _triangles;
        private List<Edge> _edges;
        private List<Vector2> _points;
        private Dictionary<Triangle, List<Triangle>> _triangleNeighbors;
        
        public DelaunayTriangulation(IEnumerable<Vector2> vertices)
        {
            _points = vertices.ToList();
            var points = _points.Select(p => new Point(p.x, p.y)).ToList();
            var dt = new DelaunayTriangulator();
            var trianglesInternal = new List<DelaunayVoronoi.Triangle>(dt.BowyerWatson(points));

            _triangles = CreataTriangles(trianglesInternal);
            _edges = CreateEdges(_triangles);
            _triangleNeighbors = CreateNeighbourLookup(trianglesInternal);
        }
        
        private List<Triangle> CreataTriangles(List<DelaunayVoronoi.Triangle> trianglesInternal)
        {
            var triangles = new List<Triangle>();
            foreach (var t in trianglesInternal)
            {
                var triangle = ConvertTriangle(t);
                triangles.Add(triangle);
            }
            return triangles;
        }
        
        private List<Edge> CreateEdges(List<Triangle> triangles)
        {
            var edges = new HashSet<Edge>();
            foreach (var triangle in triangles)
            {
                foreach (var edge in triangle.Edges)
                {
                    edges.Add(edge);
                } 
            }
            return edges.ToList();
        }
        
        public List<Triangle> TrianglesWithSharedEdge(Triangle triangle)
        {
            return _triangleNeighbors[triangle];
        }
        
        public bool IsBorderTriangle(Triangle triangle)
        {
            return _triangleNeighbors[triangle].Count < 3;
        }
        
        private Dictionary<Triangle, List<Triangle>> CreateNeighbourLookup(List<DelaunayVoronoi.Triangle> trianglesInternal)
        {
            _triangleNeighbors = new Dictionary<Triangle, List<Triangle>>();
            
            foreach (var t in trianglesInternal)
            { 
                var neighbours =  new List<Triangle>();
            
                foreach (var neighbour in t.TrianglesWithSharedEdge)
                {
                    var nt = ConvertTriangle(neighbour);
                    if (_triangles.Contains(nt))
                    {
                        neighbours.Add(nt);
                    }
                }
                _triangleNeighbors.Add(ConvertTriangle(t), neighbours);
            }

            return _triangleNeighbors;
        }


 
    

    
        
        private Triangle ConvertTriangle(DelaunayVoronoi.Triangle triangle)
        {
            return new Triangle(triangle.Vertices[0].AsVector2, triangle.Vertices[1].AsVector2, triangle.Vertices[2].AsVector2);
        }
    }
}