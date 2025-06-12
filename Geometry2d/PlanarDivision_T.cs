#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision<T> : PlanarDivision 
    {
        private readonly Dictionary<FaceId, T> _faceValues = new();


        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public PlanarDivision(float epsilon = 0.00001f) : base(epsilon) { }
        
        public bool TryGetValue(FaceId faceId, out T value)
        {
            return _faceValues.TryGetValue(faceId, out value);
        }
        
        public bool TryGetValue(Vector2 position, out T value)
        {
            FaceId faceId = FaceAt(position);
            Debug.Log("face at pos: " + faceId);
            Debug.Log("empty: " + (faceId == FaceId.Empty));
            if (faceId == FaceId.Empty)
            {
                value = default!;
                return false;
            }
            
  
            return _faceValues.TryGetValue(faceId, out value);
        }

        public void SetValue(FaceId faceId, T data)
        {
            if (!HasFace(faceId)) throw new KeyNotFoundException($"FaceId {faceId} not found.");
            _faceValues[faceId] = data;
        }

        public bool RemoveValue(FaceId faceId)
        {
            return _faceValues.Remove(faceId);
        }

        // Deletes
        public void DeleteFace(FaceId faceId)
        {
            
        }

        /// <summary>
        /// Adds a polygon to the planar division and sets the value of the face inside it.
        /// It's "drawn over" the existing faces and all the edges and vertices inside it are deleted.
        /// </summary>
        public void AddPolygonOver(Polygon polygon, T value)
        {
            // Draw the edges of the polygon and store the vertices on them:
            HashSet<VertexId> verticesOnEdges = new();

            List<VertexId> verticesOnFirstEdge = new();
            
            foreach ((Vector2 p1, Vector2 p2) in polygon.Edges())
            {
                var verticesOnEdge = PlanarGraph.AddLine(p1, p2);
                verticesOnEdges.UnionWith(verticesOnEdge);
                if (verticesOnFirstEdge.Count == 0)
                {
                    verticesOnFirstEdge = verticesOnEdge;
                }
            }
            

            // Vertices that are inside the polygon, not on the edges will be deleted:
            PlanarGraph.VerticesIn(polygon.Bounds())
                .Where(v => polygon.IsPointInside(PlanarGraph.Position(v)) && !verticesOnEdges.Contains(v))
                .ToList()
                .ForEach(PlanarGraph.DeleteVertex);

      
            // Set the value:

            FaceId faceId = FaceLeftOfEdge(verticesOnFirstEdge[0], verticesOnFirstEdge[1]);
            
            SetValue(faceId, value);

        }

        #endregion
        #region ---------------------------------------- PROTECTED METHODS ---------------------------------------------
        
        
        // If a face is split, the new faces get the old faces value.
        protected override void OnFaceSplit(FaceId oldFaceId, FaceId newFaceId1, FaceId newFaceId2)
        {
            base.OnFaceSplit(oldFaceId, newFaceId1, newFaceId2);
            if (!_faceValues.ContainsKey(oldFaceId)) return;
            _faceValues[newFaceId1] = _faceValues[oldFaceId];
            _faceValues[newFaceId2] = _faceValues[oldFaceId];
            _faceValues.Remove(oldFaceId);
        }

        
        // If faces with same value are merged, the new face is assigned the same value.
        protected override void OnFacesMerged(FaceId oldFaceId1, FaceId oldFaceId2, FaceId newFaceId)
        {
            base.OnFacesMerged(oldFaceId1, oldFaceId2, newFaceId);

            if (FacesHaveSameValue(oldFaceId1, oldFaceId2))
            {
                _faceValues[newFaceId] = _faceValues[oldFaceId1];
            }
            _faceValues.Remove(oldFaceId1);
            _faceValues.Remove(oldFaceId2);

          
        }
        
        protected override void OnFaceDestroyed(FaceId faceId)
        {
            base.OnFaceDestroyed(faceId);
            _faceValues.Remove(faceId);
        }

        private bool FacesHaveSameValue(FaceId f1, FaceId f2)
        {
            return _faceValues.TryGetValue(f1, out var value1) &&
                   _faceValues.TryGetValue(f2, out var value2) &&
                   EqualityComparer<T>.Default.Equals(value1, value2);
        }

        #endregion

    }
}