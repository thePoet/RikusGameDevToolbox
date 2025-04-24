using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class PlanarDivision<T>
    {
        #region --------------------------------- PUBLIC TYPES --------------------------------------------------------

        public enum IntersectionHandling {DoNotCheck, ThrowException, SplitEdges, DoNotCreateEdge}

        public record FaceId(Guid Value)
        {
             public static FaceId Empty => new FaceId(Guid.Empty); 
        }
      
        public record VertexId(Guid Value);


        #endregion
        #region --------------------------------- PRIVATE TYPES --------------------------------------------------------

        
        private class Vertex
        {
            public VertexId Id;
            public Vector2 Position;
            public HalfEdge HalfEdge; // Random half edge that has this vertex as origin
        }
        
        private class Face
        {
            public FaceId Id;
            public T Data;
            public HalfEdge HalfEdge;
            public List<HalfEdge> Contained; // For each hole/group of faces wholly inside the face, we store one half edge
            public Polygon AsPolygon => Polygon.CreateFromPaths(Paths()).First();

            public List<List<Vector2>> Paths()
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
            public FaceId LeftFace; // Face on the left side of the half edge
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

        #endregion

        #region ---------------------------------------------- FIELDS --------------------------------------------------

        

        private Dictionary<FaceId, Face> _faces;
    

        private SpatialCollection2d<Vertex> _vertices = new();
        private Dictionary<VertexId, Vertex> _verticesById = new();

        #endregion
        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public IEnumerable<FaceId> FaceIds() => _faces.Keys;
        

        public VertexId AddVertex(Vector2 position)
        {
            var vertex = new Vertex
            {
                Id = new VertexId(Guid.NewGuid()),
                Position = position,
                HalfEdge = null
            };
            
            _vertices.Add(position, vertex);
            _verticesById.Add(vertex.Id, vertex);
            
            return vertex.Id;
        }

        public bool AddEdge(VertexId vertex1, VertexId vertex2, IntersectionHandling intersectionHandling)
        {
            var v1 = _verticesById.GetValueOrDefault(vertex1);
            var v2 = _verticesById.GetValueOrDefault(vertex2);
            
            if (v1==null || v2==null)
            {
                throw new ArgumentException("Tried to add edge on non-existing vertex.");
            }

            // Check if the edge already exists
            // Check for intersections
            // Check if the edge is already in the graph


            bool v1HasNoEdges = v1.HalfEdge == null;
            bool v2HasNoEdges = v2.HalfEdge == null;
       
            if (v1HasNoEdges && v2HasNoEdges) AddEdgeUnconnectedVertices(v1, v2);
        

            return true;
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
        
        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------


    
        
        
        
        
        private void AddEdgeUnconnectedVertices(Vertex v1, Vertex v2)
        {
            FaceId faceId = FaceAt(v1.Position);
            
            var halfEdge1 = new HalfEdge
            {
                Origin = v1,
                LeftFace = faceId,
            };
            
            var halfEdge2 = new HalfEdge
            {
                Origin = v2,
                LeftFace = faceId,
            };
            
            halfEdge1.Next = halfEdge2;
            halfEdge1.Previous = halfEdge2;
            halfEdge2.Next = halfEdge1;
            halfEdge2.Previous = halfEdge1;

            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);
            
            v1.HalfEdge = halfEdge1;
            v2.HalfEdge = halfEdge2;
            
            if (faceId!=FaceId.Empty)
            {
                var face = _faces[faceId];
                if (face.Contained == null) face.Contained = new List<HalfEdge>();
                face.Contained.Add(halfEdge1);
            }
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
                LeftFace = previousHalfEdge.LeftFace,
            };
            
            var halfEdge2 = new HalfEdge
            {
                Origin = v2,
                Previous = halfEdge1,
                Next = previousHalfEdge.Next,
                LeftFace = previousHalfEdge.LeftFace,
            };
            
            halfEdge1.Next = halfEdge2;
            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);
            v2.HalfEdge = halfEdge2;
            
            previousHalfEdge.Next = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;

        }
        
        private void AddEdgeTwoConnectedVertices(Vertex v1, Vertex v2)
        {
            var previousV1 = FindPreviousHalfEdge(v1, v2);
            var previousV2 = FindPreviousHalfEdge(v2, v1);
            
            var halfEdge1 = new HalfEdge
            {
                Origin = v1,
                Previous = previousV1,
                Next = previousV2.Next
            };
            
            var halfEdge2 = new HalfEdge
            {
                Origin = v2,
                Previous = previousV2,
                Next = previousV1.Next
            };
            (halfEdge1.Twin, halfEdge2.Twin) = (halfEdge2, halfEdge1);

            previousV1.Next = halfEdge1;
            previousV2.Next = halfEdge2;
            halfEdge1.Next.Previous = halfEdge1;
            halfEdge2.Next.Previous = halfEdge2;

        }
        
        // Returns next half edge CCW of origin -> target that is coming INTO the origin vertex 
        private HalfEdge FindPreviousHalfEdge(Vertex origin, Vertex target)
        {
            Vector2 direction = target.Position - origin.Position;
            if (origin.HalfEdge == null) return null;
            return EdgesOriginatingFrom(origin)
                .OrderBy(he => direction.AngleCounterClockwise(target.Position - origin.Position)) // CCW order
                .First() 
                .Twin; // HalfEdge coming into the origin vertex

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
                halfEdge = halfEdge.Twin?.Next;
                if (halfEdge == null) throw new InvalidOperationException("Null halfEdge");
            } while (halfEdge != vertex.HalfEdge);

            return result;
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