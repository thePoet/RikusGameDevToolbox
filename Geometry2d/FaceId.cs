using System;

namespace RikusGameDevToolbox.Geometry2d
{
    public record FaceId(Guid Value)
    {
        public static FaceId New() => new(Guid.NewGuid());
        public static FaceId Empty => new(Guid.Empty); 
    }
}