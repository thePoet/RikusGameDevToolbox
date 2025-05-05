using System;
using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using System.Collections.Generic;


namespace RikusGameDevToolbox.Tests
{
    public class PlanarDivisionTests
    {
        private readonly Vector2[] _squareCorners = { new(0f, 0f), new (10f, 0f), new (10f, 10f), new (0f, 10f) };
        
        [Test]
        public void Square()
        {
            var (pd,_) = CreateSquare();
            
            Assert.IsTrue( pd.FaceIds().Count()==1 );

            var faceId = pd.FaceIds().First();
            var faceShape = pd.FaceShape(faceId);
            
            for (int i=0; i<4; i++)
            {
                Assert.IsTrue(faceShape.Contour.Count(v => AlmostSame(v, _squareCorners[i])) == 1);
            }
            
            List<VertexId> vertices = pd.FaceVertices(faceId).ToList();
            Assert.IsTrue(vertices.Count == 4);
            
            Assert.IsTrue(pd.FaceLeftOfEdge(vertices[0], vertices[1]) == faceId);
            Assert.IsTrue(pd.FaceLeftOfEdge(vertices[1], vertices[0]) == FaceId.Empty);
        }


        [Test]
        public void OverlappingEdges()
        {
            var pd = new PlanarDivision();

            var v1 = pd.VertexAt(new Vector2(1f, 0f));
            var v2 = pd.VertexAt(new Vector2(2f, 0f));
            var v3 = pd.VertexAt(new Vector2(3f, 0f));
            var v4 = pd.VertexAt(new Vector2(4f, 0f));


            pd.AddEdge(v1,v3);
            pd.AddEdge(v2,v4);
            
            Assert.IsTrue( pd.IsEdge(v1,v2));
            Assert.IsTrue( pd.IsEdge(v2,v3));
            Assert.IsTrue( pd.IsEdge(v3,v4));


            

          
            
//            Assert.That(() => pd.AddEdge(v3,v4), Throws.TypeOf<InvalidOperationException>());


            Assert.IsTrue( pd.VertexAt(new Vector2(5f,5f)) == null );

        }

        [Test]
        public void BuildingOnSquare()
        {
            var (pd, vertices) = CreateSquare();

            var v1 = vertices[1];
            Assert.IsTrue(v1 != null); 
            var v2 = vertices[2];
            Assert.IsTrue(v2 != null); 
            var squareFace = pd.FaceLeftOfEdge(v1, v2);

            
            Assert.IsTrue(squareFace != FaceId.Empty);
            
            var v3 = pd.AddVertex(new Vector2(15f, 5f));
            pd.AddEdge(v1, v3);

            Assert.IsTrue(squareFace == pd.FaceLeftOfEdge(v1, v2)); // Face stays the same
            Assert.IsTrue(pd.FaceLeftOfEdge(v1, v3) == FaceId.Empty ); 
            Assert.IsTrue(pd.FaceLeftOfEdge(v3, v1) == FaceId.Empty ); 
            
            pd.AddEdge(v3, v2); // Complete the triangle attached to square
            Assert.IsTrue(squareFace == pd.FaceLeftOfEdge(v1, v2)); // Face stays the same
            var triangleFace = pd.FaceLeftOfEdge(v1, v3);
            Assert.IsTrue(triangleFace != squareFace );            
            Assert.IsTrue(triangleFace != FaceId.Empty );  
            Assert.IsTrue(pd.FaceLeftOfEdge(v3, v1) == FaceId.Empty );
            
        }

        [Test]
        public void SplitSquare()
        {
            var (pd, vertices) = CreateSquare();
         
            Assert.IsTrue(pd.NumFaces() == 1);
            pd.AddEdge(vertices[0],vertices[2]);
            Assert.IsTrue(pd.NumFaces() == 2);
            pd.AddEdge(vertices[1],vertices[3]);
            Assert.IsTrue(pd.NumFaces() == 4);
        }

        [Test]
        public void Transform()
        {
            var (pd, vertices) = CreateSquare();
            
            pd.TransformVertices( pos => pos + new Vector2(20f, 0f) );

            Assert.IsTrue( pd.VertexAt(new Vector2(0f, 0f)) == null );
            var v = pd.VertexAt(new Vector2(20f, 0f));
            Assert.IsTrue( v != null );
            Assert.IsTrue( AlmostSame( pd.VertexPosition(v), new Vector2(20f,0f)) );
        }

        [Test]
        public void CombineTwoTriangles()
        {
            var pd = new PlanarDivision();
            
            VertexId[] vertices =
            {
                pd.AddVertex(new Vector2(0f,0f)),
                pd.AddVertex(new Vector2(1f,0f)),
                pd.AddVertex(new Vector2(1f,1f)),
                pd.AddVertex(new Vector2(5f,0f)),
                pd.AddVertex(new Vector2(6f,0f)),
                pd.AddVertex(new Vector2(6f,1f))
            };
            
            pd.AddEdge(vertices[0], vertices[1]);
            pd.AddEdge(vertices[1], vertices[2]);
            pd.AddEdge(vertices[2], vertices[0]);

            pd.AddEdge(vertices[3], vertices[4]);
            pd.AddEdge(vertices[4], vertices[5]);
            pd.AddEdge(vertices[5], vertices[3]);

            FaceId face1 = pd.FaceLeftOfEdge(vertices[0], vertices[1]);
            FaceId face2 = pd.FaceLeftOfEdge(vertices[3], vertices[4]);
            
            Assert.IsTrue(face1 != face2);
            Assert.IsTrue(face1 != FaceId.Empty);
            Assert.IsTrue(face2 != FaceId.Empty);
            Assert.IsTrue(pd.NumFaces() == 2);

            pd.AddEdge(vertices[1], vertices[3]);
            Assert.IsTrue(pd.NumFaces() == 2);
            Assert.IsTrue(pd.FaceLeftOfEdge(vertices[1], vertices[3]) == FaceId.Empty);
            Assert.IsTrue(pd.FaceLeftOfEdge(vertices[3], vertices[1]) == FaceId.Empty);
            
        }

        private (PlanarDivision, VertexId[]) CreateSquare()
        {
            PlanarDivision pd = new PlanarDivision();

            var v1 = pd.AddVertex( _squareCorners[0] );
            var v2 = pd.AddVertex( _squareCorners[1] );
            var v3 = pd.AddVertex( _squareCorners[2] );
            var v4 = pd.AddVertex( _squareCorners[3] );
            
            pd.AddEdge(v1, v2);
            pd.AddEdge(v2, v3);
            pd.AddEdge(v3, v4);
            pd.AddEdge(v4, v1);

            return (pd, new[] { v1, v2, v3, v4 });
        }
        
        private bool AlmostSame(Vector2 a, Vector2 b) => Vector2.Distance(a, b) < 0.0001f;
        
    
        
    }
}