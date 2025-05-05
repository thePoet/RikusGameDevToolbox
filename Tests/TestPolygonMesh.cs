using System.Collections.Generic;
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


/*
 * Output: 
   Time add 22500 polygons 719.0475 ms
   Time make a copy 1136.566 ms
   Time to fuse vertices 6657.646 ms
   Time to fuse vertices (fusing to edges enabled)5768.463 ms
   Time to add and fuse 3022.667 ms
   
   Time add 22500 polygons 551.9467 ms
   Time make a copy 1038.917 ms
   Time to fuse vertices 5820.129 ms
   Time to fuse vertices (fusing to edges enabled)5801.161 ms
   Time to add and fuse 3057.964 ms
   
   Time add 22500 polygons 590.2825 ms
   Time make a copy 1169.052 ms
   Time to fuse vertices 5882.709 ms
   Time to fuse vertices (fusing to edges enabled)5921.272 ms
   Time to add and fuse 2936.172 ms
 */

    [Test]
    public void AddingPolygons()
    {
        var mesh = new PolygonMesh();

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
        var mesh = new PolygonMesh();
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
        var mesh = new PolygonMesh();

        mesh.AddPolygon(a);
        mesh.AddPolygon(b);
        IntegrityTest(mesh);
        Assert.IsTrue(mesh.Edges().Count == 7);
        Assert.IsTrue(mesh.Points().Count == 7);

        mesh.FuseVertices(0.001f);
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

        var mesh = new PolygonMesh();

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
        

        var mesh = new PolygonMesh();

        mesh.AddPolygon(a);
        mesh.AddPolygon(b);
        mesh.AddPolygon(c);
        mesh.AddPolygon(d);
        Assert.IsTrue(mesh.Edges().Count == 14);
        Assert.IsTrue(mesh.Points().Count == 14);
      
        mesh.FuseVertices(2f);
        IntegrityTest(mesh);

        
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 5);

    }


    [Test]
    public void SplittingEdges1()
    {
        var mesh = new PolygonMesh();
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_f);
        mesh.FuseVertices(0.001f, fuseToEdges:true);
        IntegrityTest(mesh);

        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 7);
    }

    [Test]
    public void SplittingEdges2()
    {
        var mesh = new PolygonMesh();
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_e);
        mesh.FuseVertices(0.001f, fuseToEdges:true);
        IntegrityTest(mesh);
        
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
        var mesh = new PolygonMesh();
        mesh.AddPolygon(_a);
        mesh.AddPolygon(_g);
        mesh.FuseVertices(0.001f, fuseToEdges:true);
        IntegrityTest(mesh);

        Assert.IsTrue(mesh.Edges().Count == 9);
        Assert.IsTrue(mesh.Points().Count == 8);
      //  Assert.IsTrue(mesh.PolygonShapes()[0].Contour.Length == 4);
        //Assert.IsTrue(mesh.PolygonShapes()[1].Contour.Length == 6);
    }

    [Test]
    public void RemovingPolygons1()
    {
        var mesh = new PolygonMesh();

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
        var mesh = new PolygonMesh();

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
    
    
    [Test]
    public void CopyMesh()
    {
        var mesh = new PolygonMesh();

        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] points2 = { new(0, 0), new(5, -5), new(10, 0) };
        Vector2[] points3 = { new(10, 0), new(15, 5), new(10, 10) };

        var poly1 = mesh.AddPolygon(new SimplePolygon(points1));
        var poly2 = mesh.AddPolygon(new SimplePolygon(points2));
        var poly3 = mesh.AddPolygon(new SimplePolygon(points3));
        mesh.FuseVertices(0.1f);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 6);
        Assert.IsTrue(mesh.PolygonIds().Count == 3);


        PolygonMesh mesh2 = mesh.MakeCopy();
        IntegrityTest(mesh2);
        Assert.IsTrue(mesh2.Edges().Count == 8);
        Assert.IsTrue(mesh2.Points().Count == 6);
        Assert.IsTrue(mesh2.PolygonIds().Count == 3);
        Assert.IsTrue(!mesh2.PolygonIds().Contains(poly1));
        Assert.IsTrue(!mesh2.PolygonIds().Contains(poly2));
        Assert.IsTrue(!mesh2.PolygonIds().Contains(poly3));
        PolygonMesh mesh3 = mesh.MakeCopy(preservePolygonIds:true);
        Assert.IsTrue(mesh3.PolygonIds().Contains(poly1));
        Assert.IsTrue(mesh3.PolygonIds().Contains(poly2));
        Assert.IsTrue(mesh3.PolygonIds().Contains(poly3));
      
    }
    
    [Test]
    public void PartialCopyMesh()
    {
        var mesh = new PolygonMesh();

        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        Vector2[] points2 = { new(0, 0), new(5, -5), new(10, 0) };
        Vector2[] points3 = { new(10, 0), new(15, 5), new(10, 10) };

        var poly1 = mesh.AddPolygon(new SimplePolygon(points1));
        var poly2 = mesh.AddPolygon(new SimplePolygon(points2));
        var poly3 = mesh.AddPolygon(new SimplePolygon(points3));
        mesh.FuseVertices(0.1f);
        Assert.IsTrue(mesh.Edges().Count == 8);
        Assert.IsTrue(mesh.Points().Count == 6);
        Assert.IsTrue(mesh.PolygonIds().Count == 3);


        PolygonMesh mesh2 = mesh.MakeCopy(true, new List<PolygonId>(){poly2, poly3});
        IntegrityTest(mesh2);
        Assert.IsTrue(mesh2.Edges().Count == 6);
        Assert.IsTrue(mesh2.Points().Count == 5);
        Assert.IsTrue(mesh2.PolygonIds().Count == 2);
     
      
    }

    [Test]
    public void Performance()
    {
        int n = 50;
        
        Vector2[] points1 = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        SimplePolygon poly = new SimplePolygon(points1);
        
        List<SimplePolygon> polygons = new();
        
        for (int i=0;i<n;i++)
        {
            for (int j = 0; j < n; j++)
            {
                Vector2 offset = new Vector2(i * 10f, j * 10f);
                polygons.Add(poly.Translate(offset));
            }
        }
        
        var mesh = new PolygonMesh();

        float t1, t2;

        t1 = Time.realtimeSinceStartup;
        foreach (var p in polygons)
        {
            mesh.AddPolygon(p);
        }
        t2 = Time.realtimeSinceStartup;
        Debug.Log("Time add "+ n*n + " polygons " + (t2 - t1)*1000f + " ms");
       
        t1 = Time.realtimeSinceStartup;
        var m2 = mesh.MakeCopy();
        t2 = Time.realtimeSinceStartup;
        Debug.Log("Time make a copy " + (t2 - t1)*1000f + " ms");
        
        t1 = Time.realtimeSinceStartup;
        mesh.FuseVertices(0.01f);
        t2 = Time.realtimeSinceStartup;
        Debug.Log("Time to fuse vertices " + (t2 - t1)*1000f + " ms");
     
        t1 = Time.realtimeSinceStartup;
        m2.FuseVertices(0.01f, fuseToEdges:true);
        t2 = Time.realtimeSinceStartup;
        Debug.Log("Time to fuse vertices (fusing to edges enabled)" + (t2 - t1)*1000f + " ms");
        
        var m3 = mesh.MakeCopy();
        t1 = Time.realtimeSinceStartup;
        foreach (var p in polygons)
        {
            m3.AddPolygonAndFuseVertices(p, 0.01f);
        }
        t2 = Time.realtimeSinceStartup;
        Debug.Log("Time to add and fuse " + (t2 - t1)*1000f + " ms");
    }

    bool AlmostSame(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) < 0.0001f;
    }
    
    private static void IntegrityTest(PolygonMesh mesh)
    {
        var result = mesh.TestForIntegrity();
        if (result.message != "") Debug.Log("INTEGRITY FAILED\n" + result.message);
        Assert.IsTrue(result.success);
        
    }
}