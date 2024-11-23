using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct VoronoiCell : IEquatable<VoronoiCell>
    {
        public Polygon2D Polygon { get; }
        public Vector2 Point { get; }
        
        public VoronoiCell(Polygon2D polygon, Vector2 point)
        {
            Polygon = polygon;
            Point = point;
        }

        public bool Equals(VoronoiCell other) => Polygon.Equals(other.Polygon) && Point == other.Point;
        public override bool Equals(object obj) => obj is VoronoiCell vc && Equals(vc);
        public override int GetHashCode() => HashCode.Combine(Polygon, Point);
    }
}