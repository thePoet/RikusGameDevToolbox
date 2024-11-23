using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.Geometry2d.DelaunayVoronoi;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class Voronoi
    {
        private List<VoronoiCell> _cells;
        private List<Edge> _edges;
        
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        public Voronoi(IEnumerable<Vector2> points, Rect bounds, bool useCentroids=false)
        {
            
            var cellCenters = points.Select(p => new Point(p.x, p.y));

            var dt = new DelaunayTriangulator();
            var triangles = new List<Triangle>(dt.BowyerWatson(cellCenters));

            GenerateFromDelaunay(triangles, useCentroids);

          
        }
/*
        private VoronoiCell CreateCell(Point centerPoint)
        {
            var points = new List<Vector2>();
            foreach (var triangle in centerPoint.AdjacentTriangles)
            {
                //triangle.
            }
       
        }*/

        public List<VoronoiCell> Cells()
        {
            return _cells;
        } 
        
        public List<Edge> Edges()
        {
            return _edges;
        } 
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void GenerateFromDelaunay(IEnumerable<Triangle> triangulation, bool useCentroids)
        {
            var voronoiEdges = new HashSet<Edge>();
            
            var cellVertices = new Dictionary<Vector2, HashSet<Vector2>>();
            
            foreach (var triangle in triangulation)
            {
                var tct = TriangleCenter(triangle);
                foreach (Triangle neighbour in triangle.TrianglesWithSharedEdge)
                {
                    var tcn = TriangleCenter(neighbour);
                    voronoiEdges.Add(new Edge(tct, tcn));
                }
                AddCellVertex(triangle.Vert(0), tct);
                AddCellVertex(triangle.Vert(1), tct);
                AddCellVertex(triangle.Vert(2), tct);
                
            }
            _edges = voronoiEdges.ToList();

            _cells = new List<VoronoiCell>();
            foreach (KeyValuePair<Vector2,HashSet<Vector2>> kvp in cellVertices)
            {
                List<Vector2> vertices = kvp.Value.ToList();
                Polygon2D poly = Polygon2D.FromUnorderedPoints(vertices);
                Vector2 center = kvp.Key;
                _cells.Add(new VoronoiCell(poly, center));
            }
            Vector2 TriangleCenter(Triangle t)
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