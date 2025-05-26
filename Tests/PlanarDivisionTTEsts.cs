
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RikusGameDevToolbox.Tests
{
    public class PlanarDivisionTTests
    {
        private readonly Vector2[] _squareCorners = { new(0f, 0f), new (10f, 0f), new (10f, 10f), new (0f, 10f) };
        private readonly Vector2[] _squareCorners2 = { new(20f, 0f), new (30f, 0f), new (30f, 10f), new (20f, 10f) };
        private readonly Vector2[] _smallSquareCorners = { new(1f, 1f), new (2f, 1f), new (2f, 2f), new (1f, 2f) };
        
        [Test]
        public void SetAndGetValue()
        {
            var pd = PlanarDivisionPolygon(_squareCorners);
            Assert.IsTrue(pd.NumFaces==1);
            Assert.IsTrue(pd.NumEdges==4);
            Assert.IsTrue(pd.NumVertices==4);
            
            Assert.IsFalse(pd.FaceAt(new(5f,5f)) == FaceId.Empty);
            Assert.IsTrue(pd.FaceAt(new(11f,11f)) == FaceId.Empty);
         
            FaceId face1 = pd.FaceAt(new Vector2(1f, 1f));
            
            Assert.IsFalse( pd.TryGetValue(face1, out Color c) );
            pd.SetValue(face1, Color.blue);
            Assert.IsTrue( pd.TryGetValue(face1, out c) );
            Assert.IsTrue(c == Color.blue);
        }

        [Test]
        public void AddPolygons()
        {
            var pd = new PlanarDivision<Color>();
            
            SimplePolygon square = new(_squareCorners);
            pd.AddPolygonOver(square, Color.red);
            
            Assert.IsTrue(pd.NumFaces==1);
            Assert.IsTrue(pd.NumEdges==4);
            Assert.IsTrue(pd.NumVertices==4);
          
            Assert.IsTrue( IsColorAtPos(new Vector2(1f,1f), Color.red));

            square = square.Translate(new Vector2(5f, 5f));
            pd.AddPolygonOver(square, Color.blue);
            
            Debug.Log("NumFaces: " + pd.NumFaces);
            Assert.IsTrue(pd.NumFaces==2); 
            Assert.IsTrue(pd.NumEdges==10);
            Assert.IsTrue(pd.NumVertices==9);

            Assert.IsTrue( IsColorAtPos(new Vector2(1f,1f), Color.red));
            Assert.IsTrue( IsColorAtPos(new Vector2(6f,6f), Color.blue));
            Assert.IsTrue( IsColorAtPos(new Vector2(11f,11f), Color.blue));


            var outer = pd.FaceLeftOfEdge(new Vector2(0f, 0f), new Vector2(0f, 10f));
            pd.TryGetValue(outer, out Color c);
            Assert.IsTrue(c == Color.red);

            var outside = pd.FaceLeftOfEdge(new Vector2(10f, 0f), new Vector2(0f, 0f));
            Assert.IsTrue(outside == FaceId.Empty);

            var inner = pd.FaceLeftOfEdge(new Vector2(1f, 1f), new Vector2(2f, 1f));
            pd.TryGetValue(outer, out Color c2);
            Assert.IsTrue(c2 == Color.blue);

            
            Assert.IsTrue( pd.FaceLeftOfEdge(new Vector2(2f, 1f), new Vector2(1f, 1f)) == inner );
            
            
            bool IsColorAtPos(Vector2 pos, Color color) => pd.TryGetValue(pos, out Color c) && c == color;
            
        }

        [Test]
        public void AddNestedFaces()
        {
            var pd = new PlanarDivision<Color>();

            SimplePolygon square = new(_squareCorners);
            pd.AddPolygonOver(square, Color.red);
            SimplePolygon square2 = new(_smallSquareCorners);
            pd.AddPolygonOver(square2, Color.blue);
            Assert.IsTrue(pd.NumFaces==2); 
            Assert.IsTrue(pd.NumEdges==8);
            Assert.IsTrue(pd.NumVertices==8);
            Assert.IsTrue( IsColorAtPos(new Vector2(5f,5f), Color.red));
            Assert.IsTrue( IsColorAtPos(new Vector2(1.5f,1.5f), Color.blue));
            
            bool IsColorAtPos(Vector2 pos, Color color) => pd.TryGetValue(pos, out Color c) && c == color;
        }



        private PlanarDivision<Color> PlanarDivisionPolygon(Vector2[] vertices)
        {
            var pd = new PlanarDivision<Color>();

            pd.AddLine(vertices[0], vertices[1]);
            pd.AddLine(vertices[1], vertices[2]);
            pd.AddLine(vertices[2], vertices[3]);
            pd.AddLine(vertices[3], vertices[0]);

            return pd;
        }
    }
}