
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.Geometry2d.DelaunayVoronoi;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class Voronoi
    {
        public List<VoronoiCell> Cells => _cells;
        public List<Edge> VoronoiEdges => _voronoiEdges;
        public List<Triangle> DelaunayTriangles => _delaunayTriangles;

        
        private List<VoronoiCell> _cells;
        private List<Edge> _voronoiEdges;
        private List<Triangle> _delaunayTriangles;
        private Rect _bounds;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        public Voronoi(IEnumerable<Vector2> points, bool useCentroids=false)
        {
            _bounds = new Rect();
            _bounds.Bound(points);
            
            var cellCenters = points.Select(p => new Point(p.x, p.y)).ToList();

            var dt = new DelaunayTriangulator();
            var triangles = new List<DelaunayVoronoi.Triangle>(dt.BowyerWatson(cellCenters));
            
            GenerateFromDelaunay(triangles, useCentroids);

            _delaunayTriangles = new List<Triangle>();
            foreach (var t in triangles)
            {
                if (!t.IsBorder) _delaunayTriangles.Add(new Triangle(t.Vertices[0].AsVector2, t.Vertices[1].AsVector2, t.Vertices[2].AsVector2));
            }
            
        }


        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void GenerateFromDelaunay(IEnumerable<DelaunayVoronoi.Triangle> triangulation, bool useCentroids)
        {
            var voronoiEdges = new HashSet<Edge>();
            
            var cellVertices = new Dictionary<Vector2, HashSet<Vector2>>();
            
            foreach (var triangle in triangulation)
            {
               // if (triangle.IsBorder) continue;
                
                var tct = TriangleCenter(triangle);
                foreach (DelaunayVoronoi.Triangle neighbour in triangle.TrianglesWithSharedEdge)
                {
                    var tcn = TriangleCenter(neighbour);
                    voronoiEdges.Add(new Edge(tct, tcn));
                }
                AddCellVertex(triangle.Vert(0), tct);
                AddCellVertex(triangle.Vert(1), tct);
                AddCellVertex(triangle.Vert(2), tct);
                
            }
            _voronoiEdges = voronoiEdges.ToList();

            _cells = new List<VoronoiCell>();
            
          
            foreach (KeyValuePair<Vector2,HashSet<Vector2>> kvp in cellVertices)
            {
                List<Vector2> vertices = kvp.Value.ToList();
                bool isBorderCell =  (vertices.Any(v => !_bounds.Contains(v)));
                Polygon2D poly = Polygon2D.FromUnorderedPoints(vertices);
                Vector2 center = kvp.Key;
                _cells.Add(new VoronoiCell(poly, center, isBorderCell));
            }
            Vector2 TriangleCenter(DelaunayVoronoi.Triangle t)
            {
                if (useCentroids) return t.Centroid();
                return (t.Circumcenter.AsVector2);
            }
            
            void AddCellVertex(Vector2 center, Vector2 vertex)
            {
                if (!cellVertices.ContainsKey(center))
                {
                    cellVertices.Add(center, new HashSet<Vector2>());
                }
                cellVertices[center].Add(vertex);
            }
        }
        
        
        
        #endregion

       
    }
}