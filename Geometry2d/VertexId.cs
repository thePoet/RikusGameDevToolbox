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

        public bool Equals(VertexId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is VertexId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}