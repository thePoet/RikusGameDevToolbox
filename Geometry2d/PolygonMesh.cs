using System;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonMesh
    {
        public List<SimplePolygon> Polygons { get; } = new();
        private Dictionary<SimplePolygon, List<SimplePolygon>> _neighbours = new();
        
        public List<SimplePolygon> NeighboursOf(SimplePolygon simplePolygon)
        {
            return _neighbours[simplePolygon];
        }
 
        public void AddPolygon(SimplePolygon simplePolygon)
        {
            Polygons.Add(simplePolygon);
            _neighbours[simplePolygon] = new List<SimplePolygon>();
        }
        
        public void RemovePolygon(SimplePolygon simplePolygon)
        {
            while (NeighboursOf(simplePolygon).Count > 0)
            {
                MarkAsNotNeighbours(simplePolygon, NeighboursOf(simplePolygon)[0]);
            }
            _neighbours.Remove(simplePolygon);
            Polygons.Remove(simplePolygon);
        }
        
        public void MarkAsNeighbours(SimplePolygon polygon1, SimplePolygon polygon2)
        {
            _neighbours[polygon1].Add(polygon2);
            _neighbours[polygon2].Add(polygon1);
        }
        
        public void MarkAsNotNeighbours(SimplePolygon polygon1, SimplePolygon polygon2)
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