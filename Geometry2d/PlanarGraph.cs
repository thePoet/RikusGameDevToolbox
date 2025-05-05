using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RikusGameDevToolbox.RTree;
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
           public List<Edge> Edges = new List<Edge>();
           public IEnumerable<Vertex> Connections => Edges.Select(e => e.VertexA == this ? e.VertexB : e.VertexA);
           public Edge EdgeConnectingTo(Vertex v) => Edges.FirstOrDefault(e => e.VertexA == v || e.VertexB == v);
           
           public override string ToString()
           {
                return $"Vertex: {Id} ({Position.x}, {Position.y})";
           }
       }

       private class Edge : ISpatialData
       {
           public Vertex VertexA { get; }
           public Vertex VertexB { get; }
           public Envelope Envelope { get; }
              
           public Edge(Vertex vertexA, Vertex vertexB)
           {
                VertexA = vertexA;
                VertexB = vertexB;
                Envelope = new Envelope(
                    minX: Mathf.Min(vertexA.Position.x, vertexB.Position.x),
                    minY: Mathf.Min(vertexA.Position.y, vertexB.Position.y),
                    maxX: Mathf.Max(vertexA.Position.x, vertexB.Position.x),
                    maxY: Mathf.Max(vertexA.Position.y, vertexB.Position.y)
                    );
           }

           public override string ToString()
           {
                return $"Edge: {VertexA} - {VertexB}";
           }
       }


        private readonly float _epsilon;
        private readonly SpatialCollection2d<Vertex> _vertices = new();
        private readonly Dictionary<VertexId, Vertex> _verticesById = new();
        private readonly RTree<Edge> _edgesSpatial = new();


        #region ----------------------------------- PUBLIC METHODS & PROPERTIES ----------------------------------------

        public int NumEdges => _edgesSpatial.Count;
        public int NumVertices => _verticesById.Count;
        public List<(VertexId, VertexId)> Edges => _edgesSpatial.All().Select(e => (e.VertexA.Id, e.VertexB.Id)).ToList();
        public List<VertexId> Vertices => _verticesById.Keys.ToList();

        public PlanarGraph(float epsilon = 0.0001f)
        {
            _epsilon = epsilon;
        }
        
        public void Clear()
        {
            _vertices.Clear();
            _verticesById.Clear();
            _edgesSpatial.Clear();
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
            
            /*
            Debug.Log("----------");
            foreach (var edge in _edges)
            {
                Debug.Log(edge);
            }*/

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
                    Vertex intersectionVertex = InsertVertexOnEdge(intersection.edge, intersection.intersectionPosition);
                   
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
            
            Edge edge = vertexA.EdgeConnectingTo(vertexB);
            if (edge==null) throw new ArgumentException("Edge not found");

            DeleteEdge(edge);
        }



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
            return EdgesIntersecting(rectangle).Select(edge => (edge.VertexA.Id, edge.VertexB.Id));
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
            
            _edgesSpatial.Insert(edge);

            OnAddEdge(vertexA.Id, vertexB.Id);
        }
        
        private void DeleteEdge(Edge edge)
        {
            edge.VertexA.Edges.Remove(edge);
            edge.VertexB.Edges.Remove(edge);
            _edgesSpatial.Delete(edge);
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
            if (!_edgesSpatial.Delete(oldEdge)) throw new InvalidOperationException("Failed to delete edge from spatial index");
            
            // First new edge
            Edge edge1 = new Edge(oldEdge.VertexA, newVertex);
            edge1.VertexA.Edges.Add(edge1);
            edge1.VertexB.Edges.Add(edge1);
            _edgesSpatial.Insert(edge1);
           
            // Second new edge
            Edge edge2 = new Edge(newVertex, oldEdge.VertexB);
            edge2.VertexA.Edges.Add(edge2);
            edge2.VertexB.Edges.Add(edge2);
            _edgesSpatial.Insert(edge2);
            
            OnSplitEdge(oldEdge.VertexA.Id, oldEdge.VertexB.Id, newVertex.Id);
            
            return newVertex;
        }

        
        private IEnumerable<Edge> EdgesIntersecting(Rect rectangle)
        {
            Envelope searchArea = new Envelope(
                minX: rectangle.xMin,
                minY: rectangle.yMin,
                maxX: rectangle.xMax,
                maxY: rectangle.yMax
            );
            return _edgesSpatial.Search(searchArea).Where( edge => IsEdgeIntersecting(edge, rectangle));
            
            bool IsEdgeIntersecting(Edge edge, Rect rect)
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
        
      

        #endregion
    }
}