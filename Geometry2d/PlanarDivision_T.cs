#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.Geometry2d.Internal;

using UnityEngine;
using static RikusGameDevToolbox.Geometry2d.Util;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision<T> : PlanarDivision 
    {
        public bool ValuelessFacesAreEmpty = true; 
        
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
            if (faceId == FaceId.Empty)
            {
                value = default!;
                return false;
            }
  
            return _faceValues.TryGetValue(faceId, out value);
        }

        public T Value(FaceId faceId)
        {
            if (_faceValues.TryGetValue(faceId, out T value)) return value;
            return default!;
        }
    
        
        public bool FaceHasValue(FaceId faceId)
        {
            return _faceValues.ContainsKey(faceId);
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
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Adds a polygon to the planar division and sets the value of the face inside it.
        /// It's "drawn over" the existing faces and all the edges and vertices inside it are deleted.
        /// </summary>
        public void AddPolygonOver(Polygon polygon, T value)
        {
            // Draw the edges of the polygon:
            HashSet<VertexId> verticesOnEdges = new(); // Store all vertices that are on the edges of the polygon (inc. holes).
            List<VertexId> verticesOnContour = new(); // Store all vertices that are on the contour of the polygon (the first path).
            for (int pathIndex = 0; pathIndex < polygon.NumHoles + 1; pathIndex++)
            {
                foreach ((Vector2 p1, Vector2 p2) in polygon.Edges(pathIndex))
                {
                    var verticesOnEdge = PlanarGraph.AddLine(p1, p2);
                    verticesOnEdges.UnionWith(verticesOnEdge);
                    if (pathIndex == 0)
                    {
                        verticesOnContour.AddRange(verticesOnEdge.Take(verticesOnEdge.Count - 1));
                    }
                }
            }
            
            // Vertices that are inside the polygon, not on the edges will be deleted:
            PlanarGraph.VerticesIn(polygon.Bounds())
                .Where(v => polygon.IsPointInside(PlanarGraph.Position(v)) && !verticesOnEdges.Contains(v))
                .ToList()
                .ForEach(PlanarGraph.DeleteVertex);
            

            HashSet<FaceId> facesOnPolygon = new();
            LoopingPairs(verticesOnContour).ToList()
                .ForEach(pair=> facesOnPolygon.Add( FaceLeftOfEdge(pair.Item1, pair.Item2) ));
            
        

            for (int i = 0; i < 10000; i++)
            {
                if (facesOnPolygon.Count == 1)
                {
                    SetValue(facesOnPolygon.First(), value);
                    return;
                }
                
                FaceId face = facesOnPolygon.First();
                facesOnPolygon.Remove(face);

                FaceId neighbour = facesOnPolygon.FirstOrDefault(f => Neighbours(f).Contains(face))
                                   ?? throw new InvalidOperationException("Disconnected face detected while trying to add polygon over.");
                facesOnPolygon.Remove(neighbour);

                facesOnPolygon.Add( Merge(face, neighbour) );
            }
                
            throw new InvalidOperationException("Infite loop detected while trying to add polygon over.");
            

        }

        public int NumberOfSeparateGroups()
        {
            return Contours().Count;
        }
        
        /// <summary>
        /// Remove separate groups of faces from the planar division and return them as list.
        /// Largest group remains in this PlanarDivision.
        /// Embedded faces are treated as separate groups.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<PlanarDivision<T>> Split()
        {
            List<PlanarDivision<T>> result = new();
          
            List<OutsideFace> contours = Contours().OrderByDescending(outsideFace => outsideFace.Envelope.Area).ToList();
            
            for (int i=1; i < contours.Count; i++)
            {
                var faces = FacesInsideContour(contours[i]);
                result.Add(SplitFaces(contours[i], faces));
            }

            return result;
            
            HashSet<FaceId> FacesInsideContour(OutsideFace contour)
            {
               Face faceInside = contour.HalfEdge.Twin.Path as Face ?? throw new InvalidOperationException("Could not get face inside contour.");
               PathFinding<FaceId> pathFinding = new PathFinding<FaceId>
                   (traversableNodes: faceId => Neighbours(faceId), //.Where( FaceHasValue ), 
                       movementCost: (face1, face2) => 1f,
                       movementCostEstimate: (face1, face2) => 1f);
               return pathFinding.GetAllNodesConnectedTo(faceInside.Id).ToHashSet();
            }
            
            PlanarDivision<T> SplitFaces(OutsideFace contour, HashSet<FaceId> faces)
            {
                PlanarDivision<T> result = new PlanarDivision<T>(PlanarGraph.Epsilon);
                result.ValuelessFacesAreEmpty = ValuelessFacesAreEmpty;

                result._outsideFaces.Add(contour);
                _outsideFaces.Remove(contour);
                _paths.Delete(contour);
                result._paths.Insert(contour);

                foreach (FaceId faceId in faces)
                {
                    Face face = _faces[faceId];
                    result._faces[faceId] = face;
                    _faces.Remove(faceId);
                    
              
                    result._paths.Insert(face);
                    _paths.Delete(face);
                    
                    if (_faceValues.TryGetValue(faceId, out T value))
                    {
                        result._faceValues[faceId] = value;
                        _faceValues.Remove(faceId);
                    }
                }


                HashSet<VertexId> vertices = result._paths.All()
                    .SelectMany(path => PathHalfEdges(path.HalfEdge).Select(he => he.Origin))
                    .ToHashSet();
                
                result.PlanarGraph = PlanarGraph.MakeDeepCopy(preserveVertexIds:true, vertexIdFilter: v => vertices.Contains(v));
                foreach (VertexId vertexId in vertices)
                {
                    PlanarGraph.DeleteVertexWithoutCallingObservers(vertexId);
                    result._incidentEdge.Add(vertexId, _incidentEdge[vertexId]);
                    _incidentEdge.Remove(vertexId);
                }
                
                
                
                return result;
            }
            
       
          
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

        private List<OutsideFace> Contours()
        {
            // TODO: This is too slow
            return _outsideFaces.Where(IsContour).ToList();
            
            bool IsContour(OutsideFace face) 
            {
                FaceId containingFace = FaceOfPath(face);
                return containingFace == FaceId.Empty || (ValuelessFacesAreEmpty && !_faceValues.ContainsKey(containingFace));
            }
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