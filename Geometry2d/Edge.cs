using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct Edge : IEquatable<Edge>
    {
        public Vector2 Point1;
        public Vector2 Point2;
        
        public Edge(Vector2 point1, Vector2 point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
        
        
        public bool Equals(Edge e)
        {
            return Point1 == e.Point1 && Point2 == e.Point2 || 
                   Point1 == e.Point2 && Point2 == e.Point1;
        }
        
        public override bool Equals(object obj) => obj is Edge edge && Equals(edge);
        public static bool operator == (Edge e1, Edge e2) => e1.Equals(e2);
        public static bool operator != (Edge e1, Edge e2) => !e1.Equals(e2);
        public override int GetHashCode() => HashCode.Combine(Point1, Point2);
    }
    
    

}