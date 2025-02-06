
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using static RikusGameDevToolbox.Tests.Helpers;

namespace RikusGameDevToolbox.Tests
{

    public class TestsPolygon
    {
        private static readonly Vector2[] PointsA = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        private static readonly Vector2[] PointsB = { new(5, 5), new(20, 5), new(20, 6), new(5, 6) };
        private static readonly Vector2[] PointsC = { new(11, 5), new(12, 5), new(12, 6), new(11, 6) };

        private static readonly Vector2[] PointsD =
        {
            new(5, 2), new(15, 2), new(15, 9), new(5, 9), new(5, 8),
            new(14, 8), new(14, 3), new(5, 3)
        };

        private static readonly Vector2[] PointsE = { new(5, 5), new(6, 5), new(6, 6), new(5, 6) };

        private readonly SimplePolygon _a = new(PointsA);
        private readonly SimplePolygon _b = new(PointsB);
        private readonly SimplePolygon _c = new(PointsC);
        private readonly SimplePolygon _d = new(PointsD);
        private readonly SimplePolygon _e = new(PointsE);


        [Test]
        public void Area()
        {
            Assert.IsTrue(IsAlmostSame(_a.Area, 100f));
            var subs = PolygonBoolean.Subtract(_a, _e);
            Assert.IsTrue(IsAlmostSame(subs[0].Area, 99f));
        }

        [Test]
        public void IsInside()
        {

            var holed = PolygonBoolean.Subtract(_a, _e)[0];
            Assert.IsTrue(holed.IsPointInside(new Vector2(1, 1)));
            Assert.IsTrue(holed.IsPointOnEdge(new Vector2(0, 1)));
            Assert.IsTrue(!holed.IsPointInside(new Vector2(-1, -1)));
            Assert.IsTrue(!holed.IsPointInside(new Vector2(5.5f, 5.5f)));
        }

        [Test]
        public void SimpleUnion()
        {
            var union = PolygonBoolean.Union(_a, _b);
            Assert.IsTrue(union.Count == 1 && union[0] is SimplePolygon);
        }

        [Test]
        public void UnionOfTwoNonOverlappingPolygons()
        {
            var union = PolygonBoolean.Union(_a, _c);
            Assert.IsTrue(union.Count == 2 && union[0] is SimplePolygon && union[1] is SimplePolygon);
        }

        [Test]
        public void UnionCreatesPolygonWithHole()
        {
            var union = PolygonBoolean.Union(_a, _d);
            Assert.IsTrue(union.Count == 1);
            Assert.IsTrue(union[0] is PolygonWithHoles);
        }

        [Test]
        public void UnionCreatesPolygonWithAnotherPolygonInsideHole()
        {
            List<SimplePolygon> polygons = new() { _a, _c, _d };
            var union = PolygonBoolean.Union(polygons);
            Assert.IsTrue(union.Count == 2);
            Assert.IsTrue((union[0] is PolygonWithHoles && union[1] is SimplePolygon) ||
                          (union[0] is SimplePolygon && union[1] is PolygonWithHoles));
        }

        [Test]
        public void Intersection()
        {
            var ins = PolygonBoolean.Intersection(_a, _b);
            Assert.IsTrue(ins.Count == 1);
            Assert.IsTrue(ins[0] is SimplePolygon);
            var sp = ins[0] as SimplePolygon;
            Vector2[] expected = { new(5, 5), new(10, 5), new(10, 6), new(5, 6) };
            Assert.IsTrue(ContainsSamePoints(expected, sp.Contour.ToArray()));
        }

        [Test]
        public void Subtraction()
        {
            var subs = PolygonBoolean.Subtract(_a, _d);
            Assert.IsTrue(subs.Count == 1);
            Assert.IsTrue(subs[0] is SimplePolygon);
            Assert.IsTrue(subs[0].Contour.Length == 12);
        }

        [Test]
        public void ListSubtraction1()
        {
            List<Polygon> a = new List<Polygon> { _a };
            List<Polygon> b = new List<Polygon> { _d, _b };

            var subs = PolygonBoolean.Subtract(a, b);
            Assert.IsTrue(subs.Count == 1);
            Assert.IsTrue(subs[0] is SimplePolygon);
            Assert.IsTrue(subs[0].Contour.Length == 16);
        }

        [Test]
        public void ListSubtraction2()
        {
            List<Polygon> a = new List<Polygon> { _a, _c };
            List<Polygon> b = new List<Polygon> { _d };

            var subs = PolygonBoolean.Subtract(a, b);
            Assert.IsTrue(subs.Count == 2);
            Assert.IsTrue(subs[0] is SimplePolygon);
            Assert.IsTrue(subs[1] is SimplePolygon);
        }



        // Note: This test is not very robust. It only checks that the points are the same, not the order.
        bool ContainsSamePoints(Vector2[] a, Vector2[] b)
        {
            if (a.Length != b.Length) return false;
            foreach (var point in a)
            {
                if (!b.Any(p => AlmostSame(point, p))) return false;
            }

            return true;

            bool AlmostSame(Vector2 a, Vector2 b)
            {
                return Vector2.Distance(a, b) < 0.0001f;
            }
        }
    }

}
