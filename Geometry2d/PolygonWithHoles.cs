using System;
using System.Linq;
using Clipper2Lib;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonWithHoles : Polygon
    {
        ///Returns vertices of a hole in CW order
       // public Vector2[] Hole(int index) => Paths[index+1].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
        
       // public SimplePolygon Hole(int index) => new SimplePolygon(Paths[index+1]);
        
        public int NumHoles => Mathf.Max(0, Paths.Count - 1);

        public SimplePolygon Hole(int holeIndex)
        {
            if (holeIndex < 0 || holeIndex >= NumHoles) return null;
            PathD path = new PathD(Paths[holeIndex + 1]);
            path.Reverse();
            return new SimplePolygon(path);
        }
        
        internal PolygonWithHoles(PathsD paths)
        {
            int numOutlines = paths.Count(Clipper.IsPositive);
            int numHoles = paths.Count(path => !Clipper.IsPositive(path));
            
            if (numOutlines != 1 || numHoles < 1)
            {
                throw new ArgumentException("PolygonWithHoles has " + numOutlines + " outlines and " + numHoles + " holes.");
            }
            
            //NOTE: We don't check if the holes are inside the outline
            
            ArrangePathsContourFirst(paths);
            Paths = paths;
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