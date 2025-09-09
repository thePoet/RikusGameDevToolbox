
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public interface IEdgeSpatialCollection : IEdgeCollection
    {
        public IEnumerable<VertexId> VerticesIn(Rect area);
        public IEnumerable<VertexId> VerticesInCircle(Vector2 center, float radius);
        public IEnumerable<(VertexId, VertexId)> EdgesIn(Rect area);
 //       public override IEdgeSpatialCollection MakeCopy(bool preserveVertexIds = true, Func<VertexId, bool> vertexIdFilter = null);
    }
}