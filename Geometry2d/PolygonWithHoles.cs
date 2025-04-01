using System;
using System.Linq;
using Clipper2Lib;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    //TODO: Remove this class, move functionality to Polygon
    public class PolygonWithHoles : Polygon
    {
        
        internal PolygonWithHoles(PathsD paths)
        {
            int numOutlines = paths.Count(Clipper.IsPositive);
            int numHoles = paths.Count(path => !Clipper.IsPositive(path));
            
            if (numOutlines != 1 || numHoles < 1)
            {
                throw new ArgumentException("PolygonWithHoles has " + numOutlines + " outlines and " + numHoles + " holes.");
            }
            
            //NOTE: We don't check if the holes are inside the outline
            
            ArrangePathsContourFirst(paths);   // Is this necessary or is contour always first?
            Paths = paths;
            
       
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
}