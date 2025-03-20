using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;


public class TestsPolygonMesh
{
    private static readonly Vector2[] PointsA = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
    private static readonly Vector2[] PointsB = { new(20, 0), new(30, 0), new(30, 10), new(20, 10) };


    private static readonly Vector2[] PointsE = { new(5, 0), new(5, -1), new(11, -1), new(11, 0) };
    private static readonly Vector2[] PointsF = { new(-2, -2), new(1, -1), new(-1, 1) };
    private static readonly Vector2[] PointsG = { new(-5, 0), new(-5, -1), new(11, -1), new(11, 0) };

    private readonly SimplePolygon _a = new(PointsA);
    private readonly SimplePolygon _b = new(PointsB);


    private readonly SimplePolygon _e = new(PointsE);
    private readonly SimplePolygon _f = new(PointsF);
    private readonly SimplePolygon _g = new(PointsG);

    [Test]
    public void AddingPolygons()
    {
        var mesh = new PolygonMesh2();

        mesh.AddPolygon(_a);
        Assert.IsTrue(mesh.Edges().Count == 4);
        Assert.IsTrue(mesh.Points().Count == 4);

        mesh.AddPolygon(_b);
        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 8);
    }


    [Test]
    public void OnePointFuse()
    {
        SimplePolygon a = new SimplePolygon(new Vector2[] { new(0, 0), new(10, 0), new(10, 10), new(0, 10) });
        SimplePolygon b = new SimplePolygon(new Vector2[] { new(0, 10), new(0, 12), new(-1, 12) });
        var mesh = new PolygonMesh2();
        mesh.AddPolygon(a);
        mesh.AddPolygon(b);
        Assert.IsTrue(mesh.Edges().Count == 7);
        Assert.IsTrue(mesh.Points().Count == 7);
        mesh.FuseVertices(0.001f);
        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 7);
        Assert.IsTrue(mesh.Points().Count == 6);
    }

    [Test]
    public void EdgeFuse()
    {
        SimplePolygon a = new SimplePolygon(new Vector2[] { new(0, 0), new(10, 0), new(10, 10), new(0, 10) });
        SimplePolygon b = new SimplePolygon(new Vector2[] { new(-5, -5), new(10, 0), new(0, 0) });
        var mesh = new PolygonMesh2();

        mesh.AddPolygon(a);
        mesh.AddPolygon(b);
        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 7);
        Assert.IsTrue(mesh.Points().Count == 7);

        mesh.FuseVertices(0.001f);
        Debug.Log(mesh.DebugInfo());
        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 6);
        Assert.IsTrue(mesh.Points().Count == 5);

    }
    
    [Test]
    public void FuseMany()
    {
        var points = new Vector2[] { new(0, 10), new(10, 10), new(10, 20), new(0, 20) };

        SimplePolygon a = new SimplePolygon(points);
        SimplePolygon b = new SimplePolygon( points.Select(p=> p + new Vector2(10,0)) );
        SimplePolygon c = new SimplePolygon( points.Select(p=> p + new Vector2(0,10)) );  
        SimplePolygon d = new SimplePolygon( points.Select(p=> p + new Vector2(10,10)) );
        SimplePolygon e = new SimplePolygon( points.Select(p=> p + new Vector2(-10,0)) );
        SimplePolygon f = new SimplePolygon( points.Select(p=> p + new Vector2(0,-10)) );

        var mesh = new PolygonMesh2();

        mesh.AddPolygon(b);
        mesh.AddPolygon(c);
        mesh.AddPolygon(d);
        mesh.AddPolygon(e);
        mesh.AddPolygon(f);
        mesh.FuseVertices(0.001f);
        IntegrityTest(mesh);

        
        mesh.AddPolygon(a);
        mesh.FuseVertices(0.001f);
        IntegrityTest(mesh);
 
       
        
        Assert.IsTrue(mesh.Edges().Count == 18);
        Assert.IsTrue(mesh.Points().Count == 13);

    }

    [Test]
    public void ShortEdgeFuse()
    {
        Vector2 p1 = new Vector2(0, 0);
        Vector2 p2 = new Vector2(10, 0);
        Vector2 p3 = new Vector2(10, 10);
        Vector2 p4 = new Vector2(0, 10);
        Vector2 p5 = new Vector2(5, 5);
        Vector2 p6 = new Vector2(6, 5);
    
        SimplePolygon a = new SimplePolygon( new[] { p1,p5,p4 } );
        SimplePolygon b = new SimplePolygon( new[] { p1,p2,p6,p5 } );
        SimplePolygon c = new SimplePolygon( new[] { p2,p3,p6 } );
        SimplePolygon d = new SimplePolygon( new[] { p5,p6,p3,p4 } );
        

        var mesh = new PolygonMesh2();

        mesh.AddPolygon(a);
        mesh.AddPolygon(b);
        mesh.AddPolygon(c);
        mesh.AddPolygon(d);
        Assert.IsTrue(mesh.Edges().Count == 14);
        Assert.IsTrue(mesh.Points().Count == 14);
      
        mesh.FuseVertices(2f);
        
        Debug.Log(mesh.DebugInfo());
        IntegrityTest(mesh);

        
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 5);

    }


    [Test]
    public void SplittingEdges1()
    {
        var mesh = new PolygonMesh2();
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_f);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 7);
    }

    [Test]
    public void SplittingEdges2()
    {
        var mesh = new PolygonMesh2();
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
        var mesh = new PolygonMesh2();
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_g);
        Assert.IsTrue(mesh.Edges().Count == 9);
        Assert.IsTrue(mesh.Points().Count == 8);
        Assert.IsTrue(mesh.PolygonShapes()[0].Contour.Length == 4);
        Assert.IsTrue(mesh.PolygonShapes()[1].Contour.Length == 6);
    }

    [Test]
    public void RemovingPolygons1()
    {
        var mesh = new PolygonMesh2();

        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] points2 = { new(0, 0), new(5, -5), new(10, 0) };
        
        var poly1 = mesh.AddPolygon(new SimplePolygon(points1));
        var poly2 = mesh.AddPolygon(new SimplePolygon(points2));


        Assert.IsTrue(mesh.Edges().Count == 7);
        Assert.IsTrue(mesh.Points().Count == 7);
        Assert.IsTrue(mesh.PolygonIds().Count == 2);

        mesh.RemovePolygon(poly1);

        Assert.IsTrue(mesh.Edges().Count == 3);
        Assert.IsTrue(mesh.Points().Count == 3);
        Assert.IsTrue(mesh.PolygonIds().Count == 1);

        mesh.RemovePolygon(poly2);

        Assert.IsTrue(mesh.Edges().Count == 0);
        Assert.IsTrue(mesh.Points().Count == 0);
        Assert.IsTrue(mesh.PolygonIds().Count == 0);
    }
    
    [Test]
    public void RemovingPolygons2()
    {
        var mesh = new PolygonMesh2();

        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] points2 = { new(0, 0), new(5, -5), new(10, 0) };
        Vector2[] points3 = { new(10, 0), new(15, 5), new(10, 10) };

        var poly1 = mesh.AddPolygon(new SimplePolygon(points1));
        var poly2 = mesh.AddPolygon(new SimplePolygon(points2));
        var poly3 = mesh.AddPolygon(new SimplePolygon(points3));
        
        mesh.FuseVertices(0.1f);

        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 6);
        Assert.IsTrue(mesh.PolygonIds().Count == 3);

        mesh.RemovePolygon(poly1);

        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 6);
        Assert.IsTrue(mesh.Points().Count == 5);
        Assert.IsTrue(mesh.PolygonIds().Count == 2);

        mesh.RemovePolygon(poly2);

        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 3);
        Assert.IsTrue(mesh.Points().Count == 3);
        Assert.IsTrue(mesh.PolygonIds().Count == 1);
    }



    bool AlmostSame(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) < 0.0001f;
    }
    
    private static void IntegrityTest(PolygonMesh2 mesh)
    {
        var result = mesh.TestForIntegrity();
        if (result.message != "") Debug.Log("INTEGRITY FAILED\n" + result.message);
        Assert.IsTrue(result.success);
        
    }
}