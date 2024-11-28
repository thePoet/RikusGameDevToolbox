
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class Voronoi
    {
        public List<VoronoiCell> Cells { get; }
        public List<Edge> VoronoiEdges { get; }

        private Rect _bounds;
        private readonly bool _useCentroids;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        public Voronoi(IEnumerable<Vector2> points, bool useCentroids = false)
        {
            _bounds = new Rect();
            _bounds.Bound(points);

            _useCentroids = useCentroids;
            var delaunay = new DelaunayTriangulation(points);
            (Cells, VoronoiEdges) = FromDelaunay(delaunay);
        }


        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private (List<VoronoiCell>, List<Edge>) FromDelaunay(DelaunayTriangulation dt)
        {
            var triangles = dt.Triangles;
            var voronoiEdges = new HashSet<Edge>();
            
            var cellVertices = new Dictionary<Vector2, HashSet<Vector2>>();
            
            foreach (var triangle in triangles)
            {
                var tct = TriangleCenter(triangle);
                foreach (Triangle neighbour in dt.TrianglesWithSharedEdge(triangle))
                {
                    var tcn = TriangleCenter(neighbour);
                    voronoiEdges.Add(new Edge(tct, tcn));
                }
                AddCellVertex(triangle.Vertex1, tct);
                AddCellVertex(triangle.Vertex2, tct);
                AddCellVertex(triangle.Vertex3, tct);
            }
       

            var cells = new List<VoronoiCell>();
            foreach (KeyValuePair<Vector2,HashSet<Vector2>> kvp in cellVertices)
            {
                List<Vector2> vertices = kvp.Value.ToList();
                bool isBorderCell = false;//(vertices.Any(v => !_bounds.Contains(v)));
                Polygon poly = Polygon.FromUnorderedPoints(vertices);
                if (!IsInsideBounds(poly)) continue;
                Vector2 center = kvp.Key;
                cells.Add(new VoronoiCell(poly, center, isBorderCell));
            }

            return (cells, voronoiEdges.ToList());
            
            void AddCellVertex(Vector2 center, Vector2 vertex)
            {
                if (!cellVertices.ContainsKey(center))
                {
                    cellVertices.Add(center, new HashSet<Vector2>());
                }
                cellVertices[center].Add(vertex);
            }
        }
        
        Vector2 TriangleCenter(Triangle t)
        {
            if (_useCentroids) return t.Centroid;
            return (t.Circumcenter);
        }
        
        bool IsInsideBounds(Polygon poly) => poly.Points.All(v => _bounds.Contains(v));
        
      
        
        #endregion

       
    }
}