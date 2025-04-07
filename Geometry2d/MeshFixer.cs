using System;
using System.Collections.Generic;



namespace RikusGameDevToolbox.Geometry2d
{
    //This class is a quick and dirty fix to PolygonMeshes with intersecting polygons. It removes the offending polygons.
    public static class MeshFixer
    {
        public static List<Guid> RemoveIntersectingPolygons(PolygonMesh mesh)
        {
            int numPolygonRemoved = 0;

            var polygonToBeRemoved = FindPolygonsToBeRemoved(mesh);

            foreach (var polygonId in polygonToBeRemoved)
            {
                mesh.RemovePolygon(polygonId);
            }

            return polygonToBeRemoved;
        }

        private static List<Guid> FindPolygonsToBeRemoved(PolygonMesh mesh)
        {
            List<Guid> result = new List<Guid>();

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
            
            Guid SmallerPolygon(Guid a, Guid b) => mesh.Polygon(a).Area < mesh.Polygon(b).Area ? a : b;
            
        }

       
        
        
    }
}
