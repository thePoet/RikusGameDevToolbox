using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using RBush;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public class EdgesSpatial : IEdgeSpatialCollection
    {
        private Edges _edges = new();
        private readonly SpatialCollection2d<Edges.Vertex> _vertices2d = new();
        private readonly RBush<Edges.Edge> _edges2d = new();
        

        public IEnumerable<VertexId> VerticesIn(Rect area)
        {
            return _vertices2d.ItemsInRectangle(area).Select(v => v.Id);
        }
        
        public IEnumerable<VertexId> VerticesInCircle(Vector2 center, float radius)
        {
            return _vertices2d.ItemsInCircle(center, radius).Select(v => v.Id);
        }

        public IEnumerable<(VertexId, VertexId)> EdgesIn(Rect area)
        {
            return EdgesIntersectingRect(area).Select(edge => (edge.VertexA.Id, edge.VertexB.Id));
        }

        public VertexId AddVertex(Vector2 position)
        {
             var id = _edges.AddVertex(position);
            _vertices2d.Add(position, _edges.GetVertex(id));
            return id;
        }

        public void RemoveVertex(VertexId vertexId)
        {
            var vertex = _edges.GetVertex(vertexId);
            _edges.RemoveVertex(vertexId);
            _vertices2d.Remove(vertex.Position, vertex);
        }

        public void AddEdge(VertexId vertexId1, VertexId vertexId2)
        {
            _edges.AddEdge(vertexId1, vertexId2);
            var edge = _edges.GetEdge(vertexId1, vertexId2);
            _edges2d.Insert(edge);
        }

        public void RemoveEdge(VertexId vertexId1, VertexId vertexId2)
        {
            var edge = _edges.GetEdge(vertexId1, vertexId2);
            _edges.RemoveEdge(vertexId1, vertexId2);
            _edges2d.Delete(edge);
        }

        public Vector2 Position(VertexId vertexId) => _edges.Position(vertexId);
        public IEnumerable<VertexId> Vertices() => _edges.Vertices();
        public IEnumerable<(VertexId, VertexId)> All() => _edges.All();
        public IEnumerable<VertexId> ConnectedVertices(VertexId vertexId) => _edges.ConnectedVertices(vertexId);
        public int NumVertices => _edges.NumVertices;
        public int NumEdges => _edges.NumEdges;

        public  void TransformVertices(Func<Vector2, Vector2> transformFunc)
        {
            _edges.TransformVertices(transformFunc);
            RebuildSpatialCollections();
        }

        public IEdgeCollection MakeCopy(bool preserveVertexIds = true, Func<VertexId, bool> vertexIdFilter = null)
        {
            var copy = new EdgesSpatial();
            copy._edges = _edges.MakeCopy(preserveVertexIds, vertexIdFilter) as Edges;
            copy.RebuildSpatialCollections();
            return copy;
        }

        public void Clear()
        {
            _edges.Clear();
            _vertices2d.Clear();
            _edges2d.Clear();
        }
     
        
        /// <summary>
        /// Rebuilds the spatial collections. This is needed after the vertices have been moved.
        /// </summary>
        private void RebuildSpatialCollections()
        {
            _vertices2d.Clear();
            var vertices = _edges.Vertices().Select(v => _edges.GetVertex(v));
            foreach (Edges.Vertex v in vertices)
            {
                _vertices2d.Add(v.Position, v);
            }
            
            var edges = _edges2d.All();
            _edges2d.Clear();
            foreach (Edges.Edge edge in edges)
            {
                edge.UpdateEnvelope();
                _edges2d.Insert(edge);
            }
        }
        
        /// <summary>
        /// Returns edges that are partially or fully inside the given rectangle.
        /// </summary>
        private IEnumerable<Edges.Edge> EdgesIntersectingRect(Rect rectangle)
        {
            Envelope searchArea = new Envelope(rectangle.xMin, rectangle.yMin, rectangle.xMax, rectangle.yMax);
            return _edges2d.Search(searchArea).Where( edge => IsEdgeIntersectingRect(edge, rectangle));
            
            bool IsEdgeIntersectingRect(Edges.Edge edge, Rect rect)
            {
                return Intersection.LineSegmentRectangle(edge.VertexA.Position, edge.VertexB.Position, rect);
            }
        }
    }
}