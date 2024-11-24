using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct VoronoiCell : IEquatable<VoronoiCell>
    {
        public Polygon2D Polygon { get; }
        public Vector2 Center { get; }
        public bool IsBorderCell { get; }
        
        public VoronoiCell(Polygon2D polygon, Vector2 center, bool isBorderCell)
        {
            Polygon = polygon;
            Center = center;
            IsBorderCell = isBorderCell;
        }
        public bool Equals(VoronoiCell other) => Polygon.Equals(other.Polygon) && Center == other.Center;
        public override bool Equals(object obj) => obj is VoronoiCell vc && Equals(vc);
        public override int GetHashCode() => HashCode.Combine(Polygon, Center);
    }
}