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
        
        /// <summary>
        /// Adds a line between two points. If the line intersects with existing edges, it will split them and add new
        /// vertices. If the line overlaps with existing edges, these will be incorporated into the line. 
        /// </summary>
        /// <returns>Returns all the vertices on the line. The first is the start and last one is the end.</returns>
        public List<VertexId> AddLine(Vector2 a, Vector2 b)
        {
            Vertex va = GetOrAddVertex(a);
            Vertex vb = GetOrAddVertex(b);
            var result = new List<VertexId>();
            
            var nearbyEdges = EdgesInside(RectAroundEdge(va,vb)).ToList();
          
            List<Vertex> verticesOnLine = VerticesOnLine(va, vb); // Find vertices that already exist on the line

            for (int i = 0; i < verticesOnLine.Count-1; i++)
            {
                Vertex v1 = verticesOnLine[i];
                Vertex v2 = verticesOnLine[i+1];
                
                if (v1==v2) throw new InvalidOperationException("Something went wrong when adding a line.");
                
                result.Add( verticesOnLine[i].Id );

                if (!IsEdgeBetween(v1.Id, v2.Id))
                {
                    var intersections = ConnectVertices(v1, v2);
                    if (intersections.Contains(v1.Id) || intersections.Contains(v2.Id)) throw new InvalidOperationException("Something went wrong when adding a line.");
                    result.AddRange(intersections);
                }
            }
            result.Add(verticesOnLine.Last().Id);

            return result;


            // Connect vertices with an edge and add new vertices in case of intersections with existing edges.
            // Returns the list of intersection vertices.
            List<VertexId> ConnectVertices(Vertex a, Vertex b)
            {
                List<VertexId> intersectionVertices = new();
                
                var intersections = FindIntersections(a, b, nearbyEdges.ToList());
                Vertex currentVertex = a;
                foreach (var intersection in intersections)
                {
                    Vertex intersectionVertex = InsertVertexOnEdge(intersection.Item1, intersection.Item2, intersection.Item3);
                    AddEdge(a, intersectionVertex);
                    currentVertex = intersectionVertex;
                    intersectionVertices.Add(intersectionVertex.Id);
                }
                AddEdge(currentVertex,b);
                return intersectionVertices;
            }
         
        }
        
        public void DeleteEdge(VertexId a, VertexId b)
        {
            Vertex vertexA = _verticesById.GetValueOrDefault(a);
            Vertex vertexB = _verticesById.GetValueOrDefault(b);
            if (vertexA == null || vertexB == null) throw new ArgumentException("Vertex not found");
            if (vertexA == vertexB) throw new ArgumentException("Same vertex when deleting edge");
            if (!vertexA.Connections.Contains(vertexB)) throw new ArgumentException("Edge not found");
            vertexA.Connections.Remove(vertexB);
            vertexB.Connections.Remove(vertexA);
            RemoveEdgeFromCollection(vertexA, vertexB);
            OnDeleteEdge(a,b);
        }

        public void DeleteVertex(VertexId id)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(id); 
            if (vertex == null) throw new ArgumentException("Vertex not found");
            var connections = vertex.Connections.ToList();
            foreach (var connection in connections)
            {
                DeleteEdge(id, connection.Id);
            }

            _verticesById.Remove(id);
            _vertices.Remove(vertex.Position, vertex);
            OnDeleteVertex(id);
        }

        public void DeleteVerticesWithoutEdges()
        {
            var toBeDeleted = _verticesById.Values
                .Where(v => v.Connections.Count == 0)
                .Select(v => v.Id)
                .ToList();

            foreach (var id in toBeDeleted)
            {
                DeleteVertex(id);
            }
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
        /// Return the vertices that are within epsilon of the line. The vertices are given in order
        /// and include the start and the end. 
        /// </summary>
        private List<Vertex> VerticesOnLine(Vertex start, Vertex end)
        {
            Rect searchArea = RectAroundEdge(start, end);

            var result = _vertices.ItemsInRectangle(searchArea)
                .Where(vertex => vertex != start && vertex != end &&
                                 GeometryUtils.IsPointOnEdge(vertex.Position, start.Position, end.Position, _epsilon))
                .OrderBy(v => Vector2.Distance(start.Position, v.Position))
                .ToList();

            result.Insert(0, start);
            result.Add(end);

            return result;
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
        
        private Rect RectAroundEdge(Vertex va, Vertex vb)
        {
            var rect = RectExtensions.CreateRectToEncapsulate(va.Position, vb.Position);
            rect = rect.Grow(2f*_epsilon);
            return rect;
        }
        
        // TODO: replace with a spatial collection
        private void RemoveEdgeFromCollection(Vertex vertex1, Vertex vertex2)
        {
            _edges.RemoveAll( e => e.Item1==vertex1 && e.Item2==vertex2 || e.Item1==vertex2 && e.Item2==vertex1);
        }

        #endregion
    }
}