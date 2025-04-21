using System;
using System.Collections.Generic;



namespace RikusGameDevToolbox.Geometry2d
{
    //This class is a quick and dirty fix to PolygonMeshes with intersecting polygons. It removes the offending polygons.
    public static class MeshFixer
    {
        public static List<PolygonId> RemoveIntersectingPolygons(PolygonMesh mesh)
        {
            int numPolygonRemoved = 0;

            var polygonToBeRemoved = FindPolygonsToBeRemoved(mesh);

            foreach (var polygonId in polygonToBeRemoved)
            {
                mesh.RemovePolygon(polygonId);
            }

            return polygonToBeRemoved;
        }

        private static List<PolygonId> FindPolygonsToBeRemoved(PolygonMesh mesh)
        {
            List<PolygonId> result = new List<PolygonId>();

            foreach (var (id, polygon) in mesh.Polygons())
            {
                foreach (var neighbourId in mesh.Neighbours(id))
                {
                    if (neighbourId.GetHashCode() > id.GetHashCode()) continue;
                    if (result.Contains(id) || result.Contains(neighbourId)) continue;
                    if (mesh.AreIntersecting(id, neighbourId))
                    {
                        result.Add( SmallerPolygon(id, neighbourId) );
                    }
                }
            }

            return result;
            
            PolygonId SmallerPolygon(PolygonId a, PolygonId b) => mesh.Polygon(a).Area < mesh.Polygon(b).Area ? a : b;
            
        }

       
        
        
    }
}
