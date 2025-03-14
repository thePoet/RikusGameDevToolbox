using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;


public class TestsPolygonMesh
{
    private static readonly Vector2[] PointsA = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
    private static readonly Vector2[] PointsB = { new(20, 0), new(30, 0), new(30, 10), new(20, 10) };
    private static readonly Vector2[] PointsC = { new(-5, 5), new(0, 0), new(10, 0) };
    private static readonly Vector2[] PointsD = { new(10.00001f, 0), new(20, 0), new(20, 10), new(10, 10) };
    private static readonly Vector2[] PointsE = { new(5, 0), new(5, -1), new(11, -1), new(11, 0) };
    private static readonly Vector2[] PointsF = { new(-2, -2), new(1, -1), new(-1, 1) };
    private static readonly Vector2[] PointsG = { new(-5, 0), new(-5, -1), new(11, -1), new(11, 0) };

    private readonly SimplePolygon _a = new(PointsA);
    private readonly SimplePolygon _b = new(PointsB);
    private readonly SimplePolygon _c = new(PointsC);
    private readonly SimplePolygon _d = new(PointsD);
    private readonly SimplePolygon _e = new(PointsE);
    private readonly SimplePolygon _f = new(PointsF);
    private readonly SimplePolygon _g = new(PointsG);

    [Test]
    public void AddingPolygons()
    {
        var mesh = new PolygonMesh2(0.001f);

        mesh.AddPolygon(_a);
        Assert.IsTrue(mesh.Edges().Count == 4);
        Assert.IsTrue(mesh.Points().Count == 4);

        mesh.AddPolygon(_b);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 8);

        mesh.AddPolygon(_c);
        Assert.IsTrue(mesh.Edges().Count == 10);
        Assert.IsTrue(mesh.Points().Count == 9);

        mesh.AddPolygon(_d);
        Assert.IsTrue(mesh.Edges().Count == 12);
        Assert.IsTrue(mesh.Points().Count == 9);
    }

    [Test]
    public void SplittingEdges1()
    {
        var mesh = new PolygonMesh2(0.001f);
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_f);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 7);
    }

    [Test]
    public void SplittingEdges2()
    {
        var mesh = new PolygonMesh2(0.001f);
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_e);
        Assert.IsTrue(mesh.Edges().Count == 9);
        Assert.IsTrue(mesh.Points().Count == 8);
        foreach (var poly in mesh.PolygonShapes())
        {
            Assert.IsTrue(poly.Contour.Length == 5);
        }
    }

    [Test]
    public void SplittingEdges3()
    {
        var mesh = new PolygonMesh2(0.001f);
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_g);
        Assert.IsTrue(mesh.Edges().Count == 9);
        Assert.IsTrue(mesh.Points().Count == 8);
        Assert.IsTrue(mesh.PolygonShapes()[0].Contour.Length == 4);
        Assert.IsTrue(mesh.PolygonShapes()[1].Contour.Length == 6);
    }

    [Test]
    public void RemovingPolygons()
    {
        var mesh = new PolygonMesh2(0.001f);

        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] points2 = { new(0, 0), new(5, -5), new(10, 0) };
        Vector2[] points3 = { new(10, 0), new(15, 5), new(10, 10) };

        var poly1 = mesh.AddPolygon(new SimplePolygon(points1));
        var poly2 = mesh.AddPolygon(new SimplePolygon(points2));
        var poly3 = mesh.AddPolygon(new SimplePolygon(points3));

        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 6);
        Assert.IsTrue(mesh.PolygonIds().Count == 3);

        mesh.RemovePolygon(poly1);

        Assert.IsTrue(mesh.Edges().Count == 6);
        Assert.IsTrue(mesh.Points().Count == 5);
        Assert.IsTrue(mesh.PolygonIds().Count == 2);

        mesh.RemovePolygon(poly2);

        Assert.IsTrue(mesh.Edges().Count == 3);
        Assert.IsTrue(mesh.Points().Count == 3);
        Assert.IsTrue(mesh.PolygonIds().Count == 1);
    }


    bool AlmostSame(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) < 0.0001f;
    }
}