using System;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonMesh
    {
        public List<Polygon> Polygons { get; } = new();
        private Dictionary<Polygon, List<Polygon>> _neighbours = new();
        
        public List<Polygon> NeighboursOf(Polygon polygon)
        {
            return _neighbours[polygon];
        }
 
        public void AddPolygon(Polygon polygon)
        {
            Polygons.Add(polygon);
            _neighbours[polygon] = new List<Polygon>();
        }
        
        public void RemovePolygon(Polygon polygon)
        {
            while (NeighboursOf(polygon).Count > 0)
            {
                MarkAsNotNeighbours(polygon, NeighboursOf(polygon)[0]);
            }
            _neighbours.Remove(polygon);
            Polygons.Remove(polygon);
        }
        
        public void MarkAsNeighbours(Polygon polygon1, Polygon polygon2)
        {
            _neighbours[polygon1].Add(polygon2);
            _neighbours[polygon2].Add(polygon1);
        }
        
        public void MarkAsNotNeighbours(Polygon polygon1, Polygon polygon2)
        {
            _neighbours[polygon1].Remove(polygon2);
            _neighbours[polygon2].Remove(polygon1);
        }
        
        /// <summary>
        /// Call this in OnDrawGizmos() to visualize the mesh for debugging purposes.
        /// </summary>
        public void DrawWithGizmos()
        {
            foreach (var polygon in Polygons)
            {
                Gizmos.DrawSphere(polygon.Centroid(),0.3f);
                Gizmos.color = Color.yellow;
                foreach (var edge in polygon.Edges())
                {
                    Gizmos.DrawLine(edge.Point1, edge.Point2);
                }

                Gizmos.color = Color.blue;
                foreach (var neighbour in NeighboursOf(polygon))
                {
                    Gizmos.DrawLine(polygon.Centroid(), polygon.Centroid() + (neighbour.Centroid() - polygon.Centroid()).normalized );
                }
            }
        }
    }
}