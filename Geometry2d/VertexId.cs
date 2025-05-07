using System;

namespace RikusGameDevToolbox.Geometry2d
{
    public record VertexId(Guid Value)
    {
        public static VertexId New() => new(Guid.NewGuid());
    }
}