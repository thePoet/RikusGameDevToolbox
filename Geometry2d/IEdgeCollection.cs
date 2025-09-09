using System;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public interface IEdgeCollection
    {
        public VertexId AddVertex(Vector2 position);
        public void RemoveVertex(VertexId vertexId);
        public void AddEdge(VertexId vertexId1, VertexId vertexId2);
        public void RemoveEdge(VertexId vertexId1, VertexId vertexId2);
        public Vector2 Position(VertexId vertexId);
        public IEnumerable<VertexId> Vertices();
        public IEnumerable<(VertexId, VertexId)> All();
        public IEnumerable<VertexId> ConnectedVertices(VertexId vertexId);
        public int NumVertices { get; }
        public int NumEdges { get; }
        public void TransformVertices(System.Func<Vector2, Vector2> transformFunc);
        public IEdgeCollection MakeCopy(bool preserveVertexIds = true, Func<VertexId, bool> vertexIdFilter = null);
        public void Clear();
    }
    

}