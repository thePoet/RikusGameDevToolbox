using RBush;    
    
namespace RikusGameDevToolbox.Geometry2d.Internal
{
    internal abstract class Path : ISpatialData
    {
        public PlanarDivision.HalfEdge HalfEdge; // Random half edge on the path
        public Envelope Envelope { get; set; }
    }

    internal class Face : Path
    {
        public FaceId Id;
    }

    internal class OutsideFace : Path
    {

    }
}