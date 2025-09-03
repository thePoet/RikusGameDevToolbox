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


        public static PlanarDivision Create(IEnumerable<Vector2> points, Rect size, int relaxIterations = 0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, size, relaxIterations);
            return DivisionFromVoronoiEdges(edges);
        }   
        
        public static PlanarDivision CreateInPolygon(Polygon polygon, IEnumerable<Vector2> points,  int relaxIterations = 0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, polygon.Bounds(), relaxIterations);
            return DivisionFromVoronoiEdges(edges, polygon);
        }
        public static PlanarGraph AsPlanarGraph(Polygon polygon, IEnumerable<Vector2> points,  int relaxIterations = 0)
        {
            List<VoronoiEdge> edges = CreateVoronoiEdges(points, polygon.Bounds(), relaxIterations);
            return PlanarGraphFromVoronoiEdges(edges, polygon);
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
                        if (!cookieCutter.IsPointInside(middle)) division.DeleteEdge(v1, v2);
                    }

                    // Delete vertices outside the polygon
                    foreach (Vector2 v in edgeVertices)
                    {
                        if (!cookieCutter.IsPointInside(v) && !cookieCutter.IsPointOnEdge(v))  division.DeleteVertex(v);
                    }
                }
                return division;
        }
        
        private static PlanarGraph PlanarGraphFromVoronoiEdges(List<VoronoiEdge> voronoiEdges,  Polygon cookieCutter=null)
        {
            // TODO: This should be done without fixed epsilon.
            PlanarGraph graph = new(0.015f);

            if (cookieCutter != null)
            {
                foreach ((Vector2 v1, Vector2 v2) edge in cookieCutter.Edges())
                {
                    graph.AddLine(edge.v1, edge.v2);
                }
            }

            foreach (var edge in voronoiEdges)
            {
                (Vector2 start, Vector2 end) = (edge.Start.AsVector2(), edge.End.AsVector2());
                var edgeVertices = graph.AddLine(start, end);

                if (cookieCutter == null) continue;

                // Delete edges that go across space outside the polygon 
                foreach (var (v1, v2) in Pairs(edgeVertices))
                {
                    Vector2 middle = (graph.Position(v1) + graph.Position((v2))) / 2f;
                    if (!cookieCutter.IsPointInside(middle)) graph.DeleteEdge(v1, v2);
                }

                // Delete vertices outside the polygon
                foreach (var v in edgeVertices)
                {
                    var vPos = graph.Position(v);
                    if (!cookieCutter.IsPointInside(vPos) && !cookieCutter.IsPointOnEdge(vPos))  graph.DeleteVertex(v);
                }
            }
            return graph;

        }

        
       
    }
}