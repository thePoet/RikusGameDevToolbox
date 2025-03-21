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

            /// <summary> Returns the edge th  at connects this point to the otherPoint, null if no such edge exists.</summary>
            public Edge EdgeConnectingTo(Point otherPoint) => Edges.FirstOrDefault(edge => edge.HasPoint(otherPoint));
            public bool IsConnectedTo(Point otherPoint) => Edges.Any(edge => edge.HasPoint(otherPoint));

            public IEnumerable<Point> ConnectedPoints => Edges.Select(edge => edge.Point1 == this ? edge.Point2 : edge.Point1)
                                                              .Distinct();

            public override string ToString() => "Point " + ShortHash +  ": " + Position;
            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";
        }

        private class Edge
        {
            public Point Point1;
            public Point Point2;

            public Poly LeftPoly;
            public Poly RightPoly;

            public int NumberOfPolys => (LeftPoly == null ? 0 : 1) + (RightPoly == null ? 0 : 1);
            public bool HasPoint(Point point) => Point1 == point || Point2 == point;
            public Point PointThatIsNot(Point point)
            {
                if (!HasPoint(point)) throw new ArgumentException("Point not in edge.");
                return Point1 == point ? Point2 : Point1;
            }
            
            public void ReplacePoint(Point oldPoint, Point newPoint)
            {
                if (Point1 == oldPoint)
                {
                    Point1 = newPoint;
                    return;
                }
                if (Point2 == oldPoint)
                {
                    Point2 = newPoint;
                    return;
                }
                throw new  ArgumentException("Old point not found in edge.");

            }
            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";
            public override string ToString() => "Edge: " + ShortHash + " " + Point1 + " -> " + Point2 
                                                 + " Left: " + (LeftPoly == null ? "null" : LeftPoly.ShortHash) 
                                                 + " Right: " + (RightPoly == null ? "null" : RightPoly.ShortHash);
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
            
            public override string ToString() => "Polygon: " + ShortHash +"   " + string.Join(", ", Points.Select(p => p.ToString()));
            
            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";
       
        }


        #region --------------------------------------------- FIELDS ---------------------------------------------------
   
        private bool _outlinesAreUpToDate = true;
        private float _longestEdgeLength = 0f;
        private readonly SpatialCollection2d<Point> _points = new();
        private readonly List<Edge> _edges = new();
        private readonly Dictionary<Guid, Poly> _polys = new();
        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

       
        

        public List<(Vector2, Vector2)> Edges() => _edges.Select(e => (e.Point1.Position, e.Point2.Position)).ToList();

        public List<Vector2> Points() => _points.ToList()
                                                .Select(p => p.Position)
                                                .ToList();

        public List<SimplePolygon> PolygonShapes() => _polys.Values.Select(p => p.AsSimplePolygon()).ToList();

        public List<Guid> PolygonIds() => _polys.Keys.ToList();

        public List<(Guid, SimplePolygon)> Polygons() => _polys.Select(kvp => (kvp.Key, kvp.Value.AsSimplePolygon())).ToList();


        public Guid AddPolygon(SimplePolygon shape)
        {
            Poly poly = CreatePolygon(shape);
            poly.Id = Guid.NewGuid();
            _polys.Add(poly.Id, poly);
            _outlinesAreUpToDate = false;
            return poly.Id;
        }
        
        public void ChangeShapeOfPolygon(Guid id, SimplePolygon newShape)
        {
            RemoveGeometry(id);
            try
            {
                _polys[id] = CreatePolygon(newShape); // sekoittaako tämä jos joku iteroi _polys:in yli?
                _polys[id].Id = id;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to change shape of polygon, deleted instead: " + e.Message);
                _polys.Remove(id);
            }
            _outlinesAreUpToDate = false; 
        }

       

        public void RemovePolygon(Guid id)
        {
            RemoveGeometry(id);
            _polys.Remove(id);
            _outlinesAreUpToDate = false; 
        }

        public HashSet<Guid> Neighbours(Guid id)
        {
            HashSet<Guid> result = new();
            var poly = _polys[id];
            foreach (var edge in poly.Edges())
            {
                if (edge.LeftPoly != null && edge.LeftPoly.Id != id)
                {
                    result.Add(edge.LeftPoly.Id);
                }
                if (edge.RightPoly != null && edge.RightPoly.Id != id)
                {
                    result.Add(edge.RightPoly.Id);
                }
            }
            return result;
        }

        public void FuseVertices(float tolerance, bool fuseToEdges=false)
        {
            HashSet<Point> pointsToTryFuse = new(_points);
            FusePoints(pointsToTryFuse);

            if (!fuseToEdges) return;
            
            // We can only attach to edges without polygons on both sides
            List<Edge> edgesToBeProcessed = _edges.Where(e => e.NumberOfPolys<2).ToList();
            HashSet<Point> createdPoints = new();

            while (edgesToBeProcessed.Any())
            {
                Edge edge = edgesToBeProcessed.First();
                edgesToBeProcessed.RemoveAt(0);

                var pointsOnEdge = PointsOnEdge(edge.Point1, edge.Point2, tolerance);
                if (!pointsOnEdge.Any()) continue;

                var dublicates = pointsOnEdge.Select(p => new Point(p.Position))
                    .OrderBy(p => Vector2.Distance(p.Position, edge.Point1.Position))
                    .ToList();

                InsertPointsIntoEdge(dublicates, edge);
                
                foreach (var p in dublicates)
                {
                    createdPoints.Add(p);
                }
            }

            foreach (var p in createdPoints)
            {
                _points.Add(p.Position, p);
            }
            
            
            FusePoints(createdPoints);

            return; 
            
            // --------------------
            void FusePoints(HashSet<Point> toBeProcessed)
            {
                while (toBeProcessed.Any())
                {
                    var point = toBeProcessed.First();
                    toBeProcessed.Remove(point);
                
                    foreach (var nearbyPoint in PointsWithinTolerance(point))
                    {
                        if (IsFuseAllowed(point, nearbyPoint))
                        {
                            Fuse(point, nearbyPoint);
                            toBeProcessed.Remove(nearbyPoint);
                        }
                    }
                }
            }
            
            
            List<Point> PointsWithinTolerance(Point point)
            {
                return _points.ItemsInCircle(point.Position, tolerance)
                    .Where(p => p != point).ToList();
            }
            
            bool IsFuseAllowed(Point point1, Point point2)
            {
                if (point1==point2) return false;
    
                foreach (var middlePoint in PointsConnectedToBoth(point1, point2))
                {
                    Edge e1 = point1.EdgeConnectingTo(middlePoint);
                    Edge e2 = point2.EdgeConnectingTo(middlePoint);
                    if (e1.NumberOfPolys !=1 ) return false; 
                    if (e2.NumberOfPolys !=1 ) return false;
                }
             
                return true;
            }
            

       
            
            void Fuse(Point remainingPoint, Point removedPoint)
            {
                var affectedPolys = PolysWithPoint(removedPoint);   
                
                List<Edge> edgesToDestroy = new();
                List<Edge> edgesToAttach = new();

                foreach (var edge in removedPoint.Edges)
                {
                    if (edge.HasPoint(remainingPoint) || edge.PointThatIsNot(removedPoint).IsConnectedTo(remainingPoint))
                    {
                        edgesToDestroy.Add(edge);
                    }
                    else
                    {
                        edgesToAttach.Add(edge);
                    }
                }
                foreach (var edge in edgesToAttach)
                {
                    edge.ReplacePoint(removedPoint, remainingPoint);
                    remainingPoint.Edges.Add(edge);   
                }

                foreach (var edge in edgesToDestroy)
                {
                    edge.PointThatIsNot(removedPoint).Edges.Remove(edge);
                    _edges.Remove(edge);
                }
                
                // Update polygons
                foreach (var poly in affectedPolys.ToList())
                {
                    if (poly.Points.Contains(remainingPoint))
                    {
                        poly.Points.Remove(removedPoint);
                    }
                    else
                    {
                        int index = poly.Points.IndexOf(removedPoint);
                        poly.Points[index] = remainingPoint;
                    }
                        
              
                    foreach (var (a,b) in AsLoopingPairs(poly.Points))
                    {
                        var edge = a.EdgeConnectingTo(b);
                        if (edge == null)
                        {
                            Debug.Log("Something wrong with edges in poly");
                        }
                        else
                        {
                            if (a == edge.Point1)
                            {
                                edge.LeftPoly = poly;
                            }
                            else
                            {
                                edge.RightPoly = poly;
                            }                        
                        }
                        
                    }
                }
                
                _points.Remove(removedPoint.Position, removedPoint);
            }

            HashSet<Poly> PolysWithPoint(Point point)
            {
                HashSet<Poly> result = new();
                foreach (var edge in point.Edges)
                {
                    if (edge.LeftPoly != null) result.Add(edge.LeftPoly);
                    if (edge.RightPoly != null) result.Add(edge.RightPoly);
                }
                return result;
            }
            
            List<Point> PointsConnectedToBoth(Point a, Point b)
            {
                return a.ConnectedPoints.Where(b.IsConnectedTo).ToList();
            }
            
        }
        
        public void TrimShortEdges(float minLength, bool excludeOutline=false)
        {
            throw new NotImplementedException();
        }
        
        public bool PolygonAt(Vector2 position, out Guid id)
        {
            throw new NotImplementedException();
        }
        
        public int NumberOfSeparateAreas()
        {
            throw new NotImplementedException();
        }

        public Polygon ShapeAsPolygon()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Makes a deep copy of the mesh.
        /// </summary>
        /// <param name="preservePolygonIds">If false new ids are assigned at random.</param>
        public PolygonMesh2 MakeCopy(bool preservePolygonIds=false)
        {
            PolygonMesh2 newMesh = new();
            newMesh._outlinesAreUpToDate = _outlinesAreUpToDate;
            newMesh._longestEdgeLength = _longestEdgeLength;
            
            var p2p = new Dictionary<Point, Point>();
            var e2e = new Dictionary<Edge, Edge>();
            var poly2poly = new Dictionary<Poly, Poly>();


            foreach (var point in _points)
            {
                var newPoint = new Point(point.Position);
                p2p.Add(point,newPoint);
                newMesh._points.Add(newPoint.Position, newPoint);
            }

            foreach (var (id, poly) in _polys)
            {
                var newPoly = new Poly();
                if (preservePolygonIds) newPoly.Id = id;
                else newPoly.Id = Guid.NewGuid();
              
                foreach (var p in poly.Points)
                {
                    newPoly.Points.Add(p2p[p]);
                }
                poly2poly.Add(poly, newPoly);
                newMesh._polys.Add(newPoly.Id, newPoly); 
            }

            foreach (var edge in _edges)
            {
                var newEdge = new Edge();
                newEdge.Point1 = p2p[edge.Point1];
                newEdge.Point2 = p2p[edge.Point2];
                if (edge.LeftPoly!=null) newEdge.LeftPoly = poly2poly[edge.LeftPoly];
                if (edge.RightPoly!=null) newEdge.RightPoly = poly2poly[edge.RightPoly];
                
       
               newMesh._edges.Add(newEdge);
               
               newEdge.Point1.Edges.Add(newEdge);
               newEdge.Point2.Edges.Add(newEdge);
               
            }

            return newMesh;

        }


        public (bool success, string message) TestForIntegrity(bool testForPolygons=true)
        {
            string message = "";
            bool success = true;

            
            foreach (var point in _points)
            {
                foreach (var edge in point.Edges)
                {
                    if (!_edges.Contains(edge)) Problem("Point's edge missing from collection. " + edge + " " + point);
                }
            }
            
            foreach (var edge in _edges)
            {
                if (!_points.Contains(edge.Point1)) Problem("Edge's point missing from collection. "  + edge + "  " + edge.Point1);
                if (!_points.Contains(edge.Point2)) Problem("Edge's point missing from collection. "  + edge + "  " + edge.Point2);
            }

            if (!testForPolygons)   return (success, message);
            
            foreach (var poly in _polys.Values)
            {
                int numPoints = poly.Points.Count;
                if ( numPoints < 3) Problem("Polygon with " + numPoints + " points. " + poly.Id);
                

                foreach ((Point a, Point b)  in AsLoopingPairs(poly.Points))
                {
                    if (!_points.Contains(a)) Problem("Point missing from collection. "  + a + " in " + poly);
                    if (!_points.Contains(b)) Problem("Point missing from collection. "  + b + " in " + poly);
                    
                    var edge = a.EdgeConnectingTo(b);
                    if (edge == null)
                    {
                        Problem("Edge missing between points " + a + " and " + b + ". " + poly);
                        continue;
                    }
                    
                    if ( (a==edge.Point1 && edge.LeftPoly != poly) || 
                         (a==edge.Point2 && edge.RightPoly != poly))
                    {
                        Problem("Polygons edge does not have the polygon on the correct side. " + poly);
                    }
                    if (edge.RightPoly == edge.LeftPoly)
                    {
                        Problem("Edge has the same polygon on both sides. " + poly);
                    }
                        

                }
            }
            
            return (success, message);
            
            void Problem(string msg)
            {
                success = false;
                message += msg + "\n";
            }
        }
        
        
        public string DebugInfo()
        {
            string result = "";


            foreach (var p in _points)
            {
                result += p + " Edges: ";
                foreach (var e in p.Edges)
                {
                    result += _edges.IndexOf(e) + " ";
                }
                result += "\n";
            }

            for (int i=0; i<_edges.Count; i++)
            {
                result += i + " " + _edges[i] + "\n";
            }
            foreach (var g in _polys.Values) result += g + "\n";
          
            return result;
        }
        
        /// <summary>
        /// Call this in OnDrawGizmos() to visualize the mesh for debugging purposes.
        /// </summary>
        public void DrawWithGizmos()
        {
            foreach (var edge in _edges)
            { 
                if (edge.LeftPoly == null || edge.RightPoly == null)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.blue;
                }
                Gizmos.DrawLine(edge.Point1.Position, edge.Point2.Position);
            }
        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------

        private Poly CreatePolygon(SimplePolygon simplePolygon)
        {
            Poly poly = new();

            poly.Points = simplePolygon.Contour.Select(p => new Point(p)).ToList();
            poly.Points.ForEach(p => _points.Add(p.Position, p));

            // Create edges
            foreach (var (point1, point2) in AsLoopingPairs(poly.Points))
            {
               var edge = CreateNewEdge(point1, point2);
               _edges.Add(edge);
               edge.LeftPoly = poly; // SimpePolygon is always CCW
            }
            
            return poly;

            Edge CreateNewEdge(Point point1, Point point2)
            {
                Edge edge = new()
                {
                    Point1 = point1,
                    Point2 = point2,
                };
                point1.Edges.Add(edge);
                point2.Edges.Add(edge);
                float length = Vector2.Distance(point1.Position, point2.Position);
                if (length > _longestEdgeLength)
                {
                    _longestEdgeLength = length;
                }
                return edge;
            }
           
        }
        void InsertPointsIntoEdge(List<Point> points, Edge edge)
        {
            foreach (var point in points)
            {
                var (edge1, edge2) = SplitEdge(edge, point);
                edge = edge2;
            }
        }


        /// <summary>
        /// Replaces the edge with two new edges that go through a new point at given position.
        /// </summary>
        (Edge, Edge) SplitEdge(Edge edge, Point point)
        {
            Edge edge1 = new()
            {
                Point1 = edge.Point1,
                Point2 = point,
                LeftPoly = edge.LeftPoly,
                RightPoly = edge.RightPoly
            };
                
            Edge edge2 = new()
            {
                Point1 = point,
                Point2 = edge.Point2,
                LeftPoly = edge.LeftPoly,
                RightPoly = edge.RightPoly
            };

            point.Edges.Add(edge1);
            point.Edges.Add(edge2);
            
            edge.Point1.Edges.Remove(edge);
            edge.Point2.Edges.Remove(edge);
            edge.Point1.Edges.Add(edge1);
            edge.Point2.Edges.Add(edge2);
                
            _edges.Remove(edge); // SLOW WITH LIST!
            _edges.Add(edge1);
            _edges.Add(edge2);
           
            InsertPointIntoPoly(point, edge.LeftPoly, edge.Point1, edge.Point2);
            InsertPointIntoPoly(point, edge.RightPoly, edge.Point1, edge.Point2);

            return (edge1, edge2);
              
            // Inserts the point into Poly between pointA and pointB
            void InsertPointIntoPoly(Point point, Poly poly, Point pointA, Point pointB)
            {
                if (poly == null) return;
                    
                int index1 = poly.Points.IndexOf(pointA);
                int index2 = poly.Points.IndexOf(pointB);
                if (Mathf.Min(index1, index2) == 0 && Mathf.Max(index1, index2)==poly.Points.Count-1)
                {
                    poly.Points.Add(point);
                    return;
                }
                poly.Points.Insert(Mathf.Max(index1, index2), point);
            } 
        }
    
        
        private void RemoveGeometry(Guid id)
        {
             var poly = _polys[id];

             List<Edge> edgesToBeDestroyed = new();


             foreach (var edge in poly.Edges())
             {
                 if (edge.LeftPoly == poly)
                 {
                     edge.LeftPoly = null;
                 }
                 else
                 {
                     edge.RightPoly = null;
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
                     _points.Remove(edge.Point1.Position, edge.Point1);
                 }
                 if (edge.Point2.Edges.Count == 0)
                 {
                     _points.Remove(edge.Point2.Position, edge.Point2);
                 }
             }

             foreach (Edge edge in edgesToBeDestroyed)
             {
                 _edges.Remove(edge);
             }
             
             poly.Points.Clear();
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

        /// <summary>
        /// Returns other points that are within epsilon of the  line segment between the given points.
        /// </summary>
        private List<Point> PointsOnEdge(Point edgePoint1, Point edgePoint2, float epsilon)
        {
            List<Point> result = new();
            Rect area = RectSurrounding(edgePoint1.Position, edgePoint2.Position); 
            var pointsInArea = _points.ItemsInRectangle(area);
                 
            foreach (Point point in pointsInArea) 
            {
                if (point == edgePoint1 || point == edgePoint2) continue;
                if (IsPointOnEdge(point.Position, edgePoint1.Position, edgePoint2.Position, epsilon))
                {
                    result.Add(point);
                }
            }

            return result;
            
            Rect RectSurrounding(Vector2 a, Vector2 b)
            {
                float m = epsilon * 2f; // Margin to catch points that are on the edge
                return Rect.MinMaxRect( xmin: Mathf.Min(a.x, b.x)-m, ymin: Mathf.Min(a.y, b.y)-m,
                                        xmax: Mathf.Max(a.x, b.x)+m, ymax: Mathf.Max(a.y, b.y)+m );
            }
        }
        
        /// <summary>
        /// Returns list of edges that go through the given point but do not have the point as either endpoint.
        /// </summary>
        private List<Edge> GetEdgesThroughPoint(Point point, float epsilon)
        {
            Rect area = RectCenteredAt(point.Position, size: _longestEdgeLength + epsilon*10f);
            var pointsInArea = _points.ItemsInRectangle(area);

            List<Edge> result = new();
            foreach (var p in pointsInArea)
            {
                foreach (var edge in p.Edges)
                {
                    if (edge.HasPoint(point)) continue;
                    if (!IsPointOnEdge(point.Position, edge.Point1.Position, edge.Point2.Position, epsilon )) continue;
                    if (!result.Contains(edge)) result.Add(edge);
                }
            }
            return result;
        }

        private bool IsPointOnEdge(Vector2 point, Vector2 edgeStart, Vector2 edgeEnd, float epsilon)
        {
            return IsInBoundingBox(point, edgeStart, edgeEnd) &&
                   DistanceFromLine(point, edgeStart, edgeEnd) < epsilon;

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
        
      
        private Rect RectCenteredAt(Vector2 center, float size)
        {
            return Rect.MinMaxRect(center.x - size * 0.5f, center.y - size * 0.5f,
                                   center.x + size * 0.5f, center.y + size * 0.5f);
        }

        #endregion
    }
}
