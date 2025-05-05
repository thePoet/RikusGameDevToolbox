using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarGraph
    {
        public record VertexId(Guid Value) { public static VertexId New() => new(Guid.NewGuid()); }
     
       private class Vertex
       {
           public VertexId Id;
           public Vector2 Position;
           public readonly List<Vertex> Connections = new();
       }


        private readonly float _epsilon;
        private readonly SpatialCollection2d<Vertex> _vertices = new();
        private readonly Dictionary<VertexId, Vertex> _verticesById = new();
        private readonly List<(Vertex, Vertex)> _edges = new(); // TODO: replace with a spatial collection




        #region ----------------------------------- PUBLIC METHODS & PROPERTIES ----------------------------------------

        public int NumEdges => _edges.Count;
        public int NumVertices => _verticesById.Count;
        public List<(Vector2, Vector2)> Edges => _edges.Select(e => (e.Item1.Position, e.Item2.Position)).ToList();
        public List<Vector2> Vertices => _verticesById.Values.Select(v => v.Position).ToList();

        public PlanarGraph(float epsilon = 0.0001f)
        {
            _epsilon = epsilon;
        }
        
        public void Clear()
        {
            _vertices.Clear();
            _verticesById.Clear();
            _edges.Clear();
        }        
        
        public void AddLine(Vector2 a, Vector2 b)
        {
            Vertex va = GetOrAddVertex(a);
            Vertex vb = GetOrAddVertex(b);
            
            var nearbyEdges = EdgesInside(BoundingRect()).ToList();
            
            foreach (var edge in nearbyEdges)
            {
                var result = Overlap(va, vb, edge.Item1, edge.Item2);
                if (result.isOverlapping)
                {
                    if (result.nonOverlappingEdges == null) return;
                    foreach (var edgePair in result.nonOverlappingEdges)
                    {
                        AddLine(edgePair.Item1.Position, edgePair.Item2.Position);
                    }
                    return;
                }
            }

            var intersections = FindIntersections(va, vb, nearbyEdges.ToList());
            
            var currentVertex = va;
            foreach (var intersection in intersections)
            {
                Vertex newVertex = InsertVertexOnEdge(intersection.Item1, intersection.Item2, intersection.Item3);
                AddEdge(currentVertex, newVertex);
                currentVertex = newVertex;
            }

            AddEdge(currentVertex,vb);
            return;
            
            Rect BoundingRect()
            {
                var rect = RectExtensions.CreateRectToEncapsulate(a, b);
                rect = rect.Grow(_epsilon);
                return rect;
            }
        }
        
        public void DeleteEdge(VertexId a, VertexId b)
        {
            
        }
        
        public Vector2 Position(VertexId vertexId)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(vertexId);
            if (vertex == null) throw new ArgumentException("Vertex not found");
            return vertex.Position;
        }
        
        public bool IsEdgeBetween(VertexId a, VertexId b)
        {
            Vertex vertexA = _verticesById.GetValueOrDefault(a);
            Vertex vertexB = _verticesById.GetValueOrDefault(b);
            if (vertexA == null || vertexB == null) throw new ArgumentException("Vertex not found");
            return vertexA.Connections.Contains(vertexB);
        }
        
        public IEnumerable<VertexId> EdgesOfVertex(VertexId vertexId)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(vertexId);
            if (vertex == null) throw new ArgumentException("Vertex not found");
            return vertex.Connections.Select(v => v.Id);
        }
        
        
        /// <summary>
        /// Return the vertex at given position or within distance epsilon of it. Returns null if no such vertex exists.
        /// </summary>
        public VertexId VertexAt(Vector2 position)
        {
            var existingVertices = _vertices.ItemsInRectangle(EpsilonRect(position));
            if (existingVertices.Any()) return existingVertices.First().Id;
            return null;
        }
        
        public IEnumerable<VertexId> VerticesIn(Rect rectangle)
        {
            return _vertices.ItemsInRectangle(rectangle).Select(v => v.Id);
        }

        public IEnumerable<(VertexId, VertexId)> EdgesIn(Rect rectangle)
        {
            return EdgesInside(rectangle)
                .Select(edge => (edge.Item1.Id, edge.Item2.Id));
        }

        #endregion
        #region ------------------------------------------ PROTECTED METHODS ----------------------------------------------

        
        protected void OnAddVertex(VertexId vertex)
        {
        }
        protected void OnAddEdge(VertexId vertexA, VertexId vertexB)
        {
        }
        protected void OnSplitEdge(VertexId vertexA, VertexId vertexB, VertexId newVertex)
        {
        }
        protected void OnDeleteVertex(VertexId vertex)
        {
        }
        protected void OnDeleteEdge(VertexId vertexA, VertexId vertexB)
        {
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private Vertex GetOrAddVertex(Vector2 position)
        {
            var existingVertices = _vertices.ItemsInRectangle(EpsilonRect(position));
            if (existingVertices.Any()) return existingVertices.First();

           var edges = EdgesInside(EpsilonRect(position)).ToList();
           if (edges.Any())
           {
               return InsertVertexOnEdge(edges.First().Item1, edges.First().Item2, position);
           }

            var newVertex = new Vertex
            {
                Id = VertexId.New(),
                Position = position
            };
            _vertices.Add(newVertex.Position, newVertex);
            _verticesById[newVertex.Id] = newVertex;
            OnAddVertex(newVertex.Id);
            return newVertex;
          
        }

     
        private void AddEdge(Vertex vertexA, Vertex vertexB)
        {
            if (vertexA == vertexB) return;
            if (vertexA.Connections.Contains(vertexB)) return; // Edge already exists
            
            vertexA.Connections.Add(vertexB);
            vertexB.Connections.Add(vertexA);
            _edges.Add((vertexA, vertexB));

            OnAddEdge(vertexA.Id, vertexB.Id);
        }
        
        private Vertex InsertVertexOnEdge(Vertex vertexA, Vertex vertexB, Vector2 position)
        {
            // Set the position exactly on the edge:
            position = GeometryUtils.ProjectPointOnEdge(position, vertexA.Position, vertexB.Position);
           
            var newVertex = new Vertex
            {
                Id = VertexId.New(),
                Position = position
            };
            _vertices.Add(newVertex.Position, newVertex);
            _verticesById[newVertex.Id] = newVertex;
           
            RemoveEdgeFromCollection(vertexA, vertexB);

            vertexA.Connections.Remove(vertexB);
            vertexB.Connections.Remove(vertexA);
            vertexA.Connections.Add(newVertex);
            vertexB.Connections.Add(newVertex);
            newVertex.Connections.Add(vertexA);
            newVertex.Connections.Add(vertexB);
            
            _edges.Add((vertexA, newVertex));
            _edges.Add((vertexB, newVertex));
            
            OnSplitEdge(vertexA.Id, vertexB.Id, newVertex.Id);
            
            return newVertex;

            
            void RemoveEdgeFromCollection(Vertex vertex1, Vertex vertex2)
            {
                _edges.RemoveAll( e => e.Item1==vertex1 && e.Item2==vertex2 || e.Item1==vertex2 && e.Item2==vertex1);
            }

        }

        
        private IEnumerable<(Vertex, Vertex)> EdgesInside(Rect rectangle)
        {
            foreach (var edge in _edges)
            {
                if (Intersection.LineSegmentRectangle(edge.Item1.Position, edge.Item2.Position, rectangle))
                {
                    yield return (edge.Item1, edge.Item2);
                }
            }
        }
        
        
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
            
            var (v1P, v2P) = (v1.Position, v2.Position);
            var (e1P, e2P) = (edgeV1.Position, edgeV2.Position);
            bool isV1OnEdge = GeometryUtils.IsPointOnEdge(v1P, e1P, e2P, _epsilon);
            bool isV2OnEdge = GeometryUtils.IsPointOnEdge(v2P, e1P, e2P, _epsilon);

        
            
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
        
        /// <summary>
        /// Returns list of intersections between the line segment v1-v2 and the given edges.
        /// </summary>
        /// <returns>List of intersecting edges and intersection positions.
        /// Intersections are ordered by distance from v1 with the closest first.</returns>
        private List<(Vertex, Vertex, Vector2)> FindIntersections(Vertex v1, Vertex v2, List<(Vertex, Vertex)> edges)
        {
            var result = new List<(Vertex, Vertex, Vector2 intersectionPos)>();

            foreach (var edge in edges)
            {
                if (edge.Item1 == v1 || edge.Item1 == v2) continue;
                if (edge.Item2 == v1 || edge.Item2 == v2) continue;

                Vector2? intersection = Intersection.LineSegmentPosition(v1.Position, v2.Position,
                    edge.Item1.Position, edge.Item2.Position);
                if (intersection == null) continue;

                var intersectionPoint = intersection.Value;
                result.Add((edge.Item1, edge.Item2, intersectionPoint));
            }

            return result.OrderBy(i => Vector2.Distance(v1.Position, i.intersectionPos)).ToList();
        }

        // Returns rectangle with side lenght of 2 * _epsilon with given coordinate as center:
        private Rect EpsilonRect(Vector2 centerPosition)
        {
            return new Rect(centerPosition.x - _epsilon, centerPosition.y - _epsilon, _epsilon*2f, _epsilon*2f);
        }
        
        
        #endregion
    }
}