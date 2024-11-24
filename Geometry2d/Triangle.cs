using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct Triangle : IEquatable<Triangle>
    {
        public Vector2 Vertex1 { get; }
        public Vector2 Vertex2 { get; }
        public Vector2 Vertex3 { get; }

        public Vector2[] Vertices => new[] { Vertex1, Vertex2, Vertex3 };
        public Edge[] Edges => new[] { new Edge(Vertex1, Vertex2), new Edge(Vertex2, Vertex3), new Edge(Vertex3, Vertex1) };

        public Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            if (v1 == v2 || v1 == v3 || v2 == v3)
            {
                throw new ArgumentException("Triangle vertices must be unique.");
            }

            (Vertex1, Vertex2, Vertex3) = (v1, v2, v3);
        }
   
        public bool Equals(Triangle other)
        {
            return ( Vertex1 == other.Vertex1 || Vertex1 == other.Vertex2 || Vertex1 == other.Vertex3 ) &&
                   ( Vertex2 == other.Vertex1 || Vertex2 == other.Vertex2 || Vertex2 == other.Vertex3 ) &&
                   ( Vertex3 == other.Vertex1 || Vertex3 == other.Vertex2 || Vertex3 == other.Vertex3 );
        }


        public override bool Equals(object obj) => obj is Triangle triangle && Equals(triangle);
        public static bool operator ==(Triangle t1, Triangle t2) => t1.Equals(t2);
        public static bool operator !=(Triangle t1, Triangle t2) => !t1.Equals(t2);

        public override int GetHashCode()
        {
            return HashCode.Combine(Vertex1, Vertex2, Vertex3);
        }
        
        public override string ToString()
        {
            return "Triangle: " + Vertex1 + ", " + Vertex2 + ", " + Vertex3;
        }
    }
}