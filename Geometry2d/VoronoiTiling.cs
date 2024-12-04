using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SharpVoronoiLib;


namespace RikusGameDevToolbox.Geometry2d
{
    public static class VoronoiTiling
    {
        private static List<VoronoiEdge> _edges;
        
        public static Vector2 AsVector2(this VoronoiPoint point) => new Vector2((float)point.X, (float)point.Y);

        public static PolygonMesh Create(IEnumerable<Vector2> points, Rect bounds)
        {
           List<VoronoiSite> sites = points.Select(p => new VoronoiSite(p.x, p.y)).ToList();
        
           List<VoronoiEdge> edges = VoronoiPlane.TessellateOnce
           (
               sites, 
               bounds.min.x,
               bounds.min.y,
               bounds.max.x,
               bounds.max.y
           );

           _edges = edges;
           return PolygonMeshFromEdges(edges);
        }
        
        /// <summary>
        /// Generates a PolygonMesh from list of VoronoiEdges produced with SharpVoronoiLib
        /// </summary>
        private static PolygonMesh PolygonMeshFromEdges(List<VoronoiEdge> edges)
        {
            Dictionary<VoronoiSite, Polygon> polygons = new();
            List<(VoronoiSite, VoronoiSite)> neighbours = new();
            
            foreach (var edge in edges)
            {
                AddSite(edge.Left);
                AddSite(edge.Right);
                if (edge.Left == null || edge.Right == null) continue;
                neighbours.Add((edge.Left, edge.Right));
            }

            var mesh = new PolygonMesh();
            foreach (Polygon polygon in polygons.Values)
            {
                mesh.AddPolygon(polygon);
            }

            foreach (var (siteA, siteB) in neighbours)
            {
                mesh.MarkAsNeighbours(polygons[siteA], polygons[siteB]);
            }
            
            return mesh;

            void AddSite(VoronoiSite site)
            {
                if (site==null || polygons.ContainsKey(site)) return;
                polygons.Add(site, ConvertToPolygon(site));
            }

            Polygon ConvertToPolygon(VoronoiSite site)
            {
                var vertices = site.Points.Select( p => p.AsVector2() );
                return Polygon.FromUnorderedPoints(vertices);
            }
            
        }
       
    }
}