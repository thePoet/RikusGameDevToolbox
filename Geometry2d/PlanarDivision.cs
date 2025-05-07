using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{


    
    public class PlanarDivision
    {
        private class Vertex
        {
            public VertexId Id;
            public Vector2 Position;
            public HalfEdge HalfEdge; // Random half edge that has this vertex as origin
        }
        
        private class Face 
        {
            public FaceId Id;
            public HalfEdge HalfEdge; // Random half edge of the face
            public bool IsBoundary; // Not an actual face but outside boundary of planar division i.e. the area outside faces.
            public List<HalfEdge> Contained; // For each hole/group of faces wholly inside the face, we store one half edge
            public Polygon AsPolygon => Polygon.CreateFromPaths(Paths()).First();

            private List<List<Vector2>> Paths()
            {
                var paths = new List<List<Vector2>> { HalfEdge.Path().ToList() };
                if (Contained != null)
                {
                    paths.AddRange(Contained.Select(halfEdge => halfEdge.Path().ToList()));
                }
                return paths;
            }
        }

        private class HalfEdge
        {
            public Vertex Origin; // Vertex at the origin of the half edge
            public Face FaceOnLeft; // Face on the left side of the half edge
            public HalfEdge Twin; // Half edge with same vertices that goes in the opposite direction 
            public HalfEdge Next; // Next half edge in the boundary of LeftFace
            public HalfEdge Previous; // Previous half edge in the boundary of LeftFace
            
            public Vertex Target => Twin.Origin; 
            public IEnumerable<Vector2> Path() 
            {
                var current = this;
                int i = 0;
                do
                {
                    i++;
                    if (i > 10000) throw new InvalidOperationException("Infinite loop in Path");
                    yield return current.Origin.Position;
                    current = current.Next;
                } while (current != this);
            }
          
        }

     

        #region ---------------------------------------------- FIELDS --------------------------------------------------

        

        private Dictionary<FaceId, Face> _faces = new();

        private SpatialCollection2d<Vertex> _vertices = new();
        private Dictionary<VertexId, Vertex> _verticesById = new();
        private HashSet<HalfEdge> _halfEdges = new();
        private float _epsilon = 0.00001f;

        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public IEnumerable<VertexId> Vertices() => _verticesById.Keys;
        
        public VertexId VertexAt(Vector2 position, float tolerance = 0.00001f) => _vertices.ItemsInCircle(position, tolerance)
            .Select(v => v.Id)
            .FirstOrDefault();
        
        public int NumFaces() => _faces.Values.Count(face => face.IsBoundary == false);
        
        public IEnumerable<FaceId> FaceIds() => _faces.Values.Where(face => !face.IsBoundary).Select(face => face.Id);

        public IEnumerable<VertexId> FaceVertices(FaceId faceId)
        {
            if (!_faces.TryGetValue(faceId, out var face)) throw new ArgumentException($"Face with id {faceId} does not exist.");
            return VerticesOnFace(face.HalfEdge);
        }
        
        /// <summary>
        /// The shape of the PlanarDivision as whole given as a list of polygons. TODO: may contain holes.
        /// </summary>
        public List<Polygon> DivisionShapes() => _faces.Values.Where(face => face.IsBoundary).Select(face => face.AsPolygon).ToList();
        
        /// <summary>
        /// Shape of the given face as Polygon. TODO: May contain holes.
        /// </summary>
        public Polygon FaceShape(FaceId faceId)
        {
            if (!_faces.TryGetValue(faceId, out var face))
            {
                throw new ArgumentException($"Face with id {faceId} does not exist.");
            }
            return face.AsPolygon;
        }

        /// <summary>
        /// Is there an edge connecting the vertices?
        /// </summary>
        public bool IsEdge(VertexId v1, VertexId v2)
        {
            var vertex1 = _verticesById.GetValueOrDefault(v1);
            var vertex2 = _verticesById.GetValueOrDefault(v2);
            if (vertex1==null || vertex2==null) throw new ArgumentException("Non-existing vertex.");
            return FindHalfEdge(vertex1, vertex2) != null;
        }

        /// <summary>
        /// Returns the face on the left side of the edge. If the edge is a boundary edge, returns FaceId.Empty.
        /// </summary>
        /// <param name="edgeStart"></param>
        /// <param name="edgeEnd"></param>
        /// <returns></returns>
        public FaceId FaceLeftOfEdge(VertexId edgeStart, VertexId edgeEnd)
        {
            var halfEdge = FindHalfEdge(_verticesById[edgeStart], _verticesById[edgeEnd]);
            if (halfEdge.FaceOnLeft.IsBoundary)
            {
                return FaceId.Empty;
            }
            return halfEdge.FaceOnLeft.Id;
        }
        
        public Vector2 VertexPosition(VertexId vertexId)
        {
            if (!_verticesById.TryGetValue(vertexId, out var vertex)) throw new ArgumentException($"Vertex with id {vertexId} does not exist.");
            return vertex.Position;
        }

        public VertexId AddVertex(Vector2 position)
        {
            var vertex = new Vertex
            {
                Id = VertexId.New(),
                Position = position,
                HalfEdge = null
            };
            
            _vertices.Add(position, vertex);
            _verticesById.Add(vertex.Id, vertex);
            
            return vertex.Id;
        }

        public void AddEdge(VertexId vertex1, VertexId vertex2)
        {
            var v1 = _verticesById.GetValueOrDefault(vertex1);
            var v2 = _verticesById.GetValueOrDefault(vertex2);
     
            
            if (v1==null || v2==null)
            {
                throw new ArgumentException("Tried to add edge on non-existing vertex.");
            }

            if (FindHalfEdge(v1,v2) != null)
            {
                throw new InvalidOperationException("Tried to add edge that already exists.");
            }
            
            if (v1==v2)
            {
                throw new InvalidOperationException("Tried to add edge with same start and end vertex.");
            }

        
            Rect boundingBox =  RectExtensions.CreateRectToEncapsulate(v1.Position, v2.Position);
            boundingBox.Grow(Mathf.Max(boundingBox.size.x, boundingBox.size.y) * 0.0001f);
            var nearbyEdges = EdgesIn(boundingBox);


            foreach (var edge in nearbyEdges)
            {
                var result = Overlap(v1, v2, edge.Origin, edge.Target);
                if (result.isOverlapping)
                {
                    if (result.nonOverlappingEdges == null) return;
                    foreach (var edgePair in result.nonOverlappingEdges)
                    {
                        AddEdge(edgePair.Item1.Id, edgePair.Item2.Id);
                    }
                    return;
                }
            }
          
            
            List<(HalfEdge edge, Vector2 position)> intersections = FindIntersections(v1, v2, nearbyEdges);

            var currentVertex = v1;
            foreach (var intersection in intersections)
            {
                VertexId newVertexId = SplitEdge(intersection.edge.Origin.Id, intersection.edge.Target.Id, intersection.position);
                Vertex newVertex = _verticesById[newVertexId];
   
                AddEdgeBetween(currentVertex, newVertex);
                currentVertex = newVertex;
            }
            

            AddEdgeBetween(currentVertex,v2);
            return;
       
            void AddEdgeBetween(Vertex v1, Vertex v2)
            {
                bool v1HasEdges = v1.HalfEdge != null;
                bool v2HasEdges = v2.HalfEdge != null;
                
                if (!v1HasEdges && !v2HasEdges) AddEdgeUnconnectedVertices(v1, v2);
                if (v1HasEdges && !v2HasEdges) AddEdgeOneConnectedVertex(v1, v2);
                if (!v1HasEdges && v2HasEdges) AddEdgeOneConnectedVertex(v2, v1);
                if (v1HasEdges && v2HasEdges) AddEdgeTwoConnectedVertices(v1, v2);
            }
       
          
        }

        public VertexId SplitEdge(VertexId v1, VertexId v2, Vector2 newVertexPosition)
        {
            HalfEdge oldEdge= HalfEdgeConnecting(_verticesById[v1], _verticesById[v2]);
            if (oldEdge == null)
            {
                throw new ArgumentException("Tried to split edge that does not exist.");
            }
          
            // TODO: TARKISTA RISTEYKSET!

            VertexId newVertexID = AddVertex(newVertexPosition);
          
            InsertVertexIntoEdge(oldEdge, _verticesById[newVertexID]);

            return newVertexID;
        }

        public void DeleteEdge(VertexId v1, VertexId v2)
        {
            var vertex1 = _verticesById.GetValueOrDefault(v1);
            var vertex2 = _verticesById.GetValueOrDefault(v2);
     
            
            if (v1==null || v2==null)
            {
                throw new ArgumentException("Tried to delete edge with non-existing vertex.");
            }

            var edge = FindHalfEdge(vertex1, vertex2);
            if (edge == null)
            {
                throw new InvalidOperationException("Tried to delete non-existing edge.");
            }
            
            DeleteEdge(edge);
        }

        public void DeleteVertex(VertexId vertexId)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(vertexId);
            if (vertex == null) throw new ArgumentException("Tried to delete non-existing vertex.");
            
            foreach (HalfEdge edge in EdgesOriginatingFrom(vertex))
            {
                DeleteEdge(edge);
            }
            _vertices.Remove(vertex.Position, vertex);
            _verticesById.Remove(vertex.Id);
        }

        public void TransformVertices(Func<Vector2, Vector2> transformFunction)
        {
            _vertices.Clear();
            
            foreach (Vertex vertex in _verticesById.Values)
            {
                vertex.Position = transformFunction(vertex.Position);
                _vertices.Add(vertex.Position, vertex);
            }
        }

        
        // TODO: With r-tree or something
        public FaceId FaceAt(Vector2 position)
        {
            foreach (var face in _faces.Values)
            {
                if (face.AsPolygon.IsPointInside(position))
                {
                    return face.Id;
                }
            }
            return FaceId.Empty;
        }
        
    

        public List<(Vector2, Vector2, FaceId, bool)> HalfEdges()
        {
            var result = new List<(Vector2, Vector2, FaceId, bool )>();
            foreach (var halfEdge in _halfEdges)
            {
                result.Add((halfEdge.Origin.Position, halfEdge.Target.Position, halfEdge.FaceOnLeft.Id, halfEdge.FaceOnLeft.IsBoundary));
            }

            return result;
        }
        
        public string DebugString()
        {
            var edgeList = _halfEdges.ToList();
            
            string result = "POINTS: ";
            foreach (var vertex in _vertices.ToList())
            {
                result += IndexVertex(vertex) + " : " + vertex.Position + "\n";
            }
            foreach (var halfEdge in _halfEdges)
            {
                result += IndexEdge(halfEdge) +  " : ";
                result += IndexVertex(halfEdge.Origin) + " -> " + IndexVertex(halfEdge.Target) + "   ";
                result += IndexEdge(halfEdge.Previous)+  " -> "+ IndexEdge(halfEdge.Next) + "\n";
            }
            return result;
            string IndexEdge(HalfEdge e)
            {
                for (int i=0; i<edgeList.Count; i++)
                {
                    if (edgeList[i] == e) return "E" + i;
                }
                return "E???";
            }

            string IndexVertex(Vertex v)
            {
                var list = _vertices.ToList();
                for (int i=0; i<list.Count; i++)
                {
                    if (list[i] == v) return "V" + i;
                }
                return "V????";
            }
        }
        
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

/*
        
        private Vertex GetOrAddVertexAt(Vector2 position)
        {
            var existing = _vertices.ItemsInCircle(position, _epsilon);
            if (existing.Any()) return existing.First();

            
            boundingBox.Grow(Mathf.Max(boundingBox.size.x, boundingBox.size.y) * 0.0001f);
            var nearbyEdges = EdgesIn(boundingBox);
            
            if (existing == null)
            {
                existing = new Vertex
                {
                    Id = VertexId.New(),
                    Position = position,
                    HalfEdge = null
                };
                _vertices.Add(position, existing);
                _verticesById.Add(existing.Id, existing);
            }
            return existing;
        }*/
        
        
        /// <summary>
        /// Looks for overlap between the proposed edge v1-v2 and existing edge edgeV1-edgeV2.
        /// </summary>
        /// <returns>
        ///  isOverlapping: true if the edges overlap.
        /// nonOverlappingEdges: List of vertex pairs for edges that are the non-overlapping part of given edges.
        /// Null if no overlap.
        /// </returns>
        private (bool isOverlapping, List<(Vertex, Vertex)> nonOverlappingEdges ) Overlap( Vertex v1, Vertex v2, Vertex edgeV1, Vertex edgeV2)
        {
                
            bool overlap = GeometryUtils.AreLineSegmentsOverlapping(v1.Position, v2.Position,
                edgeV1.Position, edgeV2.Position);
            
            if (!overlap) return (false,null);

            if (Vector2.Distance(v1.Position, edgeV1.Position) > Vector2.Distance(v1.Position, edgeV2.Position))
            {
                // Arrange vertices so that v1 is closer to edgeV1
                (v1, v2) = (v2, v1);
            }
            
            Vertex commonVertex = null;
            if (v1 == edgeV1 || v1 == edgeV2) commonVertex = v1;
            if (v2 == edgeV1 || v2 == edgeV2) commonVertex = v2;
            
            var (v1p, v2p) = (v1.Position, v2.Position);
            var (e1p, e2p) = (edgeV1.Position, edgeV2.Position);
            bool isV1OnEdge = GeometryUtils.IsPointOnEdge(v1p, e1p, e2p, _epsilon);
            bool isV2OnEdge = GeometryUtils.IsPointOnEdge(v2p, e1p, e2p, _epsilon);

        
            
            if (commonVertex==null)
            {
                if (!isV1OnEdge && !isV2OnEdge)
                {
                    // Proposed edge extends the existing edge from both ends
                    return (true, new List<(Vertex, Vertex)> { (v1, edgeV1), (v2, edgeV2) });
                }

                if (isV1OnEdge && isV2OnEdge)
                {
                    // The proposed edge is completely inside the other edge
                    return (true, null);
                }

                if (isV1OnEdge)
                {
                    return (true, new List<(Vertex, Vertex)> { (edgeV2, v2) });
                }
                    
                return (true, new List<(Vertex, Vertex)> { (v1, edgeV1) });
            }


            if (commonVertex == v1)
            {
                if (isV2OnEdge) return (true,null);
                return (isOverlapping: false, nonOverlappingEdges: null);
            }
            
            if (commonVertex == v2)
            {
                if (isV1OnEdge) return (true,null);
                return (isOverlapping: false, nonOverlappingEdges: null);
            }
    
            throw new InvalidOperationException("Something went wrong when checking for overlap.");
          
        }
        
        private HalfEdge HalfEdgeConnecting(Vertex v1, Vertex v2)
        {
            if (v1 == null || v2 == null) return null;
            
            var halfEdge = v1.HalfEdge;
            if (halfEdge == null) return null;
            
            do
            {
                if (halfEdge.Target == v2) return halfEdge;
                halfEdge = halfEdge.Twin.Next;
            } while (halfEdge != v1.HalfEdge);
            
            return null;
        }
    

        private void AddEdgeUnconnectedVertices(Vertex v1, Vertex v2)
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
            
            v1.HalfEdge = halfEdge1;
            v2.HalfEdge = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);
           
            Face face = CreateFace(halfEdge1, isBoundary: true);
            
            halfEdge1.FaceOnLeft = face;
            halfEdge2.FaceOnLeft = face;

        }
        
        private void AddEdgeOneConnectedVertex(Vertex connectedVertex, Vertex unconnectedVertex)
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
            v2.HalfEdge = halfEdge2;
            
            previousHalfEdge.Next = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;
            
            _halfEdges.Add(halfEdge1);
            _halfEdges.Add(halfEdge2);

        }
        
        private void AddEdgeTwoConnectedVertices(Vertex v1, Vertex v2)
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
                SetFace(halfEdge1, face1);
                Face newFace = CreateFace(halfEdge2, isBoundary: false);
                SetFace(halfEdge2, newFace);
            }
            else if (face1.IsBoundary && face2.IsBoundary) // if connecting two boundaries
            {
                if (face1 != face2) // Different boundaries are combined into one boundary
                {
                    _faces.Remove(face2.Id);
                    SetFace(halfEdge1, face1);
                }
                else // Same boundary => one inner face is created
                {
                    bool halfEdge1IsBoundary = GeometryUtils.IsClockwise(halfEdge1.Path());
                    if (halfEdge1IsBoundary)
                    {
                        halfEdge1.FaceOnLeft = face1;
                        var newInnerFace = CreateFace(halfEdge2, isBoundary: false);
                        SetFace(halfEdge2, newInnerFace);
                    }
                    else
                    {
                        halfEdge2.FaceOnLeft = face1;
                        var newInnerFace = CreateFace(halfEdge1, isBoundary: false);
                        SetFace(halfEdge1, newInnerFace);
                    }
                }
            }
            else if (face1.IsBoundary != face2.IsBoundary) // i.e. if one face is completely inside the other...
            {
                // We'll destroy the boundary face and replace it with the regular face
                if (face1.IsBoundary)
                {
                    _faces.Remove(face2.Id);
                    SetFace(halfEdge1, face1);
                }
                else
                {
                    _faces.Remove(face1.Id);
                    SetFace(halfEdge1, face2);
                }
                if (halfEdge1.FaceOnLeft != halfEdge2.FaceOnLeft) throw new InvalidOperationException("Something went wrong when setting faces.");
            }
        }
        
        
        private void InsertVertexIntoEdge(HalfEdge edge, Vertex newVertex)
        {
            HalfEdge newHalfEdge1 = new HalfEdge();
            HalfEdge newHalfEdge2 = new HalfEdge();
            (newHalfEdge1.Twin, newHalfEdge2.Twin) = (newHalfEdge2, newHalfEdge1);
            newVertex.HalfEdge = newHalfEdge1;
            edge.Target.HalfEdge = newHalfEdge2;
            
            
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

        private void DeleteEdge(HalfEdge edge)
        {
            bool onlyEdgeFromOrigin = edge.Previous == edge.Twin;

            if (onlyEdgeFromOrigin)
            {
                edge.Origin.HalfEdge = null;
            }
            else
            {
                edge.Previous.Next = edge.Twin.Next;
                edge.Twin.Next.Previous = edge.Previous;
                edge.Origin.HalfEdge = edge.Twin.Next;
            }
            
            bool onlyEdgeFromTarget = edge.Next == edge.Twin;
            if (onlyEdgeFromTarget)
            {
                edge.Target.HalfEdge = null;
            }
            else
            {
                edge.Twin.Previous.Next = edge.Next;
                edge.Next.Previous = edge.Twin.Previous;
                edge.Target.HalfEdge = edge.Next;
            }

            _halfEdges.Remove(edge); 
            _halfEdges.Remove(edge.Twin);


            if (!onlyEdgeFromOrigin)
            {
                SetFace(edge.Twin.Next, CreateFace(edge.Twin.Next, isBoundary: false)); // TEMP
            }
            if (!onlyEdgeFromTarget)
            {
                SetFace(edge.Next, CreateFace(edge.Next, isBoundary: false)); // TEMP
            }
            
            




        }
        
        // Returns next half edge CCW of origin -> target that is coming INTO the origin vertex 
        private HalfEdge FindPreviousHalfEdge(Vertex origin, Vertex target)
        {
            Vector2 direction = target.Position - origin.Position;
            if (origin.HalfEdge == null) return null;
            return EdgesOriginatingFrom(origin)
                .OrderBy(he => direction.AngleCounterClockwise(he.Target.Position - he.Origin.Position)) // CCW order
                .First() 
                .Twin; // HalfEdge coming into the origin vertex

        }

        /// <summary>
        /// Returns list of intersections between the line segment v1-v2 and the given edges.
        /// </summary>
        /// <returns>List of intersecting edges and intersection positions.
        /// Intersections are ordered by distance from v1 with the closest first.</returns>
        private List<(HalfEdge edge, Vector2 intersection)> FindIntersections(Vertex v1, Vertex v2,
            IEnumerable<HalfEdge> edges)
        {
            var result = new List<(HalfEdge edge, Vector2 intersectionPos)>();

            foreach (var edge in edges)
            {
                if (edge.Origin == v1 || edge.Origin == v2) continue;
                if (edge.Target == v1 || edge.Target == v2) continue;

                Vector2? intersection = Intersection.LineSegmentPosition(v1.Position, v2.Position,
                    edge.Origin.Position, edge.Target.Position);
                if (intersection == null) continue;

                var intersectionPoint = intersection.Value;
                result.Add((edge, intersectionPoint));
            }

            return result.OrderBy(i => Vector2.Distance(v1.Position, i.intersectionPos)).ToList();
        }
        
        /// <summary>
        /// Returns half edges that are inside the given rectangle (even if their endpoints are outside).
        /// Only return one of each half edge pair.
        /// </summary>
        private List<HalfEdge> EdgesIn(Rect rect)
        {
            //TODO: spatial partitioning
            
            var result = new List<HalfEdge>();
            foreach (var edge in _halfEdges)
            {
                if (edge.GetHashCode() < edge.Twin.GetHashCode()) continue;
                if (Intersection.LineSegmentRectangle(edge.Origin.Position, edge.Target.Position, rect))
                {
                    result.Add(edge);
                }
            }
            return result;
        }
        
        private List<HalfEdge> EdgesOriginatingFrom(Vertex vertex)
        {
            var result = new List<HalfEdge>();
            
            var halfEdge = vertex.HalfEdge;
            if (halfEdge == null) return result;
            int i = 0;
            do
            {
                i++;
                if (i > 1000) throw new InvalidOperationException("Infinite loop in EdgesOriginatingFrom");
                result.Add(halfEdge);
                halfEdge = halfEdge.Twin.Next;
            } while (halfEdge != vertex.HalfEdge);

            return result;
        }

        private HalfEdge FindHalfEdge(Vertex origin, Vertex target)
        {
           return EdgesOriginatingFrom(origin).FirstOrDefault(he => he.Target == target);
        }
        
        
        
        /// <summary>
        ///  Follows the path starting from the given half edge and sets the face for all of them.
        /// </summary>
        private void SetFace(HalfEdge edge, Face face)
        {
            HalfEdge current = edge;
            for (int i = 0; i < 10000; i++)
            {
                current.FaceOnLeft = face;
                current = current.Next;
                if (current == edge) return;
            }
            throw new InvalidOperationException("Infinite loop in SetFaceForPath");
        }

        private List<VertexId> VerticesOnFace(HalfEdge edge)
        {
            List<VertexId> result = new();
            HalfEdge current = edge;
            for (int i = 0; i < 10000; i++)
            {
                result.Add(current.Origin.Id);
                current = current.Next;
                if (current == edge) return result;
            }
            throw new InvalidOperationException("Infinite loop in SetFaceForPath");
        }
        
        
        private Face CreateFace(HalfEdge edge, bool isBoundary)
        {
            var face = new Face
            {
                Id = FaceId.New(),
                IsBoundary = isBoundary,
                HalfEdge = edge,
            };
           _faces.Add(face.Id, face);
           return face;
        }
        
        #endregion

        /*
        PointId AddPoint(Vector2 pos)
            - Vector2 PointPosition(PointId)
            - Pointid[] PointsIn(Rectangle rect)

            * EdgeId AddEdge(PointId p1, PointId p2)
            * PointId InsertPointInEdge(EdgeId edgeId, Vector2 pos) (exception jos intersections)
        * (PointId p1, PointId p2) Edge(EdgeId id)
            * EdgeId[] EdgesIn(Rectangle rect)
            * Splice
            *
            * FaceId FaceNextTo(EdgeId, Left/Right)
            * FaceId[] FacesAround(PointId)
            * FaceId[] NeighbourFaces(FaceId)
            *
            * GetPaths
            * Split
        * CheckIntegrity
            * NumIsolatedAreas
*/

 

    }
    
    
}