using System;
using System.Linq;
using Clipper2Lib;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonWithHoles : Polygon
    {
        // Returns vertices of the outline of the shape in CCW order
        public Vector2[] Contour => Paths[0].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
        
        ///Returns vertices of a hole in CW order
        public Vector2[] Hole(int index) => Paths[index+1].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
        
        public int NumHoles => Mathf.Max(0, Paths.Count - 1);
        
        internal PolygonWithHoles(PathsD paths)
        {
            
            int numOutlines = paths.Count(Clipper.IsPositive);
            int numHoles = paths.Count(path => !Clipper.IsPositive(path));
            
            if (numOutlines != 1 || numHoles < 1)
            {
                throw new ArgumentException("PolygonWithHoles has " + numOutlines + " outlines and " + numHoles + " holes.");
            }
            
            ArrangePathsContourFirst(paths);
         }
        
        // Is this necessary or is contour always first?
        void ArrangePathsContourFirst(PathsD paths)
        {
            int contourIndex = paths.FindIndex(Clipper.IsPositive);
            if (contourIndex != 0)
            {
                (paths[contourIndex], paths[0]) = (paths[0], paths[contourIndex]);
            }
        }
    }
}