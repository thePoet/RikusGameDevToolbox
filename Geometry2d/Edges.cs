using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RBush;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class Edges : IEdgeCollection
    {
        internal class Vertex
        {
            public VertexId Id;
            public Vector2 Position;
            public List<Edge> Edges = new();
            public IEnumerable<Vertex> Connections => Edges.Select(e => e.VertexA == this ? e.VertexB : e.VertexA);
            public Edge EdgeConnectingTo(Vertex v) => Edges.FirstOrDefault(e => e.VertexA == v || e.VertexB == v);
            public override string ToString() => $"Vertex: {Id} ({Position.x}, {Position.y})";
        }

        internal class Edge : ISpatialData
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
                Rect rect = RectExtensions.CreateRectToEncapsulate(VertexA.Position, VertexB.Position);
                Envelope = new Envelope(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            }
        }
        
        private readonly Dictionary<VertexId, Vertex> _verticesById = new();
        private readonly HashSet<Edge> _edgeSet = new();
        
        public int NumVertices => _verticesById.Count;
        public int NumEdges => _edgeSet.Count;
        
        public VertexId AddVertex(Vector2 position)
        {
            if (ContainsNan(position)) throw new ArgumentException("Tried to add vertex with NaN position");

            var vertex = new Vertex
            {
                Id = VertexId.New(),
                Position = position
            };
            _verticesById[vertex.Id] = vertex;
            return vertex.Id;

            bool ContainsNan(Vector2 v) => float.IsNaN(v.x) || float.IsNaN(v.y);
        }

        public void RemoveVertex(VertexId vertexId)
        {
            Vertex vertex = _verticesById.GetValueOrDefault(vertexId); 
            if (vertex == null) throw new ArgumentException("Vertex not found");
            if (vertex.Edges.Count > 0) throw new InvalidOperationException("Cannot delete vertex with edges.");
            _verticesById.Remove(vertex.Id);
        }

        public void AddEdge(VertexId vertexId1, VertexId vertexId2)
        {
            var (vertexA, vertexB) = (GetVertex(vertexId1), GetVertex(vertexId2));
            if (vertexA == vertexB) throw new ArgumentException("Vertex A and Vertex B cannot be the same");
            if (vertexA.EdgeConnectingTo(vertexB) != null) throw new ArgumentException("Edge already exists");
            
            Edge edge = new Edge(vertexA, vertexB);
            vertexA.Edges.Add(edge);
            vertexB.Edges.Add(edge);
            _edgeSet.Add(edge);
        }

        public void RemoveEdge(VertexId vertexId1, VertexId vertexId2)
        {
            var edge = GetEdge(vertexId1, vertexId2);
            _edgeSet.Remove(edge);
            edge.VertexA.Edges.Remove(edge);
            edge.VertexB.Edges.Remove(edge);
        }

        public Vector2 Position(VertexId vertexId)
        {
            return GetVertex(vertexId).Position;
        }

        public IEnumerable<VertexId> Vertices()
        {
            return _verticesById.Keys;
        }

        public IEnumerable<(VertexId, VertexId)> All()
        {
            return _edgeSet.Select(edge => (edge.VertexA.Id, edge.VertexB.Id));
        }

        public IEnumerable<VertexId> ConnectedVertices(VertexId vertexId)
        {
            return GetVertex(vertexId).Connections.Select(v => v.Id);
        }
        
        public void TransformVertices(Func<Vector2, Vector2> transformFunc)
        {
            foreach (Vertex v in _verticesById.Values)
            {
                v.Position = transformFunc(v.Position);
            }
        }

        public IEdgeCollection MakeCopy(bool preserveVertexIds = true, Func<VertexId, bool> vertexIdFilter = null)
        {
            Edges copy = new Edges();
            
            Dictionary<Vertex, Vertex> vertexLookup = new Dictionary<Vertex, Vertex>();
            foreach (Vertex v in _verticesById.Values.Where(IsVertexIncludedInCopy))
            {
                Vertex vertexCopy = new Vertex
                {
                    Id = preserveVertexIds ? v.Id : VertexId.New(),
                    Position = v.Position
                };
                vertexLookup[v] = vertexCopy;
                copy._verticesById[vertexCopy.Id] = vertexCopy;
            }

            foreach (Edge e in _edgeSet.Where(e => IsVertexIncludedInCopy(e.VertexA) && IsVertexIncludedInCopy(e.VertexB)))
            {
                Edge edgeCopy = new Edge(vertexLookup[e.VertexA], vertexLookup[e.VertexB]);
                edgeCopy.VertexA.Edges.Add(edgeCopy);
                edgeCopy.VertexB.Edges.Add(edgeCopy);
                copy._edgeSet.Add(edgeCopy);
            }

            return copy;
            
            bool IsVertexIncludedInCopy(Vertex v) => vertexIdFilter == null || vertexIdFilter(v.Id);
          
        }

        public void Clear()
        {
            _verticesById.Clear();
            _edgeSet.Clear();
        }
        
        internal Edge GetEdge(VertexId vertexId1, VertexId vertexId2)
        {
            var (vertexA, vertexB) = (GetVertex(vertexId1), GetVertex(vertexId2));
            var edge =  vertexA.EdgeConnectingTo(vertexB);
            if (edge == null) throw new ArgumentException("Edge not found");
            return edge;
        }
        
        internal Vertex GetVertex(VertexId vertexId)
        {
            var vertex = _verticesById.GetValueOrDefault(vertexId);
            if (vertex == null) throw new ArgumentException("Vertex not found");
            return vertex;
        }
    }
}