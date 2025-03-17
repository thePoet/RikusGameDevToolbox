using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SharpVoronoiLib;


namespace RikusGameDevToolbox.Geometry2d
{
    public static class VoronoiTiling
    {
    
        
        private static Vector2 AsVector2(this VoronoiPoint point) => new Vector2((float)point.X, (float)point.Y);

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

      
           return PolygonMeshFromEdges(edges);
        }
        
        public static PolygonMesh2 Create2(IEnumerable<Vector2> points, Rect size, float minEdgeLength)
        {
            VoronoiPlane voronoi = new VoronoiPlane(size.min.x, size.min.y, size.max.x, size.max.y);
            List<VoronoiSite> sites = points.Select(p => new VoronoiSite(p.x, p.y)).ToList();
            voronoi.SetSites(sites);
            voronoi.Tessellate();
            //voronoi.Relax(1);
            List<VoronoiEdge> edges = voronoi.Edges; 
            return PolygonMesh2FromEdges(edges, minEdgeLength);
        }
        
        /// <summary>
        /// Generates a PolygonMesh from list of VoronoiEdges produced with SharpVoronoiLib
        /// </summary>
        private static PolygonMesh PolygonMeshFromEdges(List<VoronoiEdge> edges)
        {
            Dictionary<VoronoiSite, SimplePolygon> polygons = new();
            List<(VoronoiSite, VoronoiSite)> neighbours = new();
            
            foreach (var edge in edges)
            {
                AddSite(edge.Left);
                AddSite(edge.Right);
                if (edge.Left == null || edge.Right == null) continue;
                neighbours.Add((edge.Left, edge.Right));
            }

            var mesh = new PolygonMesh();
            foreach (SimplePolygon polygon in polygons.Values)
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

            SimplePolygon ConvertToPolygon(VoronoiSite site)
            {
                var vertices = site.Points.Select( p => p.AsVector2() );
                return PolygonTools.CreateFromUnorderedPoints(vertices);
            }
            
        }
        
        /// <summary>
        /// Generates a PolygonMesh from list of VoronoiEdges produced with SharpVoronoiLib
        /// </summary>
        private static PolygonMesh2 PolygonMesh2FromEdges(List<VoronoiEdge> edges, float minEdgeLength)
        {
            Dictionary<VoronoiSite, SimplePolygon> polygons = new();
         //   List<(VoronoiSite, VoronoiSite)> neighbours = new();
            
            foreach (var edge in edges)
            {
                AddSite(edge.Left);
                AddSite(edge.Right);
                if (edge.Left == null || edge.Right == null) continue;
           //     neighbours.Add((edge.Left, edge.Right));
            }

            var mesh = new PolygonMesh2(minEdgeLength);
            foreach (SimplePolygon polygon in polygons.Values)
            {
                try
                {
                    mesh.AddPolygon(polygon);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
/*
            foreach (var (siteA, siteB) in neighbours)
            {
                mesh.MarkAsNeighbours(polygons[siteA], polygons[siteB]);
            }*/
            
            return mesh;

            void AddSite(VoronoiSite site)
            {
                if (site==null || polygons.ContainsKey(site)) return;
                polygons.Add(site, ConvertToPolygon(site));
            }

            SimplePolygon ConvertToPolygon(VoronoiSite site)
            {
                List<Vector2> vertices = site.ClockwisePoints.Select( p => p.AsVector2() ).Reverse().ToList();
                return new SimplePolygon(vertices);
            //    var vertices = site.Points.Select( p => p.AsVector2() );
              //  return PolygonTools.CreateFromUnorderedPoints(vertices);
            }
            
        }
       
    }
}