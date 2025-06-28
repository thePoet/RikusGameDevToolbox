using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RBush;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d.Internal
{
    internal class PlanarDivisionHoles
    {
        public delegate IEnumerable<Vector2> FaceVertexPositions(PlanarDivision.HalfEdge halfEdge);
        
        private RBush<Path> _paths;
        private FaceVertexPositions _faceVertexPositions;
        
        
        
        public PlanarDivisionHoles(RBush<Path> paths, FaceVertexPositions faceVertexPositions)
        {
            _paths = paths;
            _faceVertexPositions = faceVertexPositions;
        }
        
        /// <summary>
        ///  Returns boundary faces inside the face. Note: only the first level of holes are returned, not holes inside the holes
        /// </summary>
        public List<OutsideFace> HolesOf(Face face)
        {
            var holes = _paths.Search(face.Envelope)
                .OfType<OutsideFace>()
                .Where(f =>  IsPathsInsideAnother(f, face))
                .ToList();
          
            // Remove holes that are inside other holes
            var copy = new List<Path>(holes);
            foreach (Path f in copy)
            {
                holes.RemoveAll(hole => hole != f && IsPathsInsideAnother(hole, f));
            }

            return holes;
        }
        
        /// <summary>
        ///  Returns the smallest normal face that contains the outside face, or null if it is not contained in any face.
        /// </summary>
        [CanBeNull]
        public Face FaceContaining(OutsideFace hole)
        {
            Vector2 pointOnFace = _faceVertexPositions(hole.HalfEdge).FirstOrDefault();
            return _paths.Search(pointOnFace)
                .OfType<Face>()
                .Where(face => IsPathsInsideAnother(hole, face))
                .OrderBy(face => face.Envelope.Area)
                .FirstOrDefault();
        }
        
    
        
        private bool IsPathsInsideAnother(Path path, Path anotherPath)
        {
            //Assuming the edges do not cross, we can just check if all vertices of a are inside b
            return _faceVertexPositions(path.HalfEdge)  
                .All(vPos => IsPointInsidePath(vPos, anotherPath));
        }

        private bool IsPointInsidePath(Vector2 position, Path path)
        {
            var polygon = path is Face
                ? new SimplePolygon(_faceVertexPositions(path.HalfEdge)) 
                : new SimplePolygon(_faceVertexPositions(path.HalfEdge).Reverse());
            return polygon.IsPointInside(position);
        }
        


    }
}
