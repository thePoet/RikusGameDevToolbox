using System.Collections.Generic;

namespace RikusGameDevToolbox.Geometry2d.DelaunayVoronoi
{
    public class Voronoi
    {
        public IEnumerable<Edge> GenerateEdgesFromDelaunay(IEnumerable<Triangle> triangulation)
        {
            var voronoiEdges = new HashSet<Edge>();
            foreach (var triangle in triangulation)
            {
                foreach (var neighbor in triangle.TrianglesWithSharedEdge)
                {
                    var edge = new Edge(triangle.Circumcenter, neighbor.Circumcenter);
                    voronoiEdges.Add(edge);
                }
            }

            return voronoiEdges;
        }
    }
}