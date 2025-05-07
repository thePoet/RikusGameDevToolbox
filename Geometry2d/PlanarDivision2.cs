using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.RTree;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision2 : PlanarGraph
    {
        private class Face : ISpatialData
        {
            public FaceId Id;
            public HalfEdge HalfEdge; // Random half edge of the face
            public bool IsBoundary; // Not an actual face but outside boundary of planar division i.e. the area outside faces.
            public List<HalfEdge> Contained; // For each hole/group of faces wholly inside the face, we store one half edge
       
            public Envelope Envelope { get; set; }
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
        
        
        private Dictionary<VertexId, HalfEdge> _incidentEdge = new();
        private Dictionary<FaceId, Face> _faces = new();
        private RTree<Face> _facesSpatial = new();
        private HashSet<HalfEdge> _halfEdges = new();
       
        #region ----------------------------------------- PUBLIC PROPERTIES --------------------------------------------
       
        public IEnumerable<FaceId> AllFaces() => _faces.Values.Where(face => !face.IsBoundary).Select(face => face.Id);

        // TODO: does not ensure face is in rect, just its bounding box
        public IEnumerable<FaceId> FacesIn(Rect rect) => _facesSpatial.Search(rect).Where(face => !face.IsBoundary).Select(face => face.Id);
        public Polygon FacePolygon(FaceId faceId) => FaceAsPolygon(_faces[faceId]);
        public int NumFaces => _faces.Count(face => !face.Value.IsBoundary);
        
        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public PlanarDivision2(float epsilon = 0.00001f) : base(epsilon)
        {
        }

        public FaceId FaceAt(Vector2 position)
        {
            var candidates = _facesSpatial.Search(position).Where(face => !face.IsBoundary);
            foreach (var face in candidates)
            {
                if (IsPointOnFace(position, face)) return face.Id;
            }
            return FaceId.Empty;
        }
        
        
        public void DeleteEdgesThatDoNotFormFaces()
        {
            throw new NotImplementedException();
        }
        
        #endregion
        #region ---------------------------------------- PROTECTED METHODS ---------------------------------------------

        protected override void OnAddVertex(VertexId vertexId)
        {
            _incidentEdge[vertexId] = null;
        }
        protected override void OnAddEdge(VertexId v1, VertexId v2)
        {
            base.OnAddEdge(v1, v2);

            bool v1HasEdges = _incidentEdge[v1] != null;
            bool v2HasEdges = _incidentEdge[v2] != null;
                
            if (!v1HasEdges && !v2HasEdges) AddEdgeUnconnectedVertices(v1, v2);
            if (v1HasEdges && !v2HasEdges) AddEdgeOneConnectedVertex(v1, v2);
            if (!v1HasEdges && v2HasEdges) AddEdgeOneConnectedVertex(v2, v1);
            if (v1HasEdges && v2HasEdges) AddEdgeTwoConnectedVertices(v1, v2);
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
        
        private void AddEdgeUnconnectedVertices(VertexId v1, VertexId v2)
        {
            var halfEdge1 = new HalfEdge();
            halfEdge1.Origin = v1;
        
            var halfEdge2 = new HalfEdge();
            halfEdge2.Origin = v2;
            
            halfEdge1.Next = halfEdge2;
            halfEdge1.Previous = halfEdge2;
            halfEdge2.Next = halfEdge1;
            halfEdge2.Previous = halfEdge1;

            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);
            
            _incidentEdge[v1] = halfEdge1;
            _incidentEdge[v2] = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);
           
            Face face = CreateFace(halfEdge1, isBoundary: true);
            
            halfEdge1.FaceOnLeft = face;
            halfEdge2.FaceOnLeft = face;

        }
        
        private void AddEdgeOneConnectedVertex(VertexId connectedVertex, VertexId unconnectedVertex)
        {
            var v1 = connectedVertex;
            var v2 = unconnectedVertex;
         
            var previousHalfEdge = FindPreviousHalfEdge(v1, v2);
            
            var halfEdge1 = new HalfEdge
            {
                Origin = v1,
                Previous = previousHalfEdge,
                FaceOnLeft = previousHalfEdge.FaceOnLeft,
            };
            
            var halfEdge2 = new HalfEdge
            {
                Origin = v2,
                Previous = halfEdge1,
                Next = previousHalfEdge.Next,
                FaceOnLeft = previousHalfEdge.FaceOnLeft,
            };
            
            halfEdge1.Next = halfEdge2;
            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);
            
            _incidentEdge[v2] = halfEdge2;
            
            previousHalfEdge.Next = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);

        }
        
        private void AddEdgeTwoConnectedVertices(VertexId v1, VertexId v2)
        {
            var previousV1 = FindPreviousHalfEdge(v1, v2);
            var previousV2 = FindPreviousHalfEdge(v2, v1);
            
            var halfEdge1 = new HalfEdge
            {
                Origin = v1,
                Previous = previousV1,
                Next = previousV2.Next,
            };
            
            var halfEdge2 = new HalfEdge
            {
                Origin = v2,
                Previous = previousV2,
                Next = previousV1.Next,
            };
            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);

            previousV1.Next = halfEdge1;
            previousV2.Next = halfEdge2;
            halfEdge1.Next.Previous = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);

            var face1 = halfEdge1.Next.FaceOnLeft;
            var face2 = halfEdge2.Next.FaceOnLeft;

        
            
            if (!face1.IsBoundary && !face2.IsBoundary) // if splitting normal face
            {
                UpdateFace(halfEdge1, face1);
                Face newFace = CreateFace(halfEdge2, isBoundary: false);
                UpdateFace(halfEdge2, newFace);
            }
            else if (face1.IsBoundary && face2.IsBoundary) // if connecting two boundaries
            {
                if (face1 != face2) // Different boundaries are combined into one boundary
                {
                    DeleteFace(face2);
                    UpdateFace(halfEdge1, face1);
                }
                else // Same boundary => one inner face is created
                {
                    bool halfEdge1IsBoundary = GeometryUtils.IsClockwise(Path(halfEdge1));
                    if (halfEdge1IsBoundary)
                    {
                        UpdateFace(halfEdge1, face1);
                        var newInnerFace = CreateFace(halfEdge2, isBoundary: false);
                        UpdateFace(halfEdge2, newInnerFace);
                    }
                    else
                    {
                        UpdateFace(halfEdge2, face1);
                        var newInnerFace = CreateFace(halfEdge1, isBoundary: false);
                        UpdateFace(halfEdge1, newInnerFace);
                    }
                }
            }
            else if (face1.IsBoundary != face2.IsBoundary) // i.e. if one face is completely inside the other...
            {
                // We'll destroy the boundary face and replace it with the regular face
                if (face1.IsBoundary)
                {
                    DeleteFace(face2);
                    UpdateFace(halfEdge1, face1);
                }
                else
                {
                    DeleteFace(face1);
                    UpdateFace(halfEdge1, face2);
                }
                if (halfEdge1.FaceOnLeft != halfEdge2.FaceOnLeft) throw new InvalidOperationException("Something went wrong when setting faces.");
            }
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

            if (!onlyEdgeFromOrigin && !onlyEdgeFromTarget)
            {
//something

                return;
            }
            
            
            if (edge.FaceOnLeft != edge.Twin.FaceOnLeft) throw new InvalidOperationException("Something wen wrong when deleting edge.");

            if (onlyEdgeFromOrigin && onlyEdgeFromTarget)
            {
                DeleteFace(edge.FaceOnLeft);
                return;
            }

            if (onlyEdgeFromOrigin)
            {
                UpdateFace(edge.Next, edge.FaceOnLeft);
                return;
            }
            
            UpdateFace(edge.Previous, edge.Twin.FaceOnLeft);
            return;
  /*
            if (!onlyEdgeFromOrigin)
            {
                UpdateFace(edge.Twin.Next, CreateFace(edge.Twin.Next, isBoundary: false)); // TEMP
            }
            if (!onlyEdgeFromTarget)
            {
                UpdateFace(edge.Next, CreateFace(edge.Next, isBoundary: false)); // TEMP
            }
*/
        }
        
        private Face CreateFace(HalfEdge edge, bool isBoundary)
        {
            var face = new Face
            {
                Id = FaceId.New(),
                IsBoundary = isBoundary,
                HalfEdge = edge,
            };
            UpdateFaceEnvelope(face);
            _faces.Add(face.Id, face);
            _facesSpatial.Insert(face);
            return face;
        }
        
        /// <summary>
        ///  Follows the path starting from the given half edge and sets the face for all of them and updates the spatial index.
        /// </summary>
        private void UpdateFace(HalfEdge edge, Face face)
        {
            face.HalfEdge = edge;
            HalfEdge current = edge;
            for (int i = 0; i < 10000; i++)
            {
                current.FaceOnLeft = face;
                current = current.Next;
                if (current == edge)
                {
                    if (!_facesSpatial.Delete(face)) throw new InvalidOperationException("Could not delete face from spatial collection.");
                    UpdateFaceEnvelope(face);
                    _facesSpatial.Insert(face);
                    return;
                }
            }
            throw new InvalidOperationException("Infinite loop in SetFaceForPath");
        }

        private void DeleteFace(Face face)
        {
            _faces.Remove(face.Id);
            if (!_facesSpatial.Delete(face)) throw new InvalidOperationException("Could not delete face from spatial collection.");
        }

        
        // Returns next half edge CCW of origin -> target that is coming INTO the origin vertex 
        private HalfEdge FindPreviousHalfEdge(VertexId origin, VertexId target)
        {
            Vector2 direction = Position(target) - Position(origin);
            if (_incidentEdge[origin] == null) return null;
            return EdgesOriginatingFrom(origin)
                .OrderBy(he => direction.AngleCounterClockwise(Position(he.Target) - Position(he.Origin))) // CCW order
                .First() 
                .Twin; // HalfEdge coming into the origin vertex

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

        private HalfEdge FindHalfEdge(VertexId origin, VertexId target)
        {
            return EdgesOriginatingFrom(origin).FirstOrDefault(he => he.Target == target);
        }


        private IEnumerable<Vector2> Path(HalfEdge halfEdge)
        {
            var current = halfEdge;
            int i = 0;
            do
            {
                i++;
                if (i > 10000) throw new InvalidOperationException("Infinite loop in Path");
                yield return Position(current.Origin);
                current = current.Next;
            } while (current != halfEdge);
        }
        
        
        private List<List<Vector2>> Paths(Face face)
        {
            var paths = new List<List<Vector2>> { Path(face.HalfEdge).ToList() };
            if (face.Contained != null)
            {
                paths.AddRange(face.Contained.Select(halfEdge => Path(halfEdge).ToList()));
            }

            return paths;
        }
        
        private Polygon FaceAsPolygon(Face face)
        {
            return Polygon.CreateFromPaths(Paths(face) ).First();
        }


        private bool IsPointOnFace(Vector2 position, Face face) => FaceAsPolygon(face).IsPointInside(position);
            
        


        private void UpdateFaceEnvelope(Face face)
        {
            face.Envelope = new Envelope( RectExtensions.CreateRectToEncapsulate(Path(face.HalfEdge)) );
        }
        
        #endregion
    }
}