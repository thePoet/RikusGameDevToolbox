using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.RTree;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision : PlanarGraph
    {
        private enum FaceType
        {
            Normal,     
            Boundary,   
            Line
        }
        
        private class Face : ISpatialData
        {
            public FaceId Id;
            public HalfEdge HalfEdge; // Random half edge of the face
            public FaceType FaceType;
            public Envelope Envelope { get; set; }

            public override string ToString()
            {
                return FaceType + " face " + Id.Value.ToString().Substring(0, 5);
            }
        }

        private class HalfEdge
        {
            public VertexId Origin; // Vertex at the origin of the half edge
            public Face FaceOnLeft; // Face on the left side of the half edge
            public HalfEdge Twin; // Half edge with same vertices that goes in the opposite direction 
            public HalfEdge Next; // Next half edge in the boundary of LeftFace
            public HalfEdge Previous; // Previous half edge in the boundary of LeftFace
            public VertexId Target => Twin.Origin; 
        }
        
        
        private readonly Dictionary<VertexId, HalfEdge> _incidentEdge = new(); // Random half edge starting from the vertex
        private readonly Dictionary<FaceId, Face> _faces = new();
        private readonly RTree<Face> _facesSpatial = new();
        private readonly HashSet<HalfEdge> _halfEdges = new(); // Tarvitaanko?
       
        #region ----------------------------------------- PUBLIC PROPERTIES --------------------------------------------
       
        public IEnumerable<FaceId> AllFaces() => _faces.Values.Where(face => face.FaceType==FaceType.Normal).Select(face => face.Id);
        public FaceId FaceAt(Vector2 position) => NormalFacesAt(position).FirstOrDefault()?.Id ?? FaceId.Empty;
        public bool HasFace(FaceId faceId) => _faces.ContainsKey(faceId);
        public IEnumerable<FaceId> FacesIn(Rect rect) => NormalFacesIn(rect).Select(face => face.Id);
        public SimplePolygon FaceContour(FaceId faceId) => new SimplePolygon(FaceVertexPositions(_faces[faceId].HalfEdge));
        public Polygon FacePolygon(FaceId faceId) => FaceAsPolygon(_faces[faceId]);
        public int NumFaces => _faces.Values.Count(face => face.FaceType==FaceType.Normal);
        
       

        internal bool IsNormalFace(FaceId faceId) => _faces.TryGetValue(faceId, out var face) && face.FaceType == FaceType.Normal;

        
        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public PlanarDivision(float epsilon = 0.00001f) : base(epsilon)
        {
        }
        
        public FaceId FaceLeftOfEdge(VertexId v1, VertexId v2)
        {
            HalfEdge halfEdge = FindHalfEdge(v1, v2);
            if (halfEdge == null) throw new ArgumentException("Tried to get face of edge that does not exist.");
            return halfEdge.FaceOnLeft.Id;
        }
        
        public FaceId FaceLeftOfEdge(Vector2 pos1, Vector2 pos2)
        {
            return FaceLeftOfEdge(VertexAt(pos1), VertexAt(pos2));
        }

        
        public void DeleteDegenerateEdges()
        {
            throw new NotImplementedException();
        }

        public override void TransformVertices(Func<Vector2, Vector2> transformFunction)
        {
            base.TransformVertices(transformFunction);
            
            foreach (var face in _faces.Values)
            {
                _facesSpatial.Delete(face);
                face.Envelope = new Envelope(RectExtensions.CreateRectToEncapsulate(FaceVertexPositions(face.HalfEdge)));
                _facesSpatial.Insert(face);
            }
        }
        #endregion
        #region ---------------------------------------- PROTECTED METHODS ---------------------------------------------

        protected virtual void OnFaceSplit(FaceId oldFace, FaceId newFace1, FaceId newFace2)
        {
            
        }
        
        protected virtual void OnFacesMerged(FaceId oldFace1, FaceId oldFace2, FaceId newFace)
        {
            
        }

        protected virtual void OnFaceCreated(FaceId faceId)
        {
            
        }

        protected virtual void OnFaceDeformed(FaceId faceId)          // ehkä   
        {
        }
        
        protected virtual void OnFaceDestroyed(FaceId faceId)
        {
            
        }
        
        protected override void OnAddVertex(VertexId vertexId)
        {
            _incidentEdge[vertexId] = null;
        }
        
        protected override void OnAddEdge(VertexId v1, VertexId v2)
        {
            base.OnAddEdge(v1, v2);
            var (halfEdge1, halfEdge2) = AddHalfEdgePairBetweenVertices(v1, v2);
            UpdateFacesAfterAddingHalfEdgePair(halfEdge1);
        }
        
        protected override void OnSplitEdge(VertexId v1, VertexId v2, VertexId newVertex)
        {
            _incidentEdge[newVertex] = null;
            HalfEdge oldEdge = FindHalfEdge(v1, v2);
            if (oldEdge == null)
            {
                throw new ArgumentException("Tried to split edge that does not exist.");
            }
            InsertVertexIntoEdge(oldEdge, newVertex);
        }

        protected override void OnDeleteVertex(VertexId vertexId)
        {
            _incidentEdge.Remove(vertexId);
        }
        
        protected override void OnDeleteEdge(VertexId v1, VertexId v2)
        {
            HalfEdge halfEdge = FindHalfEdge(v1, v2);
            DeleteHalfEdgeTwins(halfEdge);
        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------


        private (HalfEdge v1Tov2, HalfEdge v2Tov1) AddHalfEdgePairBetweenVertices(VertexId v1, VertexId v2)
        {
            var previousAtV1 = IncomingHalfEdgeCcwTo(v1, v2);
            var previousAtV2 = IncomingHalfEdgeCcwTo(v2, v1);

            var halfEdge1 = new HalfEdge();
            var halfEdge2 = new HalfEdge();

            halfEdge1.Origin = v1;
            halfEdge1.Previous = previousAtV1 ?? halfEdge2;
            halfEdge1.Next = previousAtV2?.Next ?? halfEdge2;
            
            halfEdge2.Origin = v2;
            halfEdge2.Previous = previousAtV2 ?? halfEdge1;
            halfEdge2.Next = previousAtV1?.Next ?? halfEdge1;
       
            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);
            
            halfEdge1.Previous.Next = halfEdge1;
            halfEdge2.Previous.Next = halfEdge2;
            halfEdge1.Next.Previous = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);
            _incidentEdge[v1] = halfEdge1;
            _incidentEdge[v2] = halfEdge2;

            return (halfEdge1, halfEdge2);
        }
        


        private void UpdateFacesAfterAddingHalfEdgePair(HalfEdge edge)
        {
            if (IsOnlyEdgeAtOrigin(edge) && IsOnlyEdgeAtTarget(edge))
            {
                // New unconnected edge, so create a new face:
                SetNewFaceOnHalfEdgesStartingFrom(edge);
                return;
            }
            
            if (IsOnlyEdgeAtOrigin(edge))
            {
                edge.FaceOnLeft = edge.Next.FaceOnLeft;
                edge.Twin.FaceOnLeft = edge.Next.FaceOnLeft;
                return;
            }
            
            if (IsOnlyEdgeAtTarget(edge))
            {
                edge.FaceOnLeft = edge.Previous.FaceOnLeft;
                edge.Twin.FaceOnLeft = edge.Previous.FaceOnLeft;
                return;
            }
        
            
            var face1 = edge.Next.FaceOnLeft;
            var face2 = edge.Twin.Next.FaceOnLeft;

            DeleteFace(face1);
            
            if (face1 != face2) DeleteFace(face2);

            Face newFace1 = SetNewFaceOnHalfEdgesStartingFrom(edge);
            Face newFace2 = null;

            newFace2 = (edge.Twin.FaceOnLeft != newFace1) 
                ? SetNewFaceOnHalfEdgesStartingFrom(edge.Twin) 
                : newFace1; // In case both faces are the same. (Happens when two boundary faces are merged)
           
            if (face1==face2 && face1.FaceType == FaceType.Normal)
            {
                OnFaceSplit(face1.Id, newFace1.Id, newFace2.Id);
            }
            
            if (face1==face2 && face1.FaceType == FaceType.Boundary)
            {
                if (newFace1.FaceType == FaceType.Normal && newFace2.FaceType == FaceType.Boundary)
                {
                    OnFaceCreated(newFace1.Id);
                    return;
                }
                if (newFace2.FaceType == FaceType.Normal && newFace1.FaceType == FaceType.Boundary)
                {
                    OnFaceCreated(newFace2.Id);
                    return;
                }
                throw new Exception("Something went wrong when combining two boundary faces.");
            }
            
            
            bool IsOnlyEdgeAtOrigin(HalfEdge e) => e.Previous == e.Twin;
            bool IsOnlyEdgeAtTarget(HalfEdge e) => e.Next == e.Twin;

        }
        
   
                
        private void InsertVertexIntoEdge(HalfEdge edge, VertexId newVertex)
        {
            HalfEdge newHalfEdge1 = new HalfEdge();
            HalfEdge newHalfEdge2 = new HalfEdge();
            (newHalfEdge1.Twin, newHalfEdge2.Twin) = (newHalfEdge2, newHalfEdge1);
            _incidentEdge[newVertex] = newHalfEdge1;
            _incidentEdge[edge.Target] = newHalfEdge2;
            
            newHalfEdge1.Origin = newVertex;
            newHalfEdge1.Previous = edge;
            newHalfEdge1.FaceOnLeft = edge.FaceOnLeft;
            newHalfEdge1.Next = (edge.Next == edge.Twin) ? newHalfEdge2 : edge.Next;
            
            newHalfEdge2.Origin = edge.Target;
            newHalfEdge2.Previous = edge.Twin.Previous == edge ? newHalfEdge1 : edge.Twin.Previous;
            newHalfEdge2.FaceOnLeft = edge.Twin.FaceOnLeft;
            newHalfEdge2.Next = edge.Twin;

            edge.Twin.Previous.Next = newHalfEdge2;
            edge.Next.Previous = newHalfEdge1;

            edge.Next = newHalfEdge1;
            edge.Twin.Previous = newHalfEdge2;
            edge.Twin.Origin = newVertex;

            _halfEdges.Add(newHalfEdge1);
            _halfEdges.Add(newHalfEdge2);
        }
        
        private void DeleteHalfEdgeTwins(HalfEdge edge)
        {
            bool onlyEdgeFromOrigin = edge.Previous == edge.Twin;

            if (onlyEdgeFromOrigin)
            {
                _incidentEdge[edge.Origin] = null;
            }
            else
            {
                edge.Previous.Next = edge.Twin.Next;
                edge.Twin.Next.Previous = edge.Previous;
                _incidentEdge[edge.Origin] = edge.Twin.Next;
            }
            
            bool onlyEdgeFromTarget = edge.Next == edge.Twin;
            if (onlyEdgeFromTarget)
            {
                _incidentEdge[edge.Target] = null;
            }
            else
            {
                edge.Twin.Previous.Next = edge.Next;
                edge.Next.Previous = edge.Twin.Previous;
                _incidentEdge[edge.Target] = edge.Next;
            }

            _halfEdges.Remove(edge); 
            _halfEdges.Remove(edge.Twin);
            
            // Update faces:
            
            var faceLeft = edge.FaceOnLeft;
            var faceRight = edge.Twin.FaceOnLeft;
            
            DeleteFace(faceLeft);
            if (faceLeft != faceRight) DeleteFace(faceRight);

            Face newFace1 = null;

            if (!onlyEdgeFromOrigin)
            {
                newFace1 = SetNewFaceOnHalfEdgesStartingFrom(edge.Previous);
            }

            if (!onlyEdgeFromTarget && edge.Next.FaceOnLeft != newFace1)
            {
                SetNewFaceOnHalfEdgesStartingFrom(edge.Next);
            }

            if (!onlyEdgeFromTarget && !onlyEdgeFromOrigin)
            {
                
            }
            
        }


        private Face SetNewFaceOnHalfEdgesStartingFrom(HalfEdge startEdge)
        {
            var face = new Face
            {
                Id = FaceId.New(),
                HalfEdge = startEdge
            };
            foreach (var edge  in FaceHalfEdges(startEdge))
            {
                edge.FaceOnLeft = face;
            }
            face.FaceType = GetFaceType(startEdge);
            
            UpdateFaceEnvelope(face);
            _facesSpatial.Insert(face);
            _faces.Add(face.Id, face);

            return face;
            
            FaceType GetFaceType(HalfEdge startEdge)
            {
                if (FaceHalfEdges(startEdge).All(edge => edge.FaceOnLeft == edge.Twin.FaceOnLeft)) return FaceType.Line;
                if (GeometryUtils.IsClockwise(FaceVertexPositions(startEdge))) return FaceType.Boundary;
                return FaceType.Normal;
            }        
        }

        
        private void DeleteFace(Face face)
        {
            _faces.Remove(face.Id);
            if (!_facesSpatial.Delete(face)) throw new InvalidOperationException("Could not delete face from spatial collection.");
        }

        
        // Returns next half edge CCW of origin -> target that is coming INTO the origin vertex 
        // Null if there are no edges besides the given edge 
        private HalfEdge IncomingHalfEdgeCcwTo(VertexId origin, VertexId target)
        {
            Vector2 direction = Position(target) - Position(origin);
          
            var edge = EdgesOriginatingFrom(origin)
                .Where(he => he.Target != target) // Exclude self
                .OrderBy(he => direction.AngleCounterClockwise(Position(he.Target) - Position(he.Origin))) // CCW order
                .FirstOrDefault();

            return edge?.Twin;
        }
        

        private List<HalfEdge> EdgesOriginatingFrom(VertexId vertex)
        {
            var result = new List<HalfEdge>();
            
            var halfEdge = _incidentEdge[vertex];
            if (halfEdge == null) return result;
            int i = 0;
            do
            {
                i++;
                if (i > 1000) throw new InvalidOperationException("Infinite loop in EdgesOriginatingFrom");
                result.Add(halfEdge);
                halfEdge = halfEdge.Twin.Next;
            } while (halfEdge != _incidentEdge[vertex]);

            return result;
        }
        
        private List<Face> NormalFacesAt(Vector2 position)
        {
            return _facesSpatial.Search(position)
                .Where(face => face.FaceType == FaceType.Normal && IsPointOnFace(position, face))
                .ToList();
        }
        
        private List<Face> NormalFacesIn(Rect rect)
        {
            return _facesSpatial.Search(rect)
                .Where(face => face.FaceType == FaceType.Normal && IsFaceAtLeastPartiallyInRectangle(face, rect))
                .ToList();
        }
        
        private List<Face> BoundaryFacesAt(Vector2 position)
        {
            return _facesSpatial.Search(position)
                .Where(face => face.FaceType == FaceType.Boundary && IsPointInsideFaceContour(position, face))
                .ToList();
        }
        
 

        private HalfEdge FindHalfEdge(VertexId origin, VertexId target)
        {
            return EdgesOriginatingFrom(origin).FirstOrDefault(he => he.Target == target);
        }


        private IEnumerable<Vector2> FaceVertexPositions(HalfEdge halfEdge)
        {
           return FaceHalfEdges(halfEdge).Select(he => Position(he.Origin));
        }

        private IEnumerable<HalfEdge> FaceHalfEdges(HalfEdge first)
        {
            var current = first;
            for (int i = 0; i < 10000; i++)
            {
                yield return current; 
                current = current.Next; 
                if (current == first) yield break;
            } 
            throw new InvalidOperationException("Infinite loop in FaceHalfEdges"); 
        }
        
        
        private Polygon FaceAsPolygon(Face face)
        {
            var paths = new List<List<Vector2>> { FaceVertexPositions(face.HalfEdge).ToList() };
            
            foreach (Face hole in HolesInFace(face))
            {
                paths.Add(FaceVertexPositions(hole.HalfEdge).ToList());
            }

            List<Polygon> polygons = Polygon.CreateFromPaths(paths);
            if (polygons.Count!=1) throw new InvalidOperationException("Face created " + polygons.Count + " polygons instead of 1");
            return polygons.First();
        }

        /// <summary>
        ///  Returns boundary faces inside the face. Note: only the first level of holes are returned, not holes inside the holes
        /// </summary>
        private List<Face> HolesInFace(Face face)
        {
            var holes = _facesSpatial.Search(face.Envelope)
                .Where(f => f.FaceType == FaceType.Boundary && IsFaceInside(f, face))
                .ToList();
          
            // Remove holes that are inside other holes
            var copy = new List<Face>(holes);
            foreach (Face f in copy)
            {
                holes.RemoveAll(hole => hole != f && IsFaceInside(hole, f));
            }

            return holes;
        }

        private bool IsFaceAtLeastPartiallyInRectangle(Face face, Rect rect)
        {
            return FaceHalfEdges(face.HalfEdge)
                .Any(edge => Intersection.LineSegmentRectangle(Position(edge.Origin), Position(edge.Target), rect));
        }
        
        private bool IsFaceInside(Face face, Face containerFace)
        {
            //Assuming the edges do not cross, we can just check if all vertices of a are inside b
              return FaceVertexPositions(face.HalfEdge)  
                .All(vPos => IsPointInsideFaceContour(vPos, containerFace));
        }

        private bool IsPointInsideFaceContour(Vector2 position, Face face)
        {
            var polygon = face.FaceType != FaceType.Boundary ? new SimplePolygon(FaceVertexPositions(face.HalfEdge)) : 
                new SimplePolygon(FaceVertexPositions(face.HalfEdge).Reverse());
            
            return polygon.IsPointInside(position);
        }
        
        private bool IsPointOnFace(Vector2 position, Face face)
        {
            if (face.FaceType == FaceType.Boundary) return !FaceAsPolygon(face).IsPointInside(position);
            return FaceAsPolygon(face).IsPointInside(position);
        }
        
        private void UpdateFaceEnvelope(Face face)
        {
            face.Envelope = new Envelope( RectExtensions.CreateRectToEncapsulate(FaceVertexPositions(face.HalfEdge)) );
        }
        
        #endregion
    }
}