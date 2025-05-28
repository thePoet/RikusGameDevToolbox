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
        private abstract class Path : ISpatialData
        {
            public HalfEdge HalfEdge; // Random half edge on the path
            public Envelope Envelope { get; set; }
        }

        private class Face : Path
        {
            public FaceId Id;
            public List<OutsideFace> Holes;
        }

        private class OutsideFace : Path
        {
            public Face ContainingFace;
           // public bool IsLine;
            public bool IsContainedInFace => ContainingFace != null;
        }

        private class HalfEdge
        {
            public VertexId Origin; // Vertex at the origin of the half edge
            public Path Path; // Path that this half edge belongs to (Face or OutsideFace)
            public HalfEdge Twin; // Half edge with same vertices that goes in the opposite direction 
            public HalfEdge Next; // Next half edge in the boundary of LeftFace
            public HalfEdge Previous; // Previous half edge in the boundary of LeftFace
            public VertexId Target => Twin.Origin; 
        }
        
        
        private readonly Dictionary<VertexId, HalfEdge> _incidentEdge = new(); // Random half edge starting from the vertex
        private readonly Dictionary<FaceId, Face> _faces = new();
        private readonly RTree<Path> _paths = new();
       // private readonly HashSet<HalfEdge> _halfEdges = new(); // Tarvitaanko?
       
        #region ----------------------------------------- PUBLIC PROPERTIES --------------------------------------------
       
        public IEnumerable<FaceId> AllFaces() => _faces.Keys;
        public FaceId FaceAt(Vector2 position) => NormalFaceAt(position)?.Id ?? FaceId.Empty;
        public bool HasFace(FaceId faceId) => _faces.ContainsKey(faceId);
        public IEnumerable<FaceId> FacesIn(Rect rect) => NormalFacesIn(rect).Select(face => face.Id);
        public SimplePolygon FaceContour(FaceId faceId) => new SimplePolygon(FaceVertexPositions(_faces[faceId].HalfEdge));
        public Polygon FacePolygon(FaceId faceId) => FaceAsPolygon(_faces[faceId]);
        public int NumFaces => _faces.Values.Count;

        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public PlanarDivision(float epsilon = 0.00001f) : base(epsilon)
        {
        }
        
        public FaceId FaceLeftOfEdge(VertexId v1, VertexId v2)
        {
            HalfEdge halfEdge = GetHalfEdge(v1, v2);
            if (halfEdge == null) throw new ArgumentException("Edge does not exist.");
            return FaceOfPath(halfEdge.Path);
        }
        
        public FaceId FaceLeftOfEdge(Vector2 pos1, Vector2 pos2)
        {
            var v1 = VertexAt(pos1);
            var v2 = VertexAt(pos2);
            if (v1 == null || v2 == null)
            {
                throw new ArgumentException("No vertex at given position.");
            }
            return FaceLeftOfEdge(v1,v2);
        }

        
        public void DeleteDegenerateEdges()
        {
            Edges
                .Where(edge => IsEdgeDegenerate(edge.v1, edge.v2))
                .ToList()
                .ForEach(edge => DeleteEdge(edge.Item1, edge.Item2));
            
            bool IsEdgeDegenerate(VertexId v1, VertexId v2) => GetHalfEdge(v1, v2).Path == GetHalfEdge(v2, v1).Path;
        }

        public override void TransformVertices(Func<Vector2, Vector2> transformFunction)
        {
            base.TransformVertices(transformFunction);
            
            foreach (var face in _faces.Values)
            {
                _paths.Delete(face);
                face.Envelope = new Envelope(RectExtensions.CreateRectToEncapsulate(FaceVertexPositions(face.HalfEdge)));
                _paths.Insert(face);
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
            HalfEdge oldEdge = GetHalfEdge(v1, v2);
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
            HalfEdge halfEdge = GetHalfEdge(v1, v2);
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
            
          //  _halfEdges.Add(halfEdge1);
          //  _halfEdges.Add(halfEdge2);
            _incidentEdge[v1] = halfEdge1;
            _incidentEdge[v2] = halfEdge2;

            return (halfEdge1, halfEdge2);
            
            
            // Returns next half edge CCW of vector origin -> target that is coming INTO the origin vertex 
            // Null if there are no edges besides the given edge 
            HalfEdge IncomingHalfEdgeCcwTo(VertexId origin, VertexId target)
            {
                Vector2 direction = Position(target) - Position(origin);
          
                var edge = EdgesOriginatingFrom(origin)
                    .Where(he => he.Target != target) // Exclude self
                    .OrderBy(he => direction.AngleCounterClockwise(Position(he.Target) - Position(he.Origin))) // CCW order
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
                return;
            }
            
            if (IsOnlyEdgeAtTarget(edge)) // Extend old path
            {
                edge.Path = edge.Previous.Path;
                edge.Twin.Path = edge.Previous.Path;
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

        //    _halfEdges.Add(newHalfEdge1);
        //    _halfEdges.Add(newHalfEdge2);
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

        //    _halfEdges.Remove(edge); 
        //    _halfEdges.Remove(edge.Twin);
            
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
                newFace.Holes = HolesInsideFace(newFace);
                newFace.Holes?.ForEach(hole => hole.ContainingFace = newFace);
                return newFace;
            }

            OutsideFace CreateOutsideFace(HalfEdge start)
            {
                OutsideFace outsideFace = new OutsideFace();
                outsideFace.HalfEdge = start;
                UpdateFaceEnvelope(outsideFace);
                outsideFace.ContainingFace = FaceContainingOutsideFace(outsideFace);
                if (outsideFace.ContainingFace != null)
                {
                    if (outsideFace.ContainingFace.Holes == null)
                        outsideFace.ContainingFace.Holes = new List<OutsideFace>();
                    outsideFace.ContainingFace.Holes.Add(outsideFace);
                }
                
                Debug.Log("Created outside face with containing face: " + outsideFace.ContainingFace?.Id);
                return outsideFace;
            }
            
            bool IsCcw(HalfEdge start) => !GeometryUtils.IsClockwise(FaceVertexPositions(start));

            bool HasArea(HalfEdge start) => PathHalfEdges(start).Any(e => !PathHalfEdges(start).Contains(e.Twin));
        }

        
        private void DeletePath(Path path)
        {
            if (!_paths.Delete(path)) throw new InvalidOperationException("Could not delete path from spatial collection.");
            
            if (path is Face face)
            {
                _faces.Remove(face.Id);
                if (face.Holes != null)
                {
                    foreach (OutsideFace hole in face.Holes)
                    {
                        hole.ContainingFace = FaceContainingOutsideFace(hole);
                    }
                }
            }

            if (path is OutsideFace { ContainingFace: not null } outsideFace)
            {
                outsideFace.ContainingFace.Holes?.Remove(outsideFace);
            }
        }



        private List<HalfEdge> EdgesOriginatingFrom(VertexId vertex)
        {
            var result = new List<HalfEdge>();
            if (_incidentEdge.TryGetValue(vertex, out var halfEdge) == false) return result; // No edges 
            
           // var halfEdge = _incidentEdge[vertex];
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
           return PathHalfEdges(halfEdge).Select(he => Position(he.Origin));
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
            if (face.Holes != null)
            {
                foreach (OutsideFace hole in face.Holes)
                {
                    paths.Add(FaceVertexPositions(hole.HalfEdge).ToList());
                }
            }

            List<Polygon> polygons = Polygon.CreateFromPaths(paths);
            if (polygons.Count!=1) throw new InvalidOperationException("Face created " + polygons.Count + " polygons instead of 1");
            return polygons.First();
        }

        /// <summary>
        ///  Returns boundary faces inside the face. Note: only the first level of holes are returned, not holes inside the holes
        /// </summary>
        private List<OutsideFace> HolesInsideFace(Face face)
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

            return holes.Any() ? holes : null;
        }

        /// <summary>
        ///  Returns the smallest normal face that contains the outside face, or null if it is not contained in any face.
        /// </summary>
        private Face FaceContainingOutsideFace(OutsideFace outsideFace)
        {
            return _paths.Search(Position(outsideFace.HalfEdge.Origin))
                .OfType<Face>()
                .Where(face => IsPathsInsideAnother(outsideFace, face))
                .OrderBy(face => face.Envelope.Area)
                .FirstOrDefault();
        }

        private bool IsFaceAtLeastPartiallyInRectangle(Path face, Rect rect)
        {
            return PathHalfEdges(face.HalfEdge)
                .Any(edge => Intersection.LineSegmentRectangle(Position(edge.Origin), Position(edge.Target), rect));
        }
        
        private bool IsPathsInsideAnother(Path path, Path anotherPath)
        {
            //Assuming the edges do not cross, we can just check if all vertices of a are inside b
              return FaceVertexPositions(path.HalfEdge)  
                .All(vPos => IsPointInsidePath(vPos, anotherPath));
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
            if (path is OutsideFace { ContainingFace: not null } outsideFace) return outsideFace.ContainingFace.Id;
            return FaceId.Empty;
        }
        
        private void UpdateFaceEnvelope(Path face)
        {
            face.Envelope = new Envelope( RectExtensions.CreateRectToEncapsulate(FaceVertexPositions(face.HalfEdge)) );
        }
        
        #endregion
    }
}