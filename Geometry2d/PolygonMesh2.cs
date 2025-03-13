using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonMesh2
    {
        private class Point
        {
            public readonly Vector2 Position;
            public readonly HashSet<Edge> Edges = new();

            public Point(Vector2 position)
            {
                Position = position;
            }

            /// <summary> Returns the edge that connects this point to the otherPoint, null if no such edge exists.</summary>
            public Edge EdgeConnectingTo(Point otherPoint) => Edges.FirstOrDefault(edge => edge.HasPoint(otherPoint));
        }

        private class Edge
        {
            public Point Point1;
            public Point Point2;

            public Poly Poly1;
            public Poly Poly2;

            public int NumberOfPolys => (Poly1 == null ? 0 : 1) + (Poly2 == null ? 0 : 1);
            public bool HasPoint(Point point) => Point1 == point || Point2 == point;

            public void AddPoly(Poly poly)
            {
                if (Poly1 == null)
                {
                    Poly1 = poly;
                }
                else if (Poly2 == null)
                {
                    Poly2 = poly;
                }
                else
                {
                    throw new InvalidOperationException("Edge already has polygons on both sides.");
                }
            }
        }

        private class Poly
        {
            public Guid Id;
            public List<Point> Points = new(); // CCW order
            public SimplePolygon AsSimplePolygon() => new(Points.Select(p => p.Position).ToArray());

            public IEnumerable<Edge> Edges()
            {
                foreach (var (point1, point2) in AsLoopingPairs(Points))
                {
                    yield return point1.EdgeConnectingTo(point2);
                }
            }
        }

        private class Outline
        {
            public List<Point> Points;
            public bool IsHole;
        }

        #region --------------------------------------------- FIELDS ---------------------------------------------------
 
        
        
        
        private float _epsilon;
        private bool _outlinesAreUpToDate = true;
        private readonly List<Point> _points = new();
        private readonly List<Edge> _edges = new();
        private readonly Dictionary<Guid, Poly> _polys = new();

        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        // Points that are within this manhattan-distance are considered to be the same.
        public PolygonMesh2(float epsilon)
        {
            _epsilon = epsilon;
        }

        public List<(Vector2, Vector2)> Edges() => _edges.Select(e => (e.Point1.Position, e.Point2.Position)).ToList();

        public List<Vector2> Points() => _points.Select(p => p.Position).ToList();

        public List<SimplePolygon> PolygonShapes() => _polys.Values.Select(p => p.AsSimplePolygon()).ToList();

        public List<Guid> PolygonIds() => _polys.Keys.ToList();


        public Guid AddPolygon(SimplePolygon simplePolygon)
        {
            List<Point> polysPoints = new();

            // pitäis tutkia etteivät polygonin pisteet ovat epsilonia lähemäpä toisiaan

            
         
            
            List<Point> newPoints = new();
            
            foreach (Vector2 vertexPos in simplePolygon.Contour)
            {
                Point p = ExistingPointAt(vertexPos);
                if (p == null)
                {
                    p = new Point(vertexPos);
                    _points.Add(p); 
                    newPoints.Add(p);
                }
               polysPoints.Add(p);  
            }

        
            InsertExistingPointsOnEdgesOfNewPoly(polysPoints);
            InsertNewPointsOnEdgesOfOldPolys(polysPoints); // In case of excception, the points added here won't be removed since they don't have visible effect

            // Check that edges have room for the new polygon
            foreach (var (point1, point2) in AsLoopingPairs(polysPoints))
            {
                Edge edge = point1.EdgeConnectingTo(point2);
                if (edge != null && edge.NumberOfPolys == 2)
                {
                    newPoints.ForEach(newPoint => _points.Remove(newPoint)); // Reset to previous state
                    throw new InvalidOperationException("Edge already has polygons on both sides.");
                }
            }

            Poly poly = new();

            // Create edges
            foreach (var (point1, point2) in AsLoopingPairs(polysPoints))
            {
                Edge edge = point1.EdgeConnectingTo(point2);
                if (edge == null)
                {
                    edge = CreateNewEdge(point1, point2);
                    _edges.Add(edge);
                }
                edge.AddPoly(poly);
            }

            poly.Points = polysPoints;

 
            Guid id = Guid.NewGuid();
            poly.Id = id;
            _polys.Add(id, poly);
            _outlinesAreUpToDate = false;
            
            return id;

            
            Edge CreateNewEdge(Point point1, Point point2)
            {
                Edge edge = new()
                {
                    Point1 = point1,
                    Point2 = point2,
                };
                point1.Edges.Add(edge);
                point2.Edges.Add(edge);
                return edge;
            }
            
            // Given the list of point in a new polygon, this finds points of old polygons that split the
            // edges of the new polygon and insert them into the list.
            void InsertExistingPointsOnEdgesOfNewPoly(List<Point> polyPoints)
            {
                for (int i=0; i<polyPoints.Count; i++)
                {
                    Point point1 = polyPoints[i];
                    Point point2 = polyPoints[(i+1) % polyPoints.Count];
                    List<Point> pointsOnEdge = GetPointsOnEdge(point1, point2);
                    if (pointsOnEdge.Any())
                    {
                        Point closestMiddlePoint = pointsOnEdge
                            .OrderBy(p => Vector2.Distance(p.Position, point1.Position))
                            .First();
                        
                 
                        polyPoints.Insert(i+1, closestMiddlePoint);
                    }
                }
              
            }
            
            // Given the list of point in a new polygon, this adds to them to old polygons if they split 
            // their edges
            void InsertNewPointsOnEdgesOfOldPolys(List<Point> points)
            {
               
                
                foreach (var point in points.Where(p=> !p.Edges.Any())) // Points that have edges are not new
                {
                    var edges = GetEdgesThroughPoint(point);
                    edges.ForEach(edge => InsertPointOnEdge(point, edge));
                }
            }
            
            void InsertPointOnEdge(Point point, Edge edge)
            {
                _edges.Remove(edge);
                
                
                Edge edge1 = CreateNewEdge(edge.Point1, point);
                Edge edge2 = CreateNewEdge(point, edge.Point2);
                _edges.Add(edge1);
                _edges.Add(edge2);
                edge1.Poly1 = edge.Poly1;
                edge2.Poly1 = edge.Poly1;
                edge1.Poly2 = edge.Poly2;
                edge2.Poly2 = edge.Poly2;
                
                if (edge.Poly1 != null)
                {
                    InsertPointIntoPoly(point, edge.Poly1, edge.Point1, edge.Point2);
                }
                if (edge.Poly2 != null)
                {
                    InsertPointIntoPoly(point, edge.Poly2, edge.Point1, edge.Point2);
                }

                return;
              
                // Inserts the point into Poly between pointA and pointB
                void InsertPointIntoPoly(Point point, Poly poly, Point pointA, Point pointB)
                {
                    int index1 = poly.Points.IndexOf(edge.Point1);
                    int index2 = poly.Points.IndexOf(edge.Point1);
                    if (Mathf.Min(index1, index2) == 0 && Mathf.Max(index1, index2)==poly.Points.Count-1)
                    {
                        poly.Points.Add(point);
                        return;
                    }
                    poly.Points.Insert(Mathf.Max(index1, index2), point);
                }
            }

        }

        public void RemovePolygon(Guid id)
        {
             var poly = _polys[id];

             List<Edge> edgesToBeDestroyed = new();


             foreach (var edge in poly.Edges())
             {
                 if (edge.Poly1 == poly)
                 {
                     edge.Poly1 = null;
                 }
                 else
                 {
                     edge.Poly2 = null;
                 }

                 if (edge.NumberOfPolys == 0)
                 {
                     edgesToBeDestroyed.Add(edge);
                 }
             }

             foreach (Edge edge in edgesToBeDestroyed)
             {
                 edge.Point1.Edges.Remove(edge);
                 edge.Point2.Edges.Remove(edge);
                 if (edge.Point1.Edges.Count == 0)
                 {
                     _points.Remove(edge.Point1);
                 }
                 if (edge.Point2.Edges.Count == 0)
                 {
                     _points.Remove(edge.Point2);
                 }
             }

             foreach (Edge edge in edgesToBeDestroyed)
             {
                 _edges.Remove(edge);
             }
             
             _polys.Remove(id);

             _outlinesAreUpToDate = false;
        }

        public int NumberOfSeparateMeshes()
        {
            throw new NotImplementedException();
        }

        public Polygon ShapeAsPolygon()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Call this in OnDrawGizmos() to visualize the mesh for debugging purposes.
        /// </summary>
        public void DrawWithGizmos()
        {
            Gizmos.color = Color.yellow;

            foreach (var edge in _edges)
            { 
                Gizmos.DrawLine(edge.Point1.Position, edge.Point2.Position);
            }
            
            //Gizmos.color = Color.blue;
            //foreach (var neighbour in NeighboursOf(polygon))
            //{
             //   Gizmos.DrawLine(polygon.Centroid(), polygon.Centroid() + (neighbour.Centroid() - polygon.Centroid()).normalized );
            //}

        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        /// <summary> Return an existing point in given position or null if one does not exist.</summary>
        private Point ExistingPointAt(Vector2 position)
        {
            // TODO: Horribly inefficient
            return _points.FirstOrDefault(point => IsSamePosition(position, point.Position));
        }


        private static IEnumerable<(Point a, Point b)> AsLoopingPairs(IEnumerable<Point> points)
        {
            Point previous = points.Last();
            foreach (var point in points)
            {
                yield return (previous, point);
                previous = point;
            }
        }

        private List<Point> GetPointsOnEdge(Point edgePoint1, Point edgePoint2)
        {
            List<Point> points = new();
            foreach (Point point in _points) // Inefficient !!!!!!!!!!!!!!!!1
            {
                if (point == edgePoint1 || point == edgePoint2) continue;
                if (IsPointOnEdge(point.Position, edgePoint1.Position, edgePoint2.Position))
                {
                    points.Add(point);
                }
            }

            return points;
        }
        
        private List<Edge> GetEdgesThroughPoint(Point point)
        {
            // Inefficient !!!!!!!!!!!!!!!!1
            
            return _edges.Where(edge => !edge.HasPoint(point) && IsPointOnEdge(point.Position, edge.Point1.Position, edge.Point2.Position)).ToList();
        }

        private bool IsPointOnEdge(Vector2 point, Vector2 edgeStart, Vector2 edgeEnd)
        {
            return IsInBoundingBox(point, edgeStart, edgeEnd) &&
                   DistanceFromLine(point, edgeStart, edgeEnd) < _epsilon;

            bool IsInBoundingBox(Vector2 p, Vector2 a, Vector2 b)
            {
                return Mathf.Min(a.x, b.x) <= p.x && p.x <= Mathf.Max(a.x, b.x) &&
                       Mathf.Min(a.y, b.y) <= p.y && p.y <= Mathf.Max(a.y, b.y);
            }

            float DistanceFromLine(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 rejection = (p - a).RejectionOn(b - a);
                return rejection.magnitude;
            }
        }
        
        public bool IsSamePosition(Vector2 position1, Vector2 position2)
        {
            return Mathf.Abs(position1.x - position2.x) <= _epsilon &&
                   Mathf.Abs(position1.y - position2.y) <= _epsilon;
        }

        #endregion
    }
}