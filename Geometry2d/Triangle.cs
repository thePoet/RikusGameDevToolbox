using System;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct Triangle : IEquatable<Triangle>
    {
        public Vector2 V1 { get; }
        public Vector2 V2 { get; }
        public Vector2 V3 { get; }

        public Vector2[] Vertices => new[] { V1, V2, V3 };

        public Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }
   
        public bool Equals(Triangle other)
        {
            return ( V1 == other.V1 || V1 == other.V2 || V1 == other.V3 ) &&
                   ( V2 == other.V1 || V2 == other.V2 || V2 == other.V3 ) &&
                   ( V3 == other.V1 || V3 == other.V2 || V3 == other.V3 );
        }


        public override bool Equals(object obj) => obj is Triangle triangle && Equals(triangle);
        public static bool operator ==(Triangle t1, Triangle t2) => t1.Equals(t2);
        public static bool operator !=(Triangle t1, Triangle t2) => !t1.Equals(t2);

        public override int GetHashCode()
        {
            return HashCode.Combine(V1, V2, V3);
        }
        
        public override string ToString()
        {
            return "Triangle: " + V1 + ", " + V2 + ", " + V3;
        }
    }
}