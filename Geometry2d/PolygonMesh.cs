using System;
using System.Collections.Generic;
using System.Linq;

using RikusGameDevToolbox.GeneralUse;
using UnityEngine;


namespace RikusGameDevToolbox.Geometry2d
{

    public record PolygonId(Guid Value);



    public class PolygonMesh
    {
        protected class Point
        {
            public Vector2 Position;
            public readonly HashSet<Edge> Edges = new();

            public Point(Vector2 position)
            {
                Position = position;
            }

            /// <summary> Returns the edge th  at connects this point to the otherPoint, null if no such edge exists.</summary>
            public Edge EdgeConnectingTo(Point otherPoint) => Edges.FirstOrDefault(edge => edge.HasPoint(otherPoint));

            public bool IsConnectedTo(Point otherPoint) => Edges.Any(edge => edge.HasPoint(otherPoint));

            public IEnumerable<Point> ConnectedPoints => Edges
                .Select(edge => edge.Point1 == this ? edge.Point2 : edge.Point1)
                .Distinct();

            public override string ToString() => "Point " + ShortHash + ": " + Position;
            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";
        }

        protected class Edge
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

                throw new ArgumentException("Old point not found in edge.");

            }

            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";

            public override string ToString() => "Edge: " + ShortHash + " " + Point1 + " -> " + Point2
                                                 + " Left: " + (LeftPoly == null ? "null" : LeftPoly.ShortHash)
                                                 + " Right: " + (RightPoly == null ? "null" : RightPoly.ShortHash);
        }

        protected class Poly
        {
            public PolygonId Id;
            public List<Point> Points = new(); // CCW order
            public SimplePolygon AsSimplePolygon() => new(Points.Select(p => p.Position).ToArray());
            
            public IEnumerable<Edge> Edges()
            {
                foreach (var (point1, point2) in AsLoopingPairs(Points))
                {
                    yield return point1.EdgeConnectingTo(point2);
                }
            }

            public override string ToString() =>
                "Polygon: " + ShortHash + "   " + string.Join(", ", Points.Select(p => p.ToString()));

            internal string ShortHash => "[" + Math.Abs(GetHashCode()) % 999 + "]";

        }

        private class Outline
        {
            public List<Point> points = new();
            public bool isHole = false;
        }



        #region --------------------------------------------- FIELDS ---------------------------------------------------

        private bool _outlinesAreUpToDate = false;
        private SpatialCollection2d<Point> _points = new();
        private readonly HashSet<Edge> _edges = new();
        protected readonly Dictionary<PolygonId, Poly> _polys = new();
        private List<Outline> _outlines = new();
        private float _longestEdgeLength = 0f;

        #endregion

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------


        public List<(Vector2, Vector2)> Edges() => _edges.Select(e => (e.Point1.Position, e.Point2.Position)).ToList();

        public List<Vector2> Points() => _points.ToList()
            .Select(p => p.Position)
            .ToList();

        public List<SimplePolygon> PolygonShapes() => _polys.Values.Select(p => p.AsSimplePolygon()).ToList();

        public List<PolygonId> PolygonIds() => _polys.Keys.ToList();

        public List<(PolygonId, SimplePolygon)> Polygons() =>
            _polys.Select(kvp => (kvp.Key, kvp.Value.AsSimplePolygon())).ToList();

        public SimplePolygon Polygon(PolygonId id) => _polys[id].AsSimplePolygon();

        public bool HasPolygon(PolygonId id) => _polys.ContainsKey(id);

        public List<List<Vector2>> Paths()
        {
            if (!_outlinesAreUpToDate) UpdateOutlines();
            return _outlines.Select(o => o.points.Select(p => p.Position).ToList()).ToList();
        }

        public List<List<Vector2>> Outlines()
        {
            if (!_outlinesAreUpToDate) UpdateOutlines();
            return _outlines.Where(o => !o.isHole)
                .Select(o => o.points.Select(p => p.Position).ToList()).ToList();
        }

        public List<List<Vector2>> Holes()
        {
            if (!_outlinesAreUpToDate) UpdateOutlines();
            return _outlines.Where(o => o.isHole)
                .Select(o => o.points.Select(p => p.Position).ToList()).ToList();
        }

        public List<(Vector2, Vector2)> DebugBorderEdges() => _edges
            .Where(e => e.NumberOfPolys < 2)
            .Select(e => (e.Point1.Position, e.Point2.Position)).ToList();


        public int NumPolygons => _polys.Count;

        public PolygonId AddPolygon(SimplePolygon shape)
        {
            Poly poly = CreatePolygon(shape);
            poly.Id = new PolygonId(Guid.NewGuid());
            _polys.Add(poly.Id, poly);
            _outlinesAreUpToDate = false;
            return poly.Id;
        }



        public PolygonId AddPolygonAndFuseVertices(SimplePolygon shape, float tolerance, bool fuseToEdges = false)
        {
            Poly poly = CreatePolygon(shape);
            poly.Id = new PolygonId(Guid.NewGuid());
            _polys.Add(poly.Id, poly);
            _outlinesAreUpToDate = false;

            HashSet<Point> points = new(poly.Points);
            FusePoints(points, tolerance);


            if (!fuseToEdges) return poly.Id;

            throw new NotImplementedException("Fuse to edges not implemented yet.");
            /*
            HashSet<Point> newPoints = new();
            var edgePoints = InsertPointsOnEdgesWherePointsAlreadyExist(poly.Edges(), tolerance);
            newPoints.UnionWith(edgePoints);
            FusePoints(newPoints, tolerance);

   */





            return poly.Id;



            List<Point> InsertPointsOnEdgesWherePointsAlreadyExist(IEnumerable<Edge> edges, float tolerance)
            {
                List<Point> result = new();
                foreach (var edge in edges)
                {
                    var pointsOnEdge = PointsOnEdge(edge.Point1, edge.Point2, tolerance);
                    if (!pointsOnEdge.Any()) continue;
                    var newEdgePoints = pointsOnEdge.Select(p => new Point(p.Position))
                        .OrderBy(p => Vector2.Distance(p.Position, edge.Point1.Position))
                        .ToList();
                    InsertPointsIntoEdge(newEdgePoints, edge);
                    result.AddRange(newEdgePoints);
                }

                return result;
            }
        }

        public void ChangeShapeOfPolygon(PolygonId id, SimplePolygon newShape)
        {
            RemoveGeometry(id);
            try
            {
                _polys[id] = CreatePolygon(newShape); // TODO: sekoittaako tämä jos joku iteroi _polys:in yli?
                _polys[id].Id = id;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to change shape of polygon, deleted instead: " + e.Message);
                _polys.Remove(id);
            }

            _outlinesAreUpToDate = false;
        }

        public void RemovePolygon(PolygonId id)
        {
            RemoveGeometry(id);
            _polys.Remove(id);
            _outlinesAreUpToDate = false;
        }

        public HashSet<PolygonId> Neighbours(PolygonId id)
        {
            HashSet<PolygonId> result = new();
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



        public void FuseVertices(float tolerance, bool fuseToEdges = false)
        {
            _outlinesAreUpToDate = false;

            HashSet<Point> pointsToTryFuse = new(_points);
            FusePoints(pointsToTryFuse, tolerance);

            if (!fuseToEdges) return;

            // We can only attach to edges without polygons on both sides
            List<Edge> edgesToBeProcessed = _edges.Where(e => e.NumberOfPolys < 2).ToList();
            HashSet<Point> createdPoints = new();

            while (edgesToBeProcessed.Any())
            {
                Edge edge = edgesToBeProcessed.First();
                edgesToBeProcessed.RemoveAt(0);

                var pointsOnEdge = PointsOnEdge(edge.Point1, edge.Point2, tolerance);
                if (!pointsOnEdge.Any()) continue;

                var newEdgePoints = pointsOnEdge.Select(p => new Point(p.Position))
                    .OrderBy(p => Vector2.Distance(p.Position, edge.Point1.Position))
                    .ToList();

                InsertPointsIntoEdge(newEdgePoints, edge);

                createdPoints.UnionWith(newEdgePoints);
            }

            foreach (var p in createdPoints)
            {
                _points.Add(p.Position, p);
            }


            FusePoints(createdPoints, tolerance);

        }

        public void TransformPoints(Func<Vector2, Vector2> transformFunction)
        {
            SpatialCollection2d<Point> newPoints = new();
            
            foreach (Point point in _points)
            {
                point.Position = transformFunction(point.Position);
                newPoints.Add(point.Position, point);
            }

            _points = newPoints;

            _outlinesAreUpToDate = false;
        }


   
        public int NumberOfSeparateAreas()
        {
            if (!_outlinesAreUpToDate) UpdateOutlines();
            return _outlines.Count(o => !o.isHole);
        }

        /// <summary>
        /// Returns the shape of the whole mesh
        /// </summary>
        public List<Polygon> Shape()
        {
            return Geometry2d.Polygon.CreateFromPaths(Paths());
        }


        public List<PolygonMesh> DetachDisconnectedAreas()
        {
            if (!_outlinesAreUpToDate) UpdateOutlines();

            var outlines = _outlines.Where(o => !o.isHole)
                .OrderByDescending(o => o.points.Count).ToList();

            List<PolygonMesh> result = new();

            for (int i = 1; i < outlines.Count; i++)
            {
                Poly firstPoly = SomePolygonInsideOutline(outlines[i]);
                List<PolygonId> polygonIds = PolygonsConnectedTo(firstPoly).Select(p => p.Id).ToList();
                result.Add(MakeCopy(preservePolygonIds:true, polygonIds));
                foreach (var id in polygonIds)
                {
                    RemovePolygon(id);
                }
            }

            if (result.Any())
            {
                _outlinesAreUpToDate = false;
            }

            return result;

            Poly SomePolygonInsideOutline(Outline outline)
            {
                Edge edge = outline.points[0].EdgeConnectingTo(outline.points[1]);
                return edge.RightPoly ?? edge.LeftPoly;
            }



        }


        /// <summary>
        /// Makes a deep copy of the mesh.
        /// </summary>
        /// <param name="preservePolygonIds">If false new ids are assigned at random.</param>
        public PolygonMesh MakeCopy(bool preservePolygonIds = false, List<PolygonId> polygonIdsToCopy = null)
        {


            PolygonMesh newMesh = new();

            // These map the old objects to the new ones
            var point2point = new Dictionary<Point, Point>();
            var poly2poly = new Dictionary<Poly, Poly>();


            foreach (var point in PointsToCopy(polygonIdsToCopy))
            {
                var newPoint = new Point(point.Position);
                point2point.Add(point, newPoint);
                newMesh._points.Add(newPoint.Position, newPoint);
            }

            polygonIdsToCopy ??= _polys.Keys.ToList();

            foreach (var id in polygonIdsToCopy)
            {
                var newPoly = new Poly
                {
                    Id = preservePolygonIds ? id : new PolygonId(Guid.NewGuid())
                };

                foreach (var p in _polys[id].Points)
                {
                    newPoly.Points.Add(point2point[p]);
                }

                poly2poly.Add(_polys[id], newPoly);
                newMesh._polys.Add(newPoly.Id, newPoly);
            }


            foreach (var edge in EdgesToCopy(polygonIdsToCopy))
            {
                var newEdge = new Edge();
                newEdge.Point1 = point2point[edge.Point1];
                newEdge.Point2 = point2point[edge.Point2];
                if (edge.LeftPoly != null && poly2poly.ContainsKey(edge.LeftPoly))
                    newEdge.LeftPoly = poly2poly[edge.LeftPoly];
                if (edge.RightPoly != null && poly2poly.ContainsKey(edge.RightPoly))
                    newEdge.RightPoly = poly2poly[edge.RightPoly];

                newMesh._edges.Add(newEdge);
                newEdge.Point1.Edges.Add(newEdge);
                newEdge.Point2.Edges.Add(newEdge);
            }

            return newMesh;

            IEnumerable<Point> PointsToCopy(List<PolygonId> polyIdsToCopy)
            {
                if (polyIdsToCopy == null) return _points;
                return polyIdsToCopy.SelectMany(id => _polys[id].Points).Distinct();
            }

            IEnumerable<Edge> EdgesToCopy(List<PolygonId> polyIdsToCopy)
            {
                if (polyIdsToCopy == null) return _edges;
                return polyIdsToCopy.SelectMany(id => _polys[id].Edges()).Distinct();
            }




        }


        public (bool success, string message) TestForIntegrity(bool testForPolygons = true)
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
                if (!_points.Contains(edge.Point1))
                    Problem("Edge's point missing from collection. " + edge + "  " + edge.Point1);
                if (!_points.Contains(edge.Point2))
                    Problem("Edge's point missing from collection. " + edge + "  " + edge.Point2);
            }

            if (!testForPolygons) return (success, message);

            foreach (var poly in _polys.Values)
            {
                int numPoints = poly.Points.Count;
                if (numPoints < 3) Problem("Polygon with " + numPoints + " points. " + poly.Id);


                foreach ((Point a, Point b) in AsLoopingPairs(poly.Points))
                {
                    if (!_points.Contains(a)) Problem("Point missing from collection. " + a + " in " + poly);
                    if (!_points.Contains(b)) Problem("Point missing from collection. " + b + " in " + poly);

                    var edge = a.EdgeConnectingTo(b);
                    if (edge == null)
                    {
                        Problem("Edge missing between points " + a + " and " + b + ". " + poly);
                        continue;
                    }

                    if ((a == edge.Point1 && edge.LeftPoly != poly) ||
                        (a == edge.Point2 && edge.RightPoly != poly))
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
                    result += e.ShortHash + " ";
                }

                result += "\n";
            }

            foreach (var e in _edges)
            {
                result += e + "\n";
            }

            foreach (var g in _polys.Values) result += g + "\n";

            return result;
        }

        #endregion

        #region ------------------------------------------ INTERNAL METHODS ----------------------------------------------

        internal bool AreIntersecting(PolygonId polygonA, PolygonId polygonB)
        {
            var a = _polys[polygonA];
            var b = _polys[polygonB];
            foreach (var edgeA in a.Edges())
            {
                foreach (var edgeB in b.Edges())
                {

                    if (edgeA.HasPoint(edgeB.Point1) || edgeA.HasPoint(edgeB.Point2)) continue;
                    if (Intersection.LineSegments(edgeA.Point1.Position, edgeA.Point2.Position,
                            edgeB.Point1.Position, edgeB.Point2.Position))
                    {
                        return true;
                    }
                }
            }

            return false;
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
                if (length > _longestEdgeLength) _longestEdgeLength = length;

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
                if (Mathf.Min(index1, index2) == 0 && Mathf.Max(index1, index2) == poly.Points.Count - 1)
                {
                    poly.Points.Add(point);
                    return;
                }

                poly.Points.Insert(Mathf.Max(index1, index2), point);
            }
        }


        private void RemoveGeometry(PolygonId id)
        {
            var poly = _polys[id];

            List<Edge> edgesToBeDestroyed = new();


            foreach (var edge in poly.Edges())
            {
                if (edge.LeftPoly == poly)
                {
                    edge.LeftPoly = null;
                }
                else if (edge.RightPoly == poly)
                {
                    edge.RightPoly = null;
                }
                else
                {
                    throw new InvalidOperationException("Removed polygon has en edge without said polygon.");
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

        void FusePoints(HashSet<Point> toBeProcessed, float tolerance)
        {
            while (toBeProcessed.Any())
            {
                var point = toBeProcessed.First();
                toBeProcessed.Remove(point);

                foreach (var nearbyPoint in PointsWithinTolerance(point, tolerance))
                {
                    if (IsFuseAllowed(point, nearbyPoint))
                    {
                        Fuse(point, nearbyPoint);
                        toBeProcessed.Remove(nearbyPoint);
                    }
                }
            }
        }


        List<Point> PointsWithinTolerance(Point point, float tolerance)
        {
            return _points.ItemsInCircle(point.Position, tolerance)
                .Where(p => p != point).ToList();
        }

        bool IsFuseAllowed(Point point1, Point point2)
        {
            if (point1 == point2) return false;

            foreach (var middlePoint in PointsConnectedToBoth(point1, point2))
            {
                Edge e1 = point1.EdgeConnectingTo(middlePoint);
                Edge e2 = point2.EdgeConnectingTo(middlePoint);
                if (e1.NumberOfPolys != 1) return false;
                if (e2.NumberOfPolys != 1) return false;
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


                foreach (var (a, b) in AsLoopingPairs(poly.Points))
                {
                    var edge = a.EdgeConnectingTo(b);
                    if (edge == null)
                    {
                        Debug.Log("Edge connecting polygons points does not exist.");
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


        // Returns all polygons that are connected to the given polygon
        HashSet<Poly> PolygonsConnectedTo(Poly poly)
        {
            HashSet<Poly> result = new();
            Traverse(poly);
            return result;

            void Traverse(Poly polygon)
            {
                result.Add(polygon);

                foreach (var edge in polygon.Edges())
                {
                    if (edge == null) throw new Exception("Polygon is missing an edge.");

                    if (edge.LeftPoly != null && edge.LeftPoly != polygon && !result.Contains(edge.LeftPoly))
                    {
                        Traverse(edge.LeftPoly);
                    }

                    if (edge.RightPoly != null && edge.RightPoly != polygon && !result.Contains(edge.RightPoly))
                    {
                        Traverse(edge.RightPoly);
                    }
                }
            }
        }

        private void UpdateOutlines()
        {
            _outlines.Clear();
            var unvisitedEdges = _edges.Where(e => e.NumberOfPolys == 1).ToHashSet();


            while (unvisitedEdges.Any())
            {
                var outline = new Outline();
                var currentEdge = unvisitedEdges.First();
                unvisitedEdges.Remove(currentEdge);

                var startPoint = currentEdge.Point1;
                var currentPoint = currentEdge.Point2;

                outline.points.Add(startPoint);

                int iterations = 1000;

                do
                {
                    iterations++;
                    if (iterations > 5000)
                    {
                        throw new InvalidOperationException("Infinite loop in outline traversal.");
                    }

                    outline.points.Add(currentPoint);

                    var nextEdge = currentPoint.Edges
                        .Where(e => e.NumberOfPolys == 1 && unvisitedEdges.Contains(e))
                        .OrderBy(e => EmptyOnRight(currentEdge, e) ? AngleCW(currentEdge, e) : AngleCCW(currentEdge, e))
                        .FirstOrDefault();

                    if (nextEdge == null)
                    {
                        Debug.LogWarning("Incomplete outline found.");
                        break;
                    }

                    unvisitedEdges.Remove(nextEdge);
                    currentPoint = nextEdge.PointThatIsNot(currentPoint);
                    currentEdge = nextEdge;
                } while (currentPoint != startPoint);

                if (currentPoint != startPoint) continue;

                bool clockwise = PolygonTools.IsClockwise(outline.points.Select(p => p.Position));
                bool emptyOnRight = EmptyOnRightPoints(outline.points[0], outline.points[1]);

                outline.isHole = (clockwise && emptyOnRight) || (!clockwise && !emptyOnRight);

                if (outline.isHole != clockwise)
                {
                    outline.points.Reverse();
                }

                _outlines.Add(outline);

            }

            _outlinesAreUpToDate = true;


            bool EmptyOnRight(Edge from, Edge to)
            {
                if (from.HasPoint(to.Point1) && from.HasPoint(to.Point2))
                {
                    throw new InvalidOperationException("Paranoid check failed: Edge has both points.");
                }

                bool goingOppositeWay = to.HasPoint(from.Point1);
                if (goingOppositeWay) return from.RightPoly != null;
                return from.RightPoly == null;
            }

            bool EmptyOnRightPoints(Point from, Point to)
            {
                Edge edge = from.EdgeConnectingTo(to);
                bool goingRighteWay = edge.Point1 == from;
                return goingRighteWay ? edge.RightPoly == null : edge.LeftPoly == null;
            }

            float AngleCW(Edge from, Edge to)
            {
                Point commonPoint = to.HasPoint(from.Point1) ? from.Point1 : from.Point2;
                Vector2 fromVector = from.PointThatIsNot(commonPoint).Position - commonPoint.Position;
                Vector2 toVector = to.PointThatIsNot(commonPoint).Position - commonPoint.Position;
                return fromVector.AngleClockwise(toVector);
            }

            float AngleCCW(Edge from, Edge to) => 360f - AngleCW(from, to);


     

    
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
                if (PolygonTools.IsPointOnEdge(point.Position, edgePoint1.Position, edgePoint2.Position, epsilon))
                {
                    result.Add(point);
                }
            }

            return result;

            Rect RectSurrounding(Vector2 a, Vector2 b)
            {
                float m = epsilon * 2f; // Margin to catch points that are on the edge
                return Rect.MinMaxRect(xmin: Mathf.Min(a.x, b.x) - m, ymin: Mathf.Min(a.y, b.y) - m,
                    xmax: Mathf.Max(a.x, b.x) + m, ymax: Mathf.Max(a.y, b.y) + m);
            }
        }

        // Very crude implementation
        // Should return all edges that go through the rect but also many that don't.
        private List<Edge> EdgesNear(Rect rect)
        {
            Rect searchArea = rect.Grow(_longestEdgeLength * 2f);
            List<Edge> result = new();
            foreach (var point in _points.ItemsInRectangle(searchArea))
            {
                foreach (var edge in point.Edges)
                {
                    result.Add(edge);
                }
            }

            return result;
        }

        #endregion
    }
}
