using System;

namespace RikusGameDevToolbox.Geometry2d
{
    public struct VertexId : IEquatable<VertexId>
    {
        public Guid Value { get; init; }
        public static VertexId New() => new(Guid.NewGuid());

        private VertexId(Guid value) 
        {
            Value = value;
        }

        public bool Equals(VertexId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is VertexId other && Equals(other);
        public static bool operator == (VertexId a, VertexId b) =>  a.Equals(b);
        public static bool operator != (VertexId a, VertexId b) =>  !a.Equals(b);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }
}