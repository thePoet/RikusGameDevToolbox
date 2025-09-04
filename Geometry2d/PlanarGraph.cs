using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;
using static RikusGameDevToolbox.Geometry2d.Util;

namespace RikusGameDevToolbox.Geometry2d
{
    /// <summary>
    /// This class represents a planar graph, which is a collection of vertices and edges in a 2D space. The edges
    /// never cross. If a new edge is added that intersects with existing edge, the edges are split and new
    /// vertices are created in the intersection points. The edges and vertices are stored in spatial structures for
    /// fast search.
    /// </summary>
    public class PlanarGraph
    {
        public readonly float Epsilon;
        private IEdgeSpatialCollection _edges = new EdgesSpatial();

        #region ----------------------------------- PUBLIC METHODS & PROPERTIES ----------------------------------------

        public int NumEdges => _edges.NumEdges;
        public int NumVertices => _edges.NumVertices;
        public IEnumerable<(VertexId v1, VertexId v2)> Edges => _edges.All();
        public IEnumerable<(Vector2 v1, Vector2 v2)> EdgeVertexPositions => _edges.All().Select(e => (_edges.Position(e.Item1), _edges.Position(e.Item2)));
        public IEnumerable<VertexId> Vertices => _edges.Vertices();
        public void Clear() => _edges.Clear();
        public Vector2 Position(VertexId vertexId) => _edges.Position(vertexId);
        public VertexId? VertexAt(Vector2 position) => _edges.VerticesInCircle(position, Epsilon).FirstOrDefault();
        public IEnumerable<VertexId> VerticesIn(Rect area) => _edges.VerticesIn(area);
        public void DeleteEdge(VertexId v1, VertexId v2) => _edges.RemoveEdge(v1, v2);
        public bool IsEdgeBetween(VertexId a, VertexId b) => _edges.ConnectedVertices(a).Contains(b);
        public void TransformVertices(Func<Vector2, Vector2> transformFunction) => _edges.TransformVertices(transformFunction);
        public IEnumerable<VertexId> ConnectedVertices(VertexId vertexId) => _edges.ConnectedVertices(vertexId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="epsilon">Vertices closer than epsilon are considered the same vertex.</param>
        public PlanarGraph(float epsilon = 0.0001f)
        {
            Epsilon = epsilon;
        }
        
        /// <summary>
        /// Adds a line between two points. If the line intersects with existing edges, it will split them and add new
        /// vertices. If the line overlaps with existing edges, these will be incorporated into the line. 
        /// </summary>
        /// <returns>Returns all the vertices on the line. The first is the start and last one is the end.</returns>
        public List<VertexId> AddLine(Vector2 a, Vector2 b, 
            Action<VertexId>                      onAddVertex  = null, 
            Action<VertexId, VertexId>            onAddEdge    = null, 
            Action<VertexId, VertexId, VertexId>  onSplitEdge  = null) 
           // Action<VertexId, VertexId>            onDeleteEdge = null) // TODO: would be nice to get rid of this, see GetOrAddVertex
        {
            VertexId va = GetOrAddVertex(a, onAddVertex, onSplitEdge);
            VertexId vb = GetOrAddVertex(b, onAddVertex, onSplitEdge);

            if (va.Equals(vb)) return new List<VertexId>{va};

            var result = new List<VertexId>();
            var nearbyEdges = _edges.EdgesIn( RectAroundEdge(va,vb)).ToList();
          
            List<VertexId> verticesOnLine = VerticesOnLine(va, vb); // Find vertices that already exist on the line

            foreach (var (v1, v2) in Pairs(verticesOnLine) )
            {
                if (v1.Equals(v2)) throw new InvalidOperationException("Something went wrong when adding a line.");
                
                result.Add( v1 );

                if (!IsEdgeBetween(v1, v2))
                {
                    var intersections = ConnectVertices(v1, v2);
                    if (intersections.Contains(v1) || intersections.Contains(v2)) throw new InvalidOperationException("Something went wrong when adding a line.");
                    result.AddRange(intersections);
                }
            }
            result.Add(verticesOnLine.Last());
            
            return result;

            
      
            // Return the vertices that are within epsilon of the line. The vertices are given in order
            // and include the start and the end. 
            List<VertexId> VerticesOnLine(VertexId start, VertexId end)
            {
                Rect searchArea = RectAroundEdge(start, end);
                Vector2 startPos = _edges.Position(start);
                Vector2 endPos = _edges.Position(end);

                var result = _edges.VerticesIn(searchArea)
                    .Where(vertex => !vertex.Equals(start) && !vertex.Equals(end) &&
                                     GeometryUtils.IsPointOnEdge(_edges.Position(vertex), startPos, endPos, Epsilon))
                    .OrderBy(v => Vector2.Distance(startPos, _edges.Position(v)))
                    .ToList();

                result.Insert(0, start);
                result.Add(end);

                return result;
            }

            // Connect vertices with an edge and add new vertices in case of intersections with existing edges.
            // Returns the list of intersection vertices.
            List<VertexId> ConnectVertices(VertexId vertexA, VertexId vertexB)
            {
                if (vertexA.Equals(vertexB)) throw new InvalidOperationException("Tried to connect vertex with itself.");
                
                List<VertexId> intersectionVertices = new();
                
                var intersections = FindIntersections(vertexA, vertexB, nearbyEdges.ToList());
                VertexId currentVertex = vertexA;
                foreach (var (edge, intersectionPos)  in intersections)
                {
                    VertexId v1 = edge.Item1;
                    VertexId v2 = edge.Item2;
                   
                    VertexId intersectionVertex = InsertOrGetVertexOnEdge(v1, v2, intersectionPos, onSplitEdge);
                   
                    _edges.AddEdge(currentVertex, intersectionVertex);
                    onAddEdge?.Invoke(currentVertex, intersectionVertex);
                  
                    currentVertex = intersectionVertex;
                    intersectionVertices.Add(intersectionVertex);
                }
                _edges.AddEdge(currentVertex,vertexB);
                onAddEdge?.Invoke(currentVertex, vertexB);
               
                return intersectionVertices;
            }
        }




        /// <summary>
        /// Destroys the vertex and all edges connected to it.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void DeleteVertex(VertexId vertex) // Action on tyhmä tässä
        {
            _edges.RemoveVertex(vertex);
        }
        
        public void DeleteVertexAndConnectedEdges(VertexId vertex, Action<VertexId, VertexId> onDeleteEdge = null)
        {
            foreach (var connected in _edges.ConnectedVertices(vertex).ToList())
            {
                DeleteEdge(vertex, connected);
                onDeleteEdge?.Invoke(vertex, connected);
            }
            _edges.RemoveVertex(vertex);
        }

        public void DeleteVerticesWithoutEdges(Action<VertexId> onDeleteVertex = null)
        {
            var toBeDeleted = _edges.Vertices()
                .Where(v =>  !_edges.ConnectedVertices(v).Any())
                .ToList();

            foreach (var id in toBeDeleted)
            {
                _edges.RemoveVertex(id);
                onDeleteVertex?.Invoke(id);
            }
        }
        

        /// <summary>
        /// Makes a deep copy of the planar graph.
        /// </summary>
        /// <param name="preserveVertexIds"></param>
        /// <param name="vertexIdFilter">Function that takes VertexId as parameter that returns true if the vertex is to be copied. </param>
        public PlanarGraph MakeDeepCopy(bool preserveVertexIds = true, Func<VertexId, bool> vertexIdFilter = null)
        {
            return new PlanarGraph(Epsilon)
            {
                _edges = _edges.MakeCopy(preserveVertexIds, vertexIdFilter) as EdgesSpatial
            };
        }
      
        
        #endregion


        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private VertexId GetOrAddVertex(Vector2 position, Action<VertexId> onAddVertex, Action<VertexId, VertexId, VertexId> onSplitEdge)
        {
            var existing = _edges.VerticesInCircle(position, Epsilon);
            if (existing.Any()) return existing.First();


            var edges = EdgesWithinEpsilon(position);
            if (edges.Any())
            {
                var insertedVertex = InsertOrGetVertexOnEdge(edges.First().v1, edges.First().v2, position, onSplitEdge);
      
                // Following code cause trouble in PlanarDivision, so I disabled it. This however means that we may have vertices 
                // closer to edges than epsilon, which may or may not be a problem.
                /*
                foreach (var edge in edges.Skip(1))
                {
                    if (insertedVertex.Equals(edge.v1) || insertedVertex.Equals(edge.v2)) continue;
                    
                    // This may cause some trouble in PlanarDivision because we delete and add instead of splitting the edge.
                    _edges.RemoveEdge(edge.v1, edge.v2);
                    onDeleteEdge?.Invoke(edge.v1, edge.v2);

                    if (!IsEdgeBetween(edge.v1, insertedVertex))
                    {
                        _edges.AddEdge(edge.v1, insertedVertex);
                        onAddEdge?.Invoke(edge.v1, insertedVertex);
                    }
                    if (!IsEdgeBetween(edge.v2, insertedVertex))
                    {
                        _edges.AddEdge(edge.v2, insertedVertex);
                        onAddEdge?.Invoke(edge.v2, insertedVertex);
                    }
                }*/
                return insertedVertex;
            }

            var newVertex = _edges.AddVertex(position);
            onAddVertex?.Invoke(newVertex);
            return newVertex;
        }



        private VertexId InsertOrGetVertexOnEdge(VertexId v1, VertexId v2, Vector2 position, Action<VertexId, VertexId, VertexId> onSplitEdge)
        {
            if (v1.Equals(v2)) throw new ArgumentException("Tried to insert vertex on edge with same vertices");
            
        
            var positionExactlyOnEdge = GeometryUtils.ProjectPointOnEdge(position, _edges.Position(v1), _edges.Position(v2));

            // If the position is close to one of the edge vertices, return that vertex.
            var existing = _edges.VerticesInCircle(positionExactlyOnEdge, Epsilon).ToList();
            if (existing.Any())
            {
                if (existing.First().Equals(v1) || existing.First().Equals(v2))
                {
                    // If the vertex is already one of the edge vertices, return it.
                    return existing.First();
                }
                throw new InvalidOperationException("Vertex already exists at the position: " + positionExactlyOnEdge);
            }


            var newVertex = _edges.AddVertex(positionExactlyOnEdge);
            _edges.RemoveEdge(v1,v2);
            _edges.AddEdge(v1, newVertex);
            _edges.AddEdge(newVertex, v2);
            onSplitEdge?.Invoke(v1, v2, newVertex);
            
            return newVertex;
        }

        


        private List<(VertexId v1, VertexId v2)> EdgesWithinEpsilon(Vector2 position)
        {
            // Rect with side length of 2 * _epsilon with given position as center:
            var smallRect = new Rect(position.x - Epsilon, position.y - Epsilon, Epsilon*2f, Epsilon*2f);
            
            return _edges.EdgesIn(smallRect)
                .Where(edge => GeometryUtils.IsPointOnEdge(position,_edges.Position(edge.Item1),_edges.Position(edge.Item2), Epsilon))
                .ToList();
       }
    
        /// <summary>
        /// Returns list of intersections between the line segment v1-v2 and the given edges.
        /// </summary>
        /// <returns>List of intersecting edges and intersection positions.
        /// Intersections are ordered by distance from v1 with the closest first.</returns>
        private List<((VertexId, VertexId), Vector2 intersectionPosition)> FindIntersections(VertexId v1, VertexId v2, List<(VertexId, VertexId)> edges)
        {
            var result = new List<((VertexId, VertexId), Vector2 intersectionPos)>();

            foreach (var edge in edges)
            {
                if (edge.Item1.Equals(v1) || edge.Item1.Equals(v2)) continue;
                if (edge.Item2.Equals(v1) || edge.Item2.Equals(v2)) continue;

                Vector2? intersection = Intersection.LineSegmentPosition(_edges.Position(v1), _edges.Position(v2),
                    _edges.Position(edge.Item1), _edges.Position(edge.Item2));
                if (intersection == null) continue;

                var intersectionPoint = intersection.Value;
                result.Add((edge, intersectionPoint));
            }

            return result.OrderBy(i => Vector2.Distance(_edges.Position(v1), i.intersectionPos)).ToList();
        }

     
      
        /// <summary>
        ///  Rectangle that encapsulates the edge between the two vertices with surrounding margin the width of epsilon.
        /// </summary>
        private Rect RectAroundEdge(VertexId va, VertexId vb)
        {
            var rect = RectExtensions.CreateRectToEncapsulate(_edges.Position(va), _edges.Position(vb));
            rect = rect.Grow(2f*Epsilon);
            return rect;
        }
        

        
        #endregion
    }
}