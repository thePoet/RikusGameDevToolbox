using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct Edge : IEquatable<Edge>
    {
        public Vector2 Point1;
        public Vector2 Point2;
        
        public Edge(Vector2 a, Vector2 b)
        {
            (Point1, Point2) = (a, b);
            
            // Points need to be ordered in predictable way for equality check and
            // hashcode generation to work correctly.
            if (a.x < b.x || Mathf.Approximately(a.x, b.x) && a.y < b.y)
            {
                (Point1, Point2) = (b, a);
            }
        }
        
        public float Length => Vector2.Distance(Point1, Point2);
        
        
        public bool Equals(Edge e) => Point1 == e.Point1 && Point2 == e.Point2;
        public override bool Equals(object obj) => obj is Edge edge && Equals(edge);
        public static bool operator == (Edge e1, Edge e2) => e1.Equals(e2);
        public static bool operator != (Edge e1, Edge e2) => !e1.Equals(e2);
        public override int GetHashCode() => HashCode.Combine(Point1, Point2);
    }
    
    

}