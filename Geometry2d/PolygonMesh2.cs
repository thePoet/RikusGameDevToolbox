using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;



namespace RikusGameDevToolbox.Geometry2d
{
    public class PolygonMesh2
    {
        private record Connection;
        
        private record SharedEdge : Connection
        {
            public readonly Edge Edge;
            public SharedEdge(Edge edge) => this.Edge = edge;
        }

        private record ConnectedViaPoint : Connection
        {
            public Point PointInMiddle;
            public ConnectedViaPoint(Point point) => PointInMiddle = point;
        }
        
        
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
            public bool IsConnectedTo(Point otherPoint) => Edges.Any(edge => edge.HasPoint(otherPoint));

            public IEnumerable<Point> ConnectedPoints => Edges.Select(edge => edge.Point1 == this ? edge.Point2 : edge.Point1)
                                                              .Distinct();

            public override string ToString() => "Point [" + GetHashCode() % 999 + "]: " + Position;
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
            public override string ToString() => "Edge: [" + GetHashCode() % 999 + "] " + Point1 + " -> " + Point2;
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
            
            public override string ToString() => "Polygon: " + string.Join(", ", Points.Select(p => p.ToString()));
            
       
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

        public void FuseVertices(float tolerance)
        {
            HashSet<Point> toBeProcesses = new(_points);
            while (toBeProcesses.Any())
            {
                var point = toBeProcesses.First();
                toBeProcesses.Remove(point);
                
                foreach (var nearbyPoint in PointsWithinTolerance(point))
                {
                    if (CanFuse(point, nearbyPoint))
                    {
                        Fuse(point, nearbyPoint);
                       
                        toBeProcesses.Remove(nearbyPoint);
/*
                        var result = TestForIntegrity(testForPolygons:true);
                        if (!result.success)
                        {
                            Debug.LogWarning("Integrity failure while fusing vertices: " + result.message);
                            return;
                        }*/
                    }
                }
            }
            

            return;
            
            List<Point> PointsWithinTolerance(Point point)
            {
                return _points.ItemsInCircle(point.Position, tolerance)
                    .Where(p => p != point).ToList();
            }
            
            bool CanFuse(Point point1, Point point2)
            {
                if (point1==point2) return false;
                // No direct connecting edge allowed
               if (point1.EdgeConnectingTo(point2) != null) return false;
                
                foreach (var middlePoint in PointsConnectedToBoth(point1, point2))
                {
                    Edge e1 = point1.EdgeConnectingTo(middlePoint);
                    Edge e2 = point2.EdgeConnectingTo(middlePoint);
                    
                    if (e1.NumberOfPolys !=1 ) return false; // Pitäis tarkistaa onko oikee puoli myös kai
                    if (e2.NumberOfPolys !=1 ) return false;
                }
             
                return true;
            }
            

       
            
            void Fuse(Point remainingPoint, Point removedPoint)
            {
                ReplacePointInPolys(removedPoint, remainingPoint);
                
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
                    Edge edgeMergedInto = edge.PointThatIsNot(removedPoint).EdgeConnectingTo(remainingPoint);
                    
                    edge.PointThatIsNot(removedPoint).Edges.Remove(edge);

                    if (edgeMergedInto != null)
                    {
                        if (edgeMergedInto.LeftPoly == null) edgeMergedInto.LeftPoly = edge.LeftPoly ?? edge.RightPoly;
                        if (edgeMergedInto.RightPoly == null) edgeMergedInto.RightPoly = edge.LeftPoly ?? edge.RightPoly;
                    }
                    _edges.Remove(edge);
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
            

            // Replaces the removed point with the remaining point in all polygons that contain the removed point
            void ReplacePointInPolys(Point removed, Point remain)
            {
                var polys = PolysWithPoint(removed);
                foreach (var poly in polys)
                {
                    if (poly.Points.Contains(remain))
                    {
                        poly.Points.Remove(removed);
                        //   throw new InvalidOperationException("Remaining point already in polygon.");
                    }
                    else
                    {
                        int index = poly.Points.IndexOf(removed);
                        poly.Points[index] = remain;
                    }

                }
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
                    
                    if ( (a==edge.Point1 && edge.LeftPoly != poly) || (a==edge.Point2 && edge.RightPoly != poly))
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
/*
        private Poly CreatePolygonOld(SimplePolygon simplePolygon)
        {
            List<Point> polysPoints = new();
            List<Point> newPoints = new();
            
            foreach (Vector2 vertexPos in simplePolygon.Contour)
            {
                Point p = ExistingPointAt(vertexPos);
                if (p == null)
                {
                    p = new Point(vertexPos);
                    _points.Add(vertexPos, p);
                    newPoints.Add(p);
                }
                polysPoints.Add(p);  
            }
            
            polysPoints =  polysPoints.Distinct().ToList(); // Removes dublicate points (happens when edges are shorter than epsilon)
            
            if (polysPoints.Count < 3)
            {
                newPoints.ForEach(newPoint => _points.Remove(newPoint.Position, newPoint));
                throw new InvalidOperationException("Polygon must have at least 3 points.");
            }
            
            InsertExistingPointsOnEdgesOfNewPoly(polysPoints);
            InsertNewPointsOnEdgesOfOldPolys(polysPoints); // In case of excception, the points added here won't be removed since they don't have visible effect

            // Check that edges have room for the new polygon
            foreach (var (point1, point2) in AsLoopingPairs(polysPoints))
            {
                Edge edge = point1.EdgeConnectingTo(point2);
                if (edge != null && edge.NumberOfPolys == 2)
                {
                    newPoints.ForEach(newPoint => _points.Remove(newPoint.Position, newPoint)); // Reset to previous state
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
            
            
            // Given the list of point in a new polygon, this finds points of old polygons that split the
            // edges of the new polygon and insert them into the list.
            void InsertExistingPointsOnEdgesOfNewPoly(List<Point> polyPoints)
            {
                for (int i=0; i<polyPoints.Count; i++)
                {
                    Point point1 = polyPoints[i];
                    Point point2 = polyPoints[(i+1) % polyPoints.Count];
                    List<Point> pointsOnEdge = ExistingPointsOnEdge(point1, point2);
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
                edge1.LeftPoly = edge.LeftPoly;
                edge2.LeftPoly = edge.LeftPoly;
                edge1.RightPoly = edge.RightPoly;
                edge2.RightPoly = edge.RightPoly;
                
                if (edge.LeftPoly != null)
                {
                    InsertPointIntoPoly(point, edge.LeftPoly, edge.Point1, edge.Point2);
                }
                if (edge.RightPoly != null)
                {
                    InsertPointIntoPoly(point, edge.RightPoly, edge.Point1, edge.Point2);
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
        */
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

     /*

        /// <summary> Return an existing point in given position or null if one does not exist.</summary>
        private Point ExistingPointAt(Vector2 position)
        {
            var area = RectCenteredAt(position, size: _epsilon);
            return _points.ItemsInRectangle(area).FirstOrDefault();
        }
*/

        private static IEnumerable<(Point a, Point b)> AsLoopingPairs(IEnumerable<Point> points)
        {
            Point previous = points.Last();
            foreach (var point in points)
            {
                yield return (previous, point);
                previous = point;
            }
        }

        private List<Point> ExistingPointsOnEdge(Point edgePoint1, Point edgePoint2, float epsilon)
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
/*
InvalidOperationException: No edge between removed and common point. Point: (-5.33, -1.13) Point: (-3.85, -3.12)
Point: (18.28, 7.67) Edges: 0 1 53 54 
Point: (7.50, 13.30) Edges: 2 3 8 
Point: (7.49, 12.93) Edges: 3 4 7 12 
Point: (7.26, 11.65) Edges: 6 7 13 58 59 
Point: (2.38, 8.59) Edges: 9 10 27 28 43 44 
Point: (-0.66, 6.76) Edges: 16 17 30 31 48 
Point: (-1.69, 4.62) Edges: 16 20 49 75 79 
Point: (-7.11, 3.45) Edges: 24 25 32 37 110 111 
Point: (-14.72, 1.35) Edges: 34 35 40 
Point: (-14.58, 1.35) Edges: 35 36 39 127 
Point: (-7.97, -0.09) Edges: 38 39 128 
Point: (-14.72, -9.90) Edges: 40 41 
Point: (-10.64, -9.90) Edges: 41 42 199 
Point: (-10.64, -9.90) Edges: 
Point: (-6.98, -2.02) Edges: 38 42 155 
Point: (-3.85, -3.12) Edges: 167 170 179 180 198 
Point: (-4.18, -9.38) Edges: 180 181 200 207 
Point: (-4.31, -9.90) Edges: 199 200 208 
Point: (-4.31, -9.90) Edges: 208 
Point: (-4.18, -9.38) Edges: 200 
Point: (-4.18, -9.38) Edges: 
Point: (-3.85, -3.12) Edges: 
Point: (-3.85, -3.12) Edges: 
Point: (-6.56, 1.11) Edges: 112 113 126 128 
Point: (-7.97, -0.09) Edges: 128 
Point: (-6.98, -2.02) Edges: 155 
Point: (-6.98, -2.02) Edges: -1 
Point: (-7.97, -0.09) Edges: 
Point: (-5.26, 0.91) Edges: 113 114 132 133 
Point: (-4.90, -0.12) Edges: 133 134 154 169 
Point: (-5.33, -1.13) Edges: 154 155 170 198 
Point: (-5.33, -1.13) Edges: 170 
Point: (-5.33, -1.13) Edges: 198 
Point: (-2.09, -0.61) Edges: 129 134 
Point: (-1.90, -0.95) Edges: 138 139 
Point: (-1.73, -1.05) Edges: 139 140 
Point: (-1.90, -0.95) Edges: 167 168 
Point: (-1.73, -1.05) Edges: 177 178 
Point: (-1.90, -0.95) Edges: 178 179 
Point: (-1.73, -1.05) Edges: 183 184 
Point: (-2.09, -0.61) Edges: 137 138 
Point: (-4.90, -0.12) Edges: 154 
Point: (-4.90, -0.12) Edges: 
Point: (-2.09, -0.61) Edges: 168 169 
Point: (-6.56, 1.11) Edges: 
Point: (-5.26, 0.91) Edges: 
Point: (-5.26, 0.91) Edges: -1 
Point: (-6.56, 1.11) Edges: -1 
Point: (-7.59, 2.49) Edges: 36 37 
Point: (-14.58, 1.35) Edges: 39 
Point: (-14.72, 1.35) Edges: 
Point: (-14.58, 1.35) Edges: 
Point: (-2.41, 2.28) Edges: 101 102 
Point: (-1.74, 1.48) Edges: 102 103 
Point: (-4.99, 1.37) Edges: 107 108 
Point: (-4.99, 1.37) Edges: 109 114 
Point: (-4.99, 1.37) Edges: 131 132 
Point: (-1.74, 1.48) Edges: 129 130 
Point: (-1.74, 1.48) Edges: 136 137 
Point: (-2.41, 2.28) Edges: 104 108 
Point: (-7.59, 2.49) Edges: 111 112 
Point: (-7.59, 2.49) Edges: 126 127 
Point: (-2.41, 2.28) Edges: 130 131 
Point: (-5.31, 3.74) Edges: 25 26 
Point: (-7.11, 3.45) Edges: 
Point: (-7.11, 3.45) Edges: 
Point: (-5.01, 4.16) Edges: 21 26 
Point: (-2.47, 3.47) Edges: 77 78 
Point: (-5.31, 3.74) Edges: 106 107 
Point: (-5.31, 3.74) Edges: 109 110 
Point: (-2.47, 3.47) Edges: 100 101 
Point: (-2.47, 3.47) Edges: 104 105 
Point: (-5.01, 4.16) Edges: 76 77 
Point: (-5.01, 4.16) Edges: 105 106 
Point: (0.80, 6.40) Edges: 27 31 
Point: (1.41, 4.70) Edges: 45 46 
Point: (-1.69, 4.62) Edges: 49 
Point: (-1.69, 4.60) Edges: 49 50 
Point: (1.10, 3.87) Edges: 50 51 
Point: (-0.19, 1.71) Edges: 95 96 
Point: (-0.80, -2.95) Edges: 177 181 
Point: (-0.80, -2.95) Edges: 184 185 
Point: (-0.80, -2.95) Edges: 206 207 
Point: (1.26, 0.09) Edges: 96 97 
Point: (0.98, -0.46) Edges: 140 141 
Point: (0.63, -2.93) Edges: 185 186 
Point: (0.63, -2.93) Edges: 203 204 
Point: (0.63, -2.93) Edges: 205 206 
Point: (0.98, -0.46) Edges: 163 164 
Point: (1.38, -1.93) Edges: 164 165 
Point: (1.38, -1.93) Edges: 182 186 
Point: (1.38, -1.93) Edges: 202 203 
Point: (0.98, -0.46) Edges: 182 183 
Point: (-0.19, 1.71) Edges: 98 103 
Point: (-0.19, 1.71) Edges: 135 136 
Point: (1.26, 0.09) Edges: 135 141 
Point: (1.26, 0.09) Edges: 162 163 
Point: (1.13, 3.88) Edges: 51 52 
Point: (-1.69, 4.60) Edges: 78 79 
Point: (1.10, 3.87) Edges: 94 95 
Point: (1.10, 3.87) Edges: 98 99 
Point: (-1.69, 4.60) Edges: 99 100 
Point: (1.13, 3.88) Edges: 66 67 
Point: (1.13, 3.88) Edges: 93 94 
Point: (-1.69, 4.62) Edges: 
Point: (1.41, 4.70) Edges: 47 52 
Point: (1.72, 0.28) Edges: 92 97 
Point: (1.72, 0.28) Edges: 118 119 
Point: (1.72, 0.28) Edges: 161 162 
Point: (1.41, 4.70) Edges: 65 66 
Point: (0.80, 6.40) Edges: 44 45 
Point: (0.80, 6.40) Edges: 47 48 
Point: (-5.33, 8.28) Edges: 19 20 
Point: (-5.33, 8.28) Edges: 21 22 
Point: (-0.66, 6.76) Edges: 
Point: (-0.66, 6.76) Edges: 
Point: (-5.33, 8.28) Edges: 75 76 
Point: (3.28, 5.83) Edges: 10 11 
Point: (4.48, 5.44) Edges: 6 11 
Point: (2.71, 2.74) Edges: 67 68 
Point: (2.86, 2.74) Edges: 68 69 
Point: (2.86, 2.74) Edges: 82 83 
Point: (3.72, -0.36) Edges: 115 119 
Point: (3.51, -2.47) Edges: 165 166 
Point: (3.51, -2.47) Edges: 194 195 
Point: (3.51, -2.47) Edges: 201 202 
Point: (3.72, -0.36) Edges: 145 146 
Point: (4.13, -1.48) Edges: 146 147 
Point: (4.13, -1.48) Edges: 160 166 
Point: (4.16, -9.56) Edges: 195 196 
Point: (4.25, -9.90) Edges: 196 197 
Point: (4.16, -9.56) Edges: 201 204 
Point: (4.25, -9.90) Edges: 208 209 
Point: (4.16, -9.56) Edges: 205 209 
Point: (4.13, -1.48) Edges: 193 194 
Point: (3.72, -0.36) Edges: 160 161 
Point: (2.86, 2.74) Edges: 116 117 
Point: (2.71, 2.74) Edges: 92 93 
Point: (2.71, 2.74) Edges: 117 118 
Point: (7.34, 4.62) Edges: 13 14 
Point: (5.13, 4.33) Edges: 60 61 
Point: (4.48, 1.07) Edges: 83 84 
Point: (4.48, 1.07) Edges: 115 116 
Point: (4.48, 1.07) Edges: 144 145 
Point: (5.46, 4.21) Edges: 61 62 
Point: (5.79, 1.38) Edges: 80 84 
Point: (5.79, 1.38) Edges: 88 89 
Point: (6.21, 1.15) Edges: 89 90 
Point: (6.21, 1.15) Edges: 142 143 
Point: (6.29, -1.47) Edges: 147 148 
Point: (6.59, -1.22) Edges: 142 148 
Point: (6.29, -1.47) Edges: 189 190 
Point: (6.47, -9.90) Edges: 190 191 
Point: (6.29, -1.47) Edges: 192 193 
Point: (6.47, -9.90) Edges: 192 197 
Point: (6.59, -1.22) Edges: 151 152 
Point: (6.59, -1.22) Edges: 188 189 
Point: (6.21, 1.15) Edges: 150 151 
Point: (5.79, 1.38) Edges: 143 144 
Point: (5.13, 4.33) Edges: 63 69 
Point: (5.46, 4.21) Edges: 80 81 
Point: (5.13, 4.33) Edges: 81 82 
Point: (5.46, 4.21) Edges: 87 88 
Point: (4.48, 5.44) Edges: 59 60 
Point: (7.34, 4.62) Edges: 58 62 
Point: (4.48, 5.44) Edges: 63 64 
Point: (7.34, 4.62) Edges: 86 87 
Point: (2.38, 8.59) Edges: 
Point: (2.38, 8.59) Edges: 
Point: (3.28, 5.83) Edges: 43 46 
Point: (3.28, 5.83) Edges: 64 65 
Point: (7.26, 11.65) Edges: 13 
Point: (7.26, 11.65) Edges: 
Point: (10.15, 4.93) Edges: 4 5 
Point: (11.99, 4.73) Edges: 0 5 
Point: (8.34, 4.05) Edges: 14 15 
Point: (11.22, 1.89) Edges: 55 56 
Point: (8.53, 2.04) Edges: 73 74 
Point: (8.49, 1.95) Edges: 90 91 
Point: (8.49, 1.95) Edges: 123 124 
Point: (9.17, -0.89) Edges: 124 125 
Point: (8.74, -1.32) Edges: 152 153 
Point: (8.74, -1.32) Edges: 173 174 
Point: (8.74, -1.32) Edges: 187 188 
Point: (10.21, -0.80) Edges: 120 125 
Point: (9.17, -0.89) Edges: 149 153 
Point: (9.17, -0.89) Edges: 172 173 
Point: (9.70, -9.90) Edges: 174 175 
Point: (9.70, -9.90) Edges: 187 191 
Point: (10.21, -0.80) Edges: 157 158 
Point: (10.21, -0.80) Edges: 171 172 
Point: (8.49, 1.95) Edges: 149 150 
Point: (8.53, 2.04) Edges: 85 91 
Point: (8.53, 2.04) Edges: 122 123 
Point: (11.55, 1.29) Edges: 56 57 
Point: (11.22, 1.89) Edges: 70 74 
Point: (11.55, 1.29) Edges: 120 121 
Point: (11.22, 1.89) Edges: 121 122 
Point: (11.55, 1.29) Edges: 156 157 
Point: (8.34, 4.05) Edges: 72 73 
Point: (8.34, 4.05) Edges: 85 86 
Point: (11.99, 4.73) Edges: 54 55 
Point: (11.99, 4.73) Edges: 70 71 
Point: (7.49, 12.93) Edges: 7 
Point: (7.49, 12.93) Edges: 
Point: (10.15, 4.93) Edges: 12 15 
Point: (10.15, 4.93) Edges: 71 72 
Point: (7.50, 13.30) Edges: 
Point: (3.08, 13.30) Edges: 8 9 
Point: (-3.78, 13.30) Edges: 17 18 
Point: (-7.32, 13.30) Edges: 18 19 
Point: (-7.32, 13.30) Edges: 22 23 
Point: (-12.79, 13.30) Edges: 23 24 
Point: (-12.79, 13.30) Edges: 32 33 
Point: (-14.72, 13.30) Edges: 33 34 
Point: (3.08, 13.30) Edges: 28 29 
Point: (-3.78, 13.30) Edges: 29 30 
Point: (18.28, 13.30) Edges: 1 2 
Point: (18.28, 7.67) Edges: 
Point: (18.28, -1.05) Edges: 53 57 
Point: (18.28, -6.00) Edges: 158 159 
Point: (18.28, -9.90) Edges: 175 176 
Point: (18.28, -6.00) Edges: 171 176 
Point: (18.28, -1.05) Edges: 156 159 
0 Edge: Point: (11.99, 4.73) -> Point: (18.28, 7.67)
1 Edge: Point: (18.28, 7.67) -> Point: (18.28, 13.30)
2 Edge: Point: (18.28, 13.30) -> Point: (7.50, 13.30)
3 Edge: Point: (7.50, 13.30) -> Point: (7.49, 12.93)
4 Edge: Point: (7.49, 12.93) -> Point: (10.15, 4.93)
5 Edge: Point: (10.15, 4.93) -> Point: (11.99, 4.73)
6 Edge: Point: (4.48, 5.44) -> Point: (7.26, 11.65)
7 Edge: Point: (7.26, 11.65) -> Point: (7.49, 12.93)
8 Edge: Point: (7.50, 13.30) -> Point: (3.08, 13.30)
9 Edge: Point: (3.08, 13.30) -> Point: (2.38, 8.59)
10 Edge: Point: (2.38, 8.59) -> Point: (3.28, 5.83)
11 Edge: Point: (3.28, 5.83) -> Point: (4.48, 5.44)
12 Edge: Point: (10.15, 4.93) -> Point: (7.49, 12.93)
13 Edge: Point: (7.26, 11.65) -> Point: (7.34, 4.62)
14 Edge: Point: (7.34, 4.62) -> Point: (8.34, 4.05)
15 Edge: Point: (8.34, 4.05) -> Point: (10.15, 4.93)
16 Edge: Point: (-1.69, 4.62) -> Point: (-0.66, 6.76)
17 Edge: Point: (-0.66, 6.76) -> Point: (-3.78, 13.30)
18 Edge: Point: (-3.78, 13.30) -> Point: (-7.32, 13.30)
19 Edge: Point: (-7.32, 13.30) -> Point: (-5.33, 8.28)
20 Edge: Point: (-5.33, 8.28) -> Point: (-1.69, 4.62)
21 Edge: Point: (-5.01, 4.16) -> Point: (-5.33, 8.28)
22 Edge: Point: (-5.33, 8.28) -> Point: (-7.32, 13.30)
23 Edge: Point: (-7.32, 13.30) -> Point: (-12.79, 13.30)
24 Edge: Point: (-12.79, 13.30) -> Point: (-7.11, 3.45)
25 Edge: Point: (-7.11, 3.45) -> Point: (-5.31, 3.74)
26 Edge: Point: (-5.31, 3.74) -> Point: (-5.01, 4.16)
27 Edge: Point: (0.80, 6.40) -> Point: (2.38, 8.59)
28 Edge: Point: (2.38, 8.59) -> Point: (3.08, 13.30)
29 Edge: Point: (3.08, 13.30) -> Point: (-3.78, 13.30)
30 Edge: Point: (-3.78, 13.30) -> Point: (-0.66, 6.76)
31 Edge: Point: (-0.66, 6.76) -> Point: (0.80, 6.40)
32 Edge: Point: (-7.11, 3.45) -> Point: (-12.79, 13.30)
33 Edge: Point: (-12.79, 13.30) -> Point: (-14.72, 13.30)
34 Edge: Point: (-14.72, 13.30) -> Point: (-14.72, 1.35)
35 Edge: Point: (-14.72, 1.35) -> Point: (-14.58, 1.35)
36 Edge: Point: (-14.58, 1.35) -> Point: (-7.59, 2.49)
37 Edge: Point: (-7.59, 2.49) -> Point: (-7.11, 3.45)
38 Edge: Point: (-6.98, -2.02) -> Point: (-7.97, -0.09)
39 Edge: Point: (-7.97, -0.09) -> Point: (-14.58, 1.35)
40 Edge: Point: (-14.72, 1.35) -> Point: (-14.72, -9.90)
41 Edge: Point: (-14.72, -9.90) -> Point: (-10.64, -9.90)
42 Edge: Point: (-10.64, -9.90) -> Point: (-6.98, -2.02)
43 Edge: Point: (3.28, 5.83) -> Point: (2.38, 8.59)
44 Edge: Point: (2.38, 8.59) -> Point: (0.80, 6.40)
45 Edge: Point: (0.80, 6.40) -> Point: (1.41, 4.70)
46 Edge: Point: (1.41, 4.70) -> Point: (3.28, 5.83)
47 Edge: Point: (1.41, 4.70) -> Point: (0.80, 6.40)
48 Edge: Point: (0.80, 6.40) -> Point: (-0.66, 6.76)
49 Edge: Point: (-1.69, 4.62) -> Point: (-1.69, 4.60)
50 Edge: Point: (-1.69, 4.60) -> Point: (1.10, 3.87)
51 Edge: Point: (1.10, 3.87) -> Point: (1.13, 3.88)
52 Edge: Point: (1.13, 3.88) -> Point: (1.41, 4.70)
53 Edge: Point: (18.28, -1.05) -> Point: (18.28, 7.67)
54 Edge: Point: (18.28, 7.67) -> Point: (11.99, 4.73)
55 Edge: Point: (11.99, 4.73) -> Point: (11.22, 1.89)
56 Edge: Point: (11.22, 1.89) -> Point: (11.55, 1.29)
57 Edge: Point: (11.55, 1.29) -> Point: (18.28, -1.05)
58 Edge: Point: (7.34, 4.62) -> Point: (7.26, 11.65)
59 Edge: Point: (7.26, 11.65) -> Point: (4.48, 5.44)
60 Edge: Point: (4.48, 5.44) -> Point: (5.13, 4.33)
61 Edge: Point: (5.13, 4.33) -> Point: (5.46, 4.21)
62 Edge: Point: (5.46, 4.21) -> Point: (7.34, 4.62)
63 Edge: Point: (5.13, 4.33) -> Point: (4.48, 5.44)
64 Edge: Point: (4.48, 5.44) -> Point: (3.28, 5.83)
65 Edge: Point: (3.28, 5.83) -> Point: (1.41, 4.70)
66 Edge: Point: (1.41, 4.70) -> Point: (1.13, 3.88)
67 Edge: Point: (1.13, 3.88) -> Point: (2.71, 2.74)
68 Edge: Point: (2.71, 2.74) -> Point: (2.86, 2.74)
69 Edge: Point: (2.86, 2.74) -> Point: (5.13, 4.33)
70 Edge: Point: (11.22, 1.89) -> Point: (11.99, 4.73)
71 Edge: Point: (11.99, 4.73) -> Point: (10.15, 4.93)
72 Edge: Point: (10.15, 4.93) -> Point: (8.34, 4.05)
73 Edge: Point: (8.34, 4.05) -> Point: (8.53, 2.04)
74 Edge: Point: (8.53, 2.04) -> Point: (11.22, 1.89)
75 Edge: Point: (-1.69, 4.62) -> Point: (-5.33, 8.28)
76 Edge: Point: (-5.33, 8.28) -> Point: (-5.01, 4.16)
77 Edge: Point: (-5.01, 4.16) -> Point: (-2.47, 3.47)
78 Edge: Point: (-2.47, 3.47) -> Point: (-1.69, 4.60)
79 Edge: Point: (-1.69, 4.60) -> Point: (-1.69, 4.62)
80 Edge: Point: (5.79, 1.38) -> Point: (5.46, 4.21)
81 Edge: Point: (5.46, 4.21) -> Point: (5.13, 4.33)
82 Edge: Point: (5.13, 4.33) -> Point: (2.86, 2.74)
83 Edge: Point: (2.86, 2.74) -> Point: (4.48, 1.07)
84 Edge: Point: (4.48, 1.07) -> Point: (5.79, 1.38)
85 Edge: Point: (8.53, 2.04) -> Point: (8.34, 4.05)
86 Edge: Point: (8.34, 4.05) -> Point: (7.34, 4.62)
87 Edge: Point: (7.34, 4.62) -> Point: (5.46, 4.21)
88 Edge: Point: (5.46, 4.21) -> Point: (5.79, 1.38)
89 Edge: Point: (5.79, 1.38) -> Point: (6.21, 1.15)
90 Edge: Point: (6.21, 1.15) -> Point: (8.49, 1.95)
91 Edge: Point: (8.49, 1.95) -> Point: (8.53, 2.04)
92 Edge: Point: (1.72, 0.28) -> Point: (2.71, 2.74)
93 Edge: Point: (2.71, 2.74) -> Point: (1.13, 3.88)
94 Edge: Point: (1.13, 3.88) -> Point: (1.10, 3.87)
95 Edge: Point: (1.10, 3.87) -> Point: (-0.19, 1.71)
96 Edge: Point: (-0.19, 1.71) -> Point: (1.26, 0.09)
97 Edge: Point: (1.26, 0.09) -> Point: (1.72, 0.28)
98 Edge: Point: (-0.19, 1.71) -> Point: (1.10, 3.87)
99 Edge: Point: (1.10, 3.87) -> Point: (-1.69, 4.60)
100 Edge: Point: (-1.69, 4.60) -> Point: (-2.47, 3.47)
101 Edge: Point: (-2.47, 3.47) -> Point: (-2.41, 2.28)
102 Edge: Point: (-2.41, 2.28) -> Point: (-1.74, 1.48)
103 Edge: Point: (-1.74, 1.48) -> Point: (-0.19, 1.71)
104 Edge: Point: (-2.41, 2.28) -> Point: (-2.47, 3.47)
105 Edge: Point: (-2.47, 3.47) -> Point: (-5.01, 4.16)
106 Edge: Point: (-5.01, 4.16) -> Point: (-5.31, 3.74)
107 Edge: Point: (-5.31, 3.74) -> Point: (-4.99, 1.37)
108 Edge: Point: (-4.99, 1.37) -> Point: (-2.41, 2.28)
109 Edge: Point: (-4.99, 1.37) -> Point: (-5.31, 3.74)
110 Edge: Point: (-5.31, 3.74) -> Point: (-7.11, 3.45)
111 Edge: Point: (-7.11, 3.45) -> Point: (-7.59, 2.49)
112 Edge: Point: (-7.59, 2.49) -> Point: (-6.56, 1.11)
113 Edge: Point: (-6.56, 1.11) -> Point: (-5.26, 0.91)
114 Edge: Point: (-5.26, 0.91) -> Point: (-4.99, 1.37)
115 Edge: Point: (3.72, -0.36) -> Point: (4.48, 1.07)
116 Edge: Point: (4.48, 1.07) -> Point: (2.86, 2.74)
117 Edge: Point: (2.86, 2.74) -> Point: (2.71, 2.74)
118 Edge: Point: (2.71, 2.74) -> Point: (1.72, 0.28)
119 Edge: Point: (1.72, 0.28) -> Point: (3.72, -0.36)
120 Edge: Point: (10.21, -0.80) -> Point: (11.55, 1.29)
121 Edge: Point: (11.55, 1.29) -> Point: (11.22, 1.89)
122 Edge: Point: (11.22, 1.89) -> Point: (8.53, 2.04)
123 Edge: Point: (8.53, 2.04) -> Point: (8.49, 1.95)
124 Edge: Point: (8.49, 1.95) -> Point: (9.17, -0.89)
125 Edge: Point: (9.17, -0.89) -> Point: (10.21, -0.80)
126 Edge: Point: (-6.56, 1.11) -> Point: (-7.59, 2.49)
127 Edge: Point: (-7.59, 2.49) -> Point: (-14.58, 1.35)
128 Edge: Point: (-7.97, -0.09) -> Point: (-6.56, 1.11)
129 Edge: Point: (-2.09, -0.61) -> Point: (-1.74, 1.48)
130 Edge: Point: (-1.74, 1.48) -> Point: (-2.41, 2.28)
131 Edge: Point: (-2.41, 2.28) -> Point: (-4.99, 1.37)
132 Edge: Point: (-4.99, 1.37) -> Point: (-5.26, 0.91)
133 Edge: Point: (-5.26, 0.91) -> Point: (-4.90, -0.12)
134 Edge: Point: (-4.90, -0.12) -> Point: (-2.09, -0.61)
135 Edge: Point: (1.26, 0.09) -> Point: (-0.19, 1.71)
136 Edge: Point: (-0.19, 1.71) -> Point: (-1.74, 1.48)
137 Edge: Point: (-1.74, 1.48) -> Point: (-2.09, -0.61)
138 Edge: Point: (-2.09, -0.61) -> Point: (-1.90, -0.95)
139 Edge: Point: (-1.90, -0.95) -> Point: (-1.73, -1.05)
140 Edge: Point: (-1.73, -1.05) -> Point: (0.98, -0.46)
141 Edge: Point: (0.98, -0.46) -> Point: (1.26, 0.09)
142 Edge: Point: (6.59, -1.22) -> Point: (6.21, 1.15)
143 Edge: Point: (6.21, 1.15) -> Point: (5.79, 1.38)
144 Edge: Point: (5.79, 1.38) -> Point: (4.48, 1.07)
145 Edge: Point: (4.48, 1.07) -> Point: (3.72, -0.36)
146 Edge: Point: (3.72, -0.36) -> Point: (4.13, -1.48)
147 Edge: Point: (4.13, -1.48) -> Point: (6.29, -1.47)
148 Edge: Point: (6.29, -1.47) -> Point: (6.59, -1.22)
149 Edge: Point: (9.17, -0.89) -> Point: (8.49, 1.95)
150 Edge: Point: (8.49, 1.95) -> Point: (6.21, 1.15)
151 Edge: Point: (6.21, 1.15) -> Point: (6.59<message truncated>
*/