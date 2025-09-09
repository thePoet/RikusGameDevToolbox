using System.Collections.Generic;
using System.Linq;
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
                .Where(f =>  IsPathInsideFace(f, face))
                .ToList();
          
            // Remove holes that are inside other holes
            var copy = new List<OutsideFace>(holes);
            foreach (OutsideFace f in copy)
            {
                holes.RemoveAll(hole => hole != f && IsPathInsideOutsideFace(hole, f));
            }

            return holes;
        }
        
        /// <summary>
        ///  Returns the smallest normal face that contains the outside face, or null if it is not contained in any face.
        /// </summary>
        public Face FaceContaining(OutsideFace hole)
        {
            Vector2 pointOnFace = _faceVertexPositions(hole.HalfEdge).FirstOrDefault();
            var candidates = _paths.Search(pointOnFace);
            
            return _paths.Search(pointOnFace)
                .OfType<Face>()
                .Where(face => IsPathInsideFace(hole, face))
                .OrderBy(face => face.Envelope.Area)
                .FirstOrDefault();
        }
        

        //TODO: somewhat slow:
        private bool IsPathInsideFace(Path path, Face face)
        {
            SimplePolygon facePolygon = new SimplePolygon(_faceVertexPositions(face.HalfEdge).ToArray());
            Vector2 pointOnPath = _faceVertexPositions(path.HalfEdge).First();
            return facePolygon.IsPointInside(pointOnPath);
        }
        //TODO: somewhat slow:
        private bool IsPathInsideOutsideFace(Path path, OutsideFace outsideFace)
        {
            SimplePolygon facePolygon = new SimplePolygon(_faceVertexPositions(outsideFace.HalfEdge).Reverse().ToArray());
            Vector2 pointOnPath = _faceVertexPositions(path.HalfEdge).First();
            return facePolygon.IsPointInside(pointOnPath);
        }
 
        


    }
}
