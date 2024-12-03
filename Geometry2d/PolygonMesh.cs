using System.Collections.Generic;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonInMesh
    {
        Polygon[] Polygons { get; }

        IEnumerable<int> Neighbours(int polygonIdx)
        {
            return null;
        }
        
        public bool IsOnBorder(int polygonIdx)
    }
}