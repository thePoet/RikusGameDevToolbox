using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.Geometry2d.DelaunayVoronoi;
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
        
        public DelaunayTriangulation(IEnumerable<Vector2> vertices)
        {
            _points = vertices.ToList();
            var points = _points.Select(p => new Point(p.x, p.y)).ToList();
            var dt = new DelaunayTriangulator();
            var triangles = new List<DelaunayVoronoi.Triangle>(dt.BowyerWatson(points));

            _triangles = new List<Triangle>();

            var edges = new HashSet<Edge>();
            foreach (var t in triangles)
            {
                var triangle = new Triangle(t.Vertices[0].AsVector2, t.Vertices[1].AsVector2, t.Vertices[2].AsVector2);
                _triangles.Add(triangle);
                foreach (var edge in triangle.Edges)
                {
                    edges.Add(edge);
                }
            }
            _edges = edges.ToList();
        }
    }
}