using System.Linq;
using Clipper2Lib;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    /// <summary>
    /// A 2d shape with single outline. Superclass for SimplePolygon and PolygonWithHoles.
    /// </summary>
    public abstract class Polygon
    {
        internal PathsD Paths;

        
        
        /// Points in the outline of the polygon in CCW order.
        public Vector2[] Contour => Paths[0].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
     
        public float Area => (float)Clipper.Area(Paths);
        
         

        
    }
}