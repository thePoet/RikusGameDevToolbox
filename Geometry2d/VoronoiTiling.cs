using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SharpVoronoiLib;
using static RikusGameDevToolbox.Geometry2d.Util;

namespace RikusGameDevToolbox.Geometry2d
{
    public static class VoronoiTiling
    {
    
        
        private static Vector2 AsVector2(this VoronoiPoint point) => new Vector2((float)point.X, (float)point.Y);


        public static PlanarDivision Create(IEnumerable<Vector2> points, Rect size, int relaxIterations=0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, size, relaxIterations);
            return DivisionFromVoronoiEdges(edges);
        }   
        
        public static PlanarDivision CreateInPolygon(Polygon polygon, IEnumerable<Vector2> points,  int relaxIterations=0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, polygon.Bounds(), relaxIterations);
            return DivisionFromVoronoiEdges(edges, polygon);
        }
        public static PlanarGraph CreateInPolygon2(Polygon polygon, IEnumerable<Vector2> points,  int relaxIterations=0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, polygon.Bounds(), relaxIterations);
            return DivisionFromVoronoiEdges2(edges, polygon);
        }

        public static PolygonMesh CreatePolygonMesh(IEnumerable<Vector2> points, Rect size, float minEdgeLength)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, size, 0);
            return PolygonMeshFromEdges(edges, minEdgeLength);
        }

        private static List<VoronoiEdge> CreateVoronoiEdges(IEnumerable<Vector2> points, Rect size, int relaxIterations)
        {
            VoronoiPlane voronoi = new VoronoiPlane(size.min.x, size.min.y, size.max.x, size.max.y);
            List<VoronoiSite> sites = points.Select(p => new VoronoiSite(p.x, p.y)).ToList();
            voronoi.SetSites(sites);
            voronoi.Tessellate();
            if (relaxIterations>0) voronoi.Relax(relaxIterations);
            List<VoronoiEdge> edges = voronoi.Edges;
            return edges;
        }

        

        private static PlanarDivision DivisionFromVoronoiEdges(List<VoronoiEdge> voronoiEdges,  Polygon cookieCutter=null)
        {
                // TODO: This should be done without fixed epsilon.
                PlanarDivision division = new(0.0001f);

                if (cookieCutter != null)
                {
                    foreach ((Vector2 v1, Vector2 v2) edge in cookieCutter.Edges())
                    {
                        division.AddLine(edge.v1, edge.v2);
                    }
                }

                foreach (var edge in voronoiEdges)
                {
                    (Vector2 start, Vector2 end) = (edge.Start.AsVector2(), edge.End.AsVector2());
                    var edgeVertices = division.AddLine(start, end);

                    if (cookieCutter == null) continue;

                    // Delete edges that go across space outside the polygon 
                    foreach (var (v1, v2) in Pairs(edgeVertices))
                    {
                        Vector2 middle = (v1 + v2) / 2f;
                        if (!cookieCutter.IsPointOn(middle)) division.DeleteEdge(v1, v2);
                    }

                    // Delete vertices outside the polygon
                    foreach (Vector2 v in edgeVertices)
                    {
                        if (!cookieCutter.IsPointOn(v)) division.DeleteVertex(v);
                    }
                }
                return division;
        }
        
        private static PlanarGraph DivisionFromVoronoiEdges2(List<VoronoiEdge> voronoiEdges,  Polygon cookieCutter=null)
        {
            // TODO: This should be done without fixed epsilon.
            PlanarGraph division = new(0.0001f);

            if (cookieCutter != null)
            {
                foreach ((Vector2 v1, Vector2 v2) edge in cookieCutter.Edges())
                {
                    division.AddLine(edge.v1, edge.v2);
                }
            }

            foreach (var edge in voronoiEdges)
            {
                (Vector2 start, Vector2 end) = (edge.Start.AsVector2(), edge.End.AsVector2());
                var edgeVertices = division.AddLine(start, end);

                if (cookieCutter == null) continue;

                // Delete edges that go across space outside the polygon 
                foreach (var (v1, v2) in Pairs(edgeVertices))
                {
                    Vector2 middle = (division.Position(v1) + division.Position((v2))) / 2f;
                    if (!cookieCutter.IsPointOn(middle)) division.DeleteEdge(v1, v2);
                }

                // Delete vertices outside the polygon
                foreach (var v in edgeVertices)
                {
                    if (!cookieCutter.IsPointOn(division.Position(v))) division.DeleteVertex(v);
                }
            }
            return division;
        }

        
        /// <summary>
        /// Generates a PolygonMesh from list of VoronoiEdges produced with SharpVoronoiLib
        /// </summary>
        private static PolygonMesh PolygonMeshFromEdges(List<VoronoiEdge> edges, float minEdgeLength)
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

            var mesh = new PolygonMesh();
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

            mesh.FuseVertices(minEdgeLength);
            
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