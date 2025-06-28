using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RBush;
using UnityEngine;
using RikusGameDevToolbox.Geometry2d.Internal;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision
    {
        internal class HalfEdge
        {
            public VertexId Origin; // Vertex at the origin of the half edge
            public Path Path; // Path that this half edge belongs to (Face or OutsideFace)
            public HalfEdge Twin; // Half edge with same vertices that goes in the opposite direction 
            public HalfEdge Next; // Next half edge in the boundary of LeftFace
            public HalfEdge Previous; // Previous half edge in the boundary of LeftFace
            public VertexId Target => Twin.Origin; 
        }

        protected readonly PlanarGraph PlanarGraph;
        
        private readonly Dictionary<VertexId, HalfEdge> _incidentEdge = new(); // Random half edge starting from the vertex
        private readonly Dictionary<FaceId, Face> _faces = new();
        private readonly RBush<Path> _paths = new();
        private readonly PlanarDivisionHoles _holes; 
    
       
        #region ----------------------------------------- PUBLIC PROPERTIES --------------------------------------------
       
        public IEnumerable<FaceId> AllFaces() => _faces.Keys;
        public FaceId FaceAt(Vector2 position) => NormalFaceAt(position)?.Id ?? FaceId.Empty;
        public bool HasFace(FaceId faceId) => _faces.ContainsKey(faceId);
        public IEnumerable<FaceId> FacesIn(Rect rect) => NormalFacesIn(rect).Select(face => face.Id); 
        public SimplePolygon FaceContour(FaceId faceId) => new SimplePolygon(FaceVertexPositions(_faces[faceId].HalfEdge));
        public Polygon FacePolygon(FaceId faceId) => FaceAsPolygon(_faces[faceId]);
        public int NumFaces => _faces.Values.Count;
        public int NumEdges => PlanarGraph.Edges.Count;
        public int NumVertices => PlanarGraph.Vertices.Count;
        public IEnumerable<Vector2> Vertices => PlanarGraph.Vertices.Select(v => PlanarGraph.Position(v));
        public IEnumerable<(Vector2, Vector2)> Edges => PlanarGraph.Edges
            .Select(edge => (PlanarGraph.Position(edge.Item1), PlanarGraph.Position(edge.Item2)));
        public List<Vector2> AddLine(Vector2 v1, Vector2 v2) => PlanarGraph.AddLine(v1, v2).Select(id => PlanarGraph.Position(id)).ToList();
        public void DeleteEdge(Vector2 v1, Vector2 v2) => PlanarGraph.DeleteEdge(v1, v2);
        public void DeleteVerticesWithoutEdges() => PlanarGraph.DeleteVerticesWithoutEdges();
        
    

        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public PlanarDivision(float epsilon = 0.00001f) 
        {
            PlanarGraph = new PlanarGraph(epsilon)
            {
                Observers =
                {
                    OnAddVertex = OnAddVertex,
                    OnAddEdge = OnAddEdge,
                    OnSplitEdge = OnSplitEdge,
                    OnDeleteVertex = OnDeleteVertex,
                    OnDeleteEdge = OnDeleteEdge
                }
            };
            _holes = new PlanarDivisionHoles(_paths, FaceVertexPositions);
        }
        
        public FaceId FaceLeftOfEdge(VertexId v1, VertexId v2)
        {
            HalfEdge halfEdge = GetHalfEdge(v1, v2);
            if (halfEdge == null) throw new ArgumentException("Edge does not exist.");
            return FaceOfPath(halfEdge.Path);
        }
        
        public FaceId FaceLeftOfEdge(Vector2 pos1, Vector2 pos2)
        {
            var v1 = PlanarGraph.VertexAt(pos1);
            var v2 = PlanarGraph.VertexAt(pos2);
            if (v1 == null || v2 == null)
            {
                throw new ArgumentException("No vertex at given position.");
            }
            return FaceLeftOfEdge(v1,v2);
        }

        /// <summary>
        /// Return neighbouring faces of the face but not ones contained inside it (holes).
        /// </summary>
        /// <param name="faceId"></param>
        /// <returns></returns>
        public IEnumerable<FaceId> Neighbours(FaceId faceId)
        {
            var face = FaceOrThrow(faceId);

            return PathHalfEdges(face.HalfEdge)
                .Select(he => he.Twin.Path)
                .OfType<Face>()
                .Distinct()
                .Select(f => f.Id);
        }

        public FaceId Merge(FaceId faceId1, FaceId faceId2)
        {
            var (face1, face2) = (FaceOrThrow(faceId1), FaceOrThrow(faceId2));
            
            List<HalfEdge> sharedEdges = PathHalfEdges(face1.HalfEdge)
                .Where(he => he.Twin.Path is Face face && face.Id == faceId2)
                .ToList();

            HalfEdge nonSharedEdge = PathHalfEdges(face1.HalfEdge)
                                         .FirstOrDefault(he => he.Twin.Path is Face face && face.Id != faceId2)
                                     ?? PathHalfEdges(face2.HalfEdge)
                                         .FirstOrDefault(he => he.Twin.Path is Face face && face.Id != faceId1);

            sharedEdges.ForEach(se => PlanarGraph.DeleteEdge(se.Origin, se.Target));

            FaceId newFaceId = FaceLeftOfEdge(nonSharedEdge.Origin, nonSharedEdge.Target);
            OnFacesMerged(faceId1, faceId2, newFaceId);
            return newFaceId;
        }

        public void DeleteDegenerateEdges()
        {
            PlanarGraph.Edges
                .Where(edge => IsEdgeDegenerate(edge.v1, edge.v2))
                .ToList()
                .ForEach(edge => PlanarGraph.DeleteEdge(edge.Item1, edge.Item2));
            return;
            
            bool IsEdgeDegenerate(VertexId v1, VertexId v2) => GetHalfEdge(v1, v2).Path == GetHalfEdge(v2, v1).Path;
        }

        public void TransformVertices(Func<Vector2, Vector2> transformFunction)
        {
            PlanarGraph.TransformVertices(transformFunction);
            
            foreach (var face in _faces.Values)
            {
                _paths.Delete(face);
                UpdateFaceEnvelope(face);
                _paths.Insert(face);
            }
        }

        public bool DeleteVertex(Vector2 position)
        {
            var id = PlanarGraph.VertexAt(position);
            if (id == null) return false;
            PlanarGraph.DeleteVertex(id);
            return true;
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

        protected virtual void OnFaceDestroyed(FaceId faceId)
        {
            
        }
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private void OnAddVertex(VertexId vertexId)
        {
            _incidentEdge[vertexId] = null;
        }
        
        private void OnAddEdge(VertexId v1, VertexId v2)
        {
            if (v1==v2) Debug.LogWarning("Tried to add edge with same vertices: " + v1 + " and " + v2);
            var (halfEdge1, halfEdge2) = AddHalfEdgePairBetweenVertices(v1, v2);
            UpdateFacesAfterAddingHalfEdgePair(halfEdge1);
        }
        
        private void OnSplitEdge(VertexId v1, VertexId v2, VertexId newVertex)
        {
            _incidentEdge[newVertex] = null;
            HalfEdge oldEdge = GetHalfEdge(v1, v2);
            if (oldEdge == null)
            {
                throw new ArgumentException("Tried to split edge that does not exist.");
            }
            InsertVertexIntoEdge(oldEdge, newVertex);
        }

        private void OnDeleteVertex(VertexId vertexId)
        {
            _incidentEdge.Remove(vertexId);
        }
        
        private void OnDeleteEdge(VertexId v1, VertexId v2)
        {
            HalfEdge halfEdge = GetHalfEdge(v1, v2);
            DeleteHalfEdgeTwins(halfEdge);
        }



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
            
            _incidentEdge[v1] = halfEdge1;
            _incidentEdge[v2] = halfEdge2;

            return (halfEdge1, halfEdge2);
            
            
            // Returns next half edge CCW of vector origin -> target that is coming INTO the origin vertex 
            // Null if there are no edges besides the given edge 
            HalfEdge IncomingHalfEdgeCcwTo(VertexId origin, VertexId target)
            {
                Vector2 direction = PlanarGraph.Position(target) - PlanarGraph.Position(origin);
          
                var edge = EdgesOriginatingFrom(origin)
                    .Where(he => he.Target != target) // Exclude self
                    .OrderBy(he => direction.AngleCounterClockwise(PlanarGraph.Position(he.Target) - PlanarGraph.Position(he.Origin))) // CCW order
                    .FirstOrDefault();

                return edge?.Twin;
            }
        }
        


        private void UpdateFacesAfterAddingHalfEdgePair(HalfEdge edge)
        {
            if (IsOnlyEdgeAtOrigin(edge) && IsOnlyEdgeAtTarget(edge)) // New unconnected edge, so create a new path:
            {
                CreateAndSetNewPathFrom(edge);
                return;
            }
            
            if (IsOnlyEdgeAtOrigin(edge)) // Extend old path
            {
                edge.Path = edge.Next.Path;
                edge.Twin.Path = edge.Next.Path;
                UpdatePath(edge.Path);
                return;
            }
            
            if (IsOnlyEdgeAtTarget(edge)) // Extend old path
            {
                edge.Path = edge.Previous.Path;
                edge.Twin.Path = edge.Previous.Path;
                UpdatePath(edge.Path);
                return;
            }
        
            
            var path1 = edge.Next.Path;
            var path2 = edge.Twin.Next.Path;

            DeletePath(path1);
            
            if (path1 != path2) DeletePath(path2);

            Path newPath1 = CreateAndSetNewPathFrom(edge);
            Path newPath2 = null;

            newPath2 = edge.Twin.Path != newPath1
                ? CreateAndSetNewPathFrom(edge.Twin) 
                : newPath1; // In case both faces are the same. (Happens when two boundary faces are merged)
           
            if (path1==path2 && path1 is Face face)
            {
                if (newPath1 is Face newFace1 && newPath2 is Face newFace2)
                {
                    OnFaceSplit(face.Id, newFace1.Id, newFace2.Id);
                }
                else
                {
                    throw new Exception("Something went wrong when splitting two faces.");
                }
            }
            
            if (path1==path2 && path1 is OutsideFace outsideFace)
            {
                if (newPath1 is Face face1 && newPath2 is OutsideFace)
                {
                    OnFaceCreated(face1.Id);
                    return;
                }
                if (newPath2 is Face face2 && newPath1 is OutsideFace)
                {
                    OnFaceCreated(face2.Id);
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
            newHalfEdge1.Path = edge.Path;
            newHalfEdge1.Next = (edge.Next == edge.Twin) ? newHalfEdge2 : edge.Next;
            
            newHalfEdge2.Origin = edge.Target;
            newHalfEdge2.Previous = edge.Twin.Previous == edge ? newHalfEdge1 : edge.Twin.Previous;
            newHalfEdge2.Path = edge.Twin.Path;
            newHalfEdge2.Next = edge.Twin;

            edge.Twin.Previous.Next = newHalfEdge2;
            edge.Next.Previous = newHalfEdge1;

            edge.Next = newHalfEdge1;
            edge.Twin.Previous = newHalfEdge2;
            edge.Twin.Origin = newVertex;

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

            // Update faces:
            
            var faceLeft = edge.Path;
            var faceRight = edge.Twin.Path;
            
            DeletePath(faceLeft);
            if (faceLeft != faceRight) DeletePath(faceRight);

            Path newFace1 = null;

            if (!onlyEdgeFromOrigin)
            {
                newFace1 = CreateAndSetNewPathFrom(edge.Previous);
            }

            if (!onlyEdgeFromTarget && edge.Next.Path != newFace1)
            {
                CreateAndSetNewPathFrom(edge.Next);
            }

            if (!onlyEdgeFromTarget && !onlyEdgeFromOrigin)
            {
                
            }
            
        }


        private Path CreateAndSetNewPathFrom(HalfEdge startEdge)
        {
           
            Path path = HasArea(startEdge) && IsCcw(startEdge)
                ? CreateFace(startEdge)
                : CreateOutsideFace(startEdge);
            
            
            foreach (var edge  in PathHalfEdges(startEdge))
            {
                edge.Path = path;
            }
            _paths.Insert(path);
            if (path is Face face) _faces.Add(face.Id, face);

            return path;

            Face CreateFace(HalfEdge start)
            {
                Face newFace = new Face()
                {
                    Id = FaceId.New(),
                    HalfEdge = start
                };
                UpdateFaceEnvelope(newFace);

                return newFace;
            }

            OutsideFace CreateOutsideFace(HalfEdge start)
            {
                OutsideFace outsideFace = new OutsideFace();
                outsideFace.HalfEdge = start;
                UpdateFaceEnvelope(outsideFace);

                return outsideFace;
            }
            
            bool IsCcw(HalfEdge start) => !GeometryUtils.IsClockwise(FaceVertexPositions(start));

            bool HasArea(HalfEdge start) => PathHalfEdges(start).Any(e => !PathHalfEdges(start).Contains(e.Twin));
        }

        private void UpdatePath(Path path)
        {
            if (!_paths.Delete(path))
            {
                
               foreach (var p in _paths.All())
               {
                   if (p==path) Debug.Log("Found path with envelope: " + p.Envelope);
               }

               throw new InvalidOperationException("Deleting path from spatial collection failed.");
            }
            UpdateFaceEnvelope(path);
            _paths.Insert(path);
        }
        
        private void DeletePath(Path path)
        {
            if (!_paths.Delete(path))
            {
                foreach (var p in _paths.All())
                {
                    if (p==path) throw new InvalidOperationException("Could not find path to be deleted from spatial collection.");
                }
                throw new InvalidOperationException("Tried to delete path that does not exist in spatial collection.");
            }
            
            if (path is Face face)
            {
                _faces.Remove(face.Id);
            }
        }



        private List<HalfEdge> EdgesOriginatingFrom(VertexId vertex)
        {
            var result = new List<HalfEdge>();
            if (_incidentEdge.TryGetValue(vertex, out var halfEdge) == false) return result; // No edges 
            
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
        
        private Face NormalFaceAt(Vector2 position)
        {
            return _paths.Search(position)
                .OfType<Face>()
                .Where(face => IsPointInsidePath(position, face))
                .OrderBy(face => face.Envelope.Area) // Return the smallest face that contains the point
                .FirstOrDefault();
        }
        
        private List<Face> NormalFacesIn(Rect rect)
        {
            return _paths.Search(rect)
                .OfType<Face>()
                .Where(face => IsFaceAtLeastPartiallyInRectangle(face, rect))
                .ToList();
        }
        
        private List<OutsideFace> OutsideFacesAt(Vector2 position)
        {
            return _paths.Search(position)
                .OfType<OutsideFace>()
                .Where(outsideFace => IsPointInsidePath(position, outsideFace))
                .ToList();
        }
        
 

        private HalfEdge GetHalfEdge(VertexId origin, VertexId target)
        {
            return EdgesOriginatingFrom(origin).FirstOrDefault(he => he.Target == target);
        }


        private IEnumerable<Vector2> FaceVertexPositions(HalfEdge halfEdge)
        {
           return PathHalfEdges(halfEdge).Select(he => PlanarGraph.Position(he.Origin));
        }

        private IEnumerable<HalfEdge> PathHalfEdges(HalfEdge first)
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
            foreach (OutsideFace hole in _holes.HolesOf(face))
            {
                paths.Add(FaceVertexPositions(hole.HalfEdge).ToList());
            }
            List<Polygon> polygons = Polygon.CreateFromPaths(paths);
            if (polygons.Count!=1) throw new InvalidOperationException("Face created " + polygons.Count + " polygons instead of 1");
            return polygons.First();
        }

        
        private bool IsFaceAtLeastPartiallyInRectangle(Path face, Rect rect)
        {
            return PathHalfEdges(face.HalfEdge)
                .Any(edge => Intersection.LineSegmentRectangle(PlanarGraph.Position(edge.Origin), PlanarGraph.Position(edge.Target), rect));
        }

        private bool IsPointInsidePath(Vector2 position, Path path)
        {
            var polygon = path is Face
                ? new SimplePolygon(FaceVertexPositions(path.HalfEdge)) 
                : new SimplePolygon(FaceVertexPositions(path.HalfEdge).Reverse());
            return polygon.IsPointInside(position);
        }
 
        /// <summary> Returns the Id of the face that the path belongs to as either a contour or a hole.</summary>
        private FaceId FaceOfPath(Path path)
        {
            if (path is Face face) return face.Id;
            if (path is OutsideFace outsideFace)
            {
                var containingFace = _holes.FaceContaining(outsideFace);
                if (containingFace != null) return containingFace.Id;
            }

            return FaceId.Empty;
        }
        
        private void UpdateFaceEnvelope(Path face)
        {
            Rect rect = RectExtensions.CreateRectToEncapsulate(FaceVertexPositions(face.HalfEdge));
            face.Envelope = new Envelope(MinX:rect.xMin, 
                                         MinY:rect.yMin, 
                                         MaxX:rect.xMax, 
                                         MaxY:rect.yMax);
        }

        private Face FaceOrThrow(FaceId faceId)
        {
            return _faces.TryGetValue(faceId, out var face) ? face : throw new ArgumentException("No face with given id.");
        }

        #endregion

    }
}