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
        public DelaunayEdge[] Edges => new[] { new DelaunayEdge(Vertex1, Vertex2), new DelaunayEdge(Vertex2, Vertex3), new DelaunayEdge(Vertex3, Vertex1) };
        public Vector2 Centroid => (Vertex1 + Vertex2 + Vertex3) / 3f;
        public Vector2 Circumcenter => CalculateCircumcenter();

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
        
        private Vector2 CalculateCircumcenter()
        {
            float x1 = Vertex1.x;
            float x2 = Vertex2.x;
            float x3 = Vertex3.x;
            float y1 = Vertex1.y;
            float y2 = Vertex2.y;
            float y3 = Vertex3.y;

            float D = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));
            float Ux = ((x1 * x1 + y1 * y1) * (y2 - y3) + (x2 * x2 + y2 * y2) * (y3 - y1) + (x3 * x3 + y3 * y3) * (y1 - y2)) / D;
            float Uy = ((x1 * x1 + y1 * y1) * (x3 - x2) + (x2 * x2 + y2 * y2) * (x1 - x3) + (x3 * x3 + y3 * y3) * (x2 - x1)) / D;

            return new Vector2(Ux, Uy);
        }
    }
}