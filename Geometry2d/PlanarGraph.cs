using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.RTree;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    /// <summary>
    /// This class represents a planar graph, which is a collection of vertices and edges in a 2D space. The edges
    /// never cross. If a new edge is added that intersects with existing edges, the existing edges are split and new
    /// vertices are created in the intersection points. The edges and vertices are stored in spatial structures for
    /// fast search.
    /// </summary>
    public class PlanarGraph
    {
    

        private class Vertex
        {
            public VertexId Id;
            public Vector2 Position;
            public List<Edge> Edges = new();
            public IEnumerable<Vertex> Connections => Edges.Select(e => e.VertexA == this ? e.VertexB : e.VertexA);
            public Edge EdgeConnectingTo(Vertex v) => Edges.FirstOrDefault(e => e.VertexA == v || e.VertexB == v);
            public override string ToString() => $"Vertex: {Id} ({Position.x}, {Position.y})";
        }

        private class Edge : ISpatialData
        {
            public Vertex VertexA { get; }
            public Vertex VertexB { get; }
            public Envelope Envelope { get; private set; }
            public override string ToString() => $"Edge: {VertexA} - {VertexB}";

            public Edge(Vertex vertexA, Vertex vertexB)
            {
                VertexA = vertexA;
                VertexB = vertexB;
                UpdateEnvelope();
            }
            
            public void UpdateEnvelope()
            {
                Envelope = new Envelope(RectExtensions.CreateRectToEncapsulate(VertexA.Position, VertexB.Position));
            }
        }

        private readonly float _epsilon;
        private readonly SpatialCollection2d<Vertex> _vertices = new();
        private readonly Dictionary<VertexId, Vertex> _verticesById = new();
        private readonly RTree<Edge> _edges = new();

        private Action<VertexId> _onAddVertex;
        private Action<VertexId, VertexId> _onAddEdge;
        private Action<VertexId, VertexId, VertexId> _onSplitEdge; 
        private Action<VertexId> _onDeleteVertex;
        private Action<VertexId, VertexId> _onDeleteEdge;

        #region ----------------------------------- PUBLIC METHODS & PROPERTIES ----------------------------------------

        public int NumEdges => _edges.Count;
        public int NumVertices => _verticesById.Count;
        public List<(VertexId, VertexId)> Edges => _edges.All().Select(e => (e.VertexA.Id, e.VertexB.Id)).ToList();
        public List<VertexId> Vertices => _verticesById.Keys.ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="epsilon">Vertices closer than epsilon are considered the same vertex.</param>
        /// <param name="onAddVertex">Action called after adding a vertex</param>
        /// <param name="onAddEdge">Action called after adding an edge</param>
        /// <param name="onSplitEdge">Action called after a vertex is inserted on an edge. First two parameters
        /// are verices of the edge. Note that in this case "onAddVertex" and "onAddEdge" are not called </param>
        /// <param name="onDeleteVertex">Action called after vertex is deleted</param>
        /// <param name="onDeleteEdge">Action called after edge is deleted</param>
        public PlanarGraph(float epsilon = 0.0001f,Action<VertexId> onAddVertex = null, Action<VertexId, 
                VertexId> onAddEdge = null, Action<VertexId, VertexId, VertexId> onSplitEdge = null, 
            Action<VertexId> onDeleteVertex = null, Action<VertexId, VertexId> onDeleteEdge = null)
        {
            _epsilon = epsilon;
            _onAddVertex = onAddVertex;
            _onAddEdge = onAddEdge;
            _onSplitEdge = onSplitEdge;
            _onDeleteVertex = onDeleteVertex;
            _onDeleteEdge = onDeleteEdge;
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
            
            var nearbyEdges = EdgesIntersecting(RectAroundEdge(va,vb)).ToList();
          
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
            List<VertexId> ConnectVertices(Vertex vertexA, Vertex vertexB)
            {
                List<VertexId> intersectionVertices = new();
                
                var intersections = FindIntersections(vertexA, vertexB, nearbyEdges.ToList());
                Vertex currentVertex = vertexA;
                foreach (var intersection in intersections)
                {
                    Vertex intersectionVertex = InsertVertexOnEdge(intersection.edge, intersection.intersectionPosition);
                   
                    AddEdge(vertexA, intersectionVertex);
                    currentVertex = intersectionVertex;
                    intersectionVertices.Add(intersectionVertex.Id);
                }
                AddEdge(currentVertex,vertexB);
                return intersectionVertices;
            }
         
        }




        public void DeleteEdge(VertexId a, VertexId b)
        {
            Vertex vertexA = _verticesById.GetValueOrDefault(a);
            Vertex vertexB = _verticesById.GetValueOrDefault(b);
            if (vertexA == null || vertexB == null) throw new ArgumentException("Vertex not found");
            if (vertexA == vertexB) throw new ArgumentException("Same vertex when deleting edge");
            
            Edge edge = vertexA.EdgeConnectingTo(vertexB);
            if (edge==null) throw new ArgumentException("Edge not found");

            DeleteEdge(edge);
        }

        /// <summary>
        /// Destroys the vertex and all edges connected to it.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void DeleteVertex(VertexId id)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(id); 
            if (vertex == null) throw new ArgumentException("Vertex not found");
           
            foreach (var edge in vertex.Edges.ToList())
            {
                DeleteEdge(edge);
            }

            _verticesById.Remove(id);
            _vertices.Remove(vertex.Position, vertex);
            OnDeleteVertex(id);
        }

        public void DeleteVerticesWithoutEdges()
        {
            var toBeDeleted = _verticesById.Values
                .Where(v => v.Edges.Count == 0)
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
            return vertexA.EdgeConnectingTo(vertexB) != null;
        }
        
        /// <summary>
        /// Returns vertices that are connected with edge to the given vertex.
        /// </summary>
        /// <exception cref="ArgumentException">If vertex does not exist.</exception>
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
        
        /// <summary>
        /// Vertices inside the rectangle
        /// </summary>
        public IEnumerable<VertexId> VerticesIn(Rect rectangle)
        {
            return _vertices.ItemsInRectangle(rectangle).Select(v => v.Id);
        }

        /// <summary>
        /// Returns edges that are intersecting the rectangle or completely inside it.
        /// </summary>
        public IEnumerable<(VertexId, VertexId)> EdgesIn(Rect rectangle)
        {
            return EdgesIntersecting(rectangle).Select(edge => (edge.VertexA.Id, edge.VertexB.Id));
        }

        public virtual void TransformVertices(Func<Vector2, Vector2> transformFunction)
        {
            foreach (Vertex v in _verticesById.Values)
            {
                v.Position = transformFunction(v.Position);
            }
            
            RebuildSpatialCollections();
        }

        
        #endregion
        #region ------------------------------------------ PROTECTED METHODS ----------------------------------------------

        
        protected virtual void OnAddVertex(VertexId vertex)
        {
            if (_onAddVertex != null) _onAddVertex(vertex);
        } 
        protected virtual void OnAddEdge(VertexId vertexA, VertexId vertexB)
        {
            if (_onAddEdge != null) _onAddEdge(vertexA, vertexB);
        }
        protected virtual void OnSplitEdge(VertexId vertexA, VertexId vertexB, VertexId newVertex)
        {
            if (_onSplitEdge != null) _onSplitEdge(vertexA, vertexB, newVertex);
        }
        protected virtual void OnDeleteVertex(VertexId vertex)
        {
            if (_onDeleteVertex != null) _onDeleteVertex(vertex);
        }
        protected virtual void OnDeleteEdge(VertexId vertexA, VertexId vertexB)
        {
            if (_onDeleteEdge != null) _onDeleteEdge(vertexA, vertexB);
        }
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private Vertex GetOrAddVertex(Vector2 position)
        {
            var existingVertices = _vertices.ItemsInRectangle(EpsilonRect(position));
            if (existingVertices.Any()) return existingVertices.First();

           var edges = EdgesIntersecting(EpsilonRect(position)).ToList();
           if (edges.Any())
           {
               return InsertVertexOnEdge(edges.First(), position);
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
            if (vertexA.EdgeConnectingTo(vertexB) != null) return; // Edge already exists
            
            Edge edge = new Edge(vertexA, vertexB);
            vertexA.Edges.Add(edge);
            vertexB.Edges.Add(edge);
            
            _edges.Insert(edge);

            OnAddEdge(vertexA.Id, vertexB.Id);
        }
        
        private void DeleteEdge(Edge edge)
        {
            edge.VertexA.Edges.Remove(edge);
            edge.VertexB.Edges.Remove(edge);
            _edges.Delete(edge);
            OnDeleteEdge(edge.VertexA.Id, edge.VertexB.Id);
        }
        
        private Vertex InsertVertexOnEdge(Edge edge, Vector2 position)
        {
            var oldEdge = edge;
            // Set the position exactly on the edge:
            position = GeometryUtils.ProjectPointOnEdge(position, oldEdge.VertexA.Position, oldEdge.VertexB.Position);
           
            var newVertex = new Vertex
            {
                Id = VertexId.New(),
                Position = position
            };
            _vertices.Add(newVertex.Position, newVertex);
            _verticesById[newVertex.Id] = newVertex;
            
            oldEdge.VertexA.Edges.Remove(oldEdge);
            oldEdge.VertexB.Edges.Remove(oldEdge);
            if (!_edges.Delete(oldEdge)) throw new InvalidOperationException("Failed to delete edge from spatial index");
            
            // First new edge
            Edge edge1 = new Edge(oldEdge.VertexA, newVertex);
            edge1.VertexA.Edges.Add(edge1);
            edge1.VertexB.Edges.Add(edge1);
            _edges.Insert(edge1);
           
            // Second new edge
            Edge edge2 = new Edge(newVertex, oldEdge.VertexB);
            edge2.VertexA.Edges.Add(edge2);
            edge2.VertexB.Edges.Add(edge2);
            _edges.Insert(edge2);
            
            OnSplitEdge(oldEdge.VertexA.Id, oldEdge.VertexB.Id, newVertex.Id);
            
            return newVertex;
        }

        /// <summary>
        /// Returns edges that are intersecting the rectangle or contained wholly in it.
        /// </summary>
        private IEnumerable<Edge> EdgesIntersecting(Rect rectangle)
        {
            Envelope searchArea = new Envelope(rectangle);
            return _edges.Search(searchArea).Where( edge => IsEdgeIntersectingRect(edge, rectangle));
            
            bool IsEdgeIntersectingRect(Edge edge, Rect rect)
            {
                return Intersection.LineSegmentRectangle(edge.VertexA.Position, edge.VertexB.Position, rect);
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
        private List<(Edge edge, Vector2 intersectionPosition)> FindIntersections(Vertex v1, Vertex v2, List<Edge> edges)
        {
            var result = new List<(Edge, Vector2 intersectionPos)>();

            foreach (var edge in edges)
            {
                if (edge.VertexA == v1 || edge.VertexA == v2) continue;
                if (edge.VertexB == v1 || edge.VertexB == v2) continue;

                Vector2? intersection = Intersection.LineSegmentPosition(v1.Position, v2.Position,
                    edge.VertexA.Position, edge.VertexB.Position);
                if (intersection == null) continue;

                var intersectionPoint = intersection.Value;
                result.Add((edge, intersectionPoint));
            }

            return result.OrderBy(i => Vector2.Distance(v1.Position, i.intersectionPos)).ToList();
        }

        // Returns rectangle with side length of 2 * _epsilon with given coordinate as center:
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
        
        /// <summary>
        /// Rebuilds the spatial collections. This is needed after the vertices have been moved.
        /// </summary>
        private void RebuildSpatialCollections()
        {
            _vertices.Clear();
            foreach (Vertex v in _verticesById.Values)
            {
                _vertices.Add(v.Position, v);
            }
            
            var edges = _edges.All();
            _edges.Clear();
            foreach (Edge edge in edges)
            {
                edge.UpdateEnvelope();
                _edges.Insert(edge);
            }
         
        }
      

        #endregion
    }
}