using System;
using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using System.Collections.Generic;
using SharpVoronoiLib;


namespace RikusGameDevToolbox.Tests
{
    public class PlanarGraphTests
    {
        private const float Epsilon = 0.0001f;

        private readonly Vector2[] _squareCorners = { new(0f, 0f), new(10f, 0f), new(10f, 10f), new(0f, 10f) };

        [Test]
        public void BasicConstruction()
        {
            var pg = CreateSquare();
            Assert.IsTrue( pg.NumVertices == 4 );
            Assert.IsTrue( pg.NumEdges == 4 );

            pg.AddLine(new(0f,0f), new(10f,10f));
            Assert.IsTrue( pg.NumVertices == 4 );
            Assert.IsTrue( pg.NumEdges == 5 );
 
            pg.AddLine(new(0f,10f), new(10f,0f));
            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 8 );

            var middleVertex = pg.VertexAt(new(5f, 5f));
            Assert.IsTrue( middleVertex != null );
            Assert.IsTrue( IsSame(pg.Position(middleVertex), new(5f, 5f)) );

            Assert.IsTrue( pg.EdgesOfVertex(middleVertex).Count()==4);
            var corner = pg.VertexAt(new(10f,10f));
            Assert.IsTrue( pg.EdgesOfVertex(corner).Count()==3);
            var oppositeCorner = pg.VertexAt(new(0f,0f));

            
            Assert.IsTrue( pg.IsEdgeBetween(middleVertex, corner) );
            Assert.IsFalse( pg.IsEdgeBetween(oppositeCorner, corner) );
        }

        [Test]
        public void SplitTwoEdges()
        {
            //       |   |
            //     --+---+---
            //       |   |
            var pg = new PlanarGraph(Epsilon);
            pg.AddLine(new (2f,0f), new (2f,2f));
            pg.AddLine(new (3f,0f), new (3f,2f));
            var result = pg.AddLine(new (0f,1f), new (5f,1f));
            
            Assert.IsTrue( pg.NumVertices == 8 );
            Assert.IsTrue( pg.NumEdges == 7 );
            
            Assert.IsTrue( IsSame( pg.Position(result[0]), new Vector2(0f,1f) ));
            Assert.IsTrue( IsSame( pg.Position(result[1]), new Vector2(2f,1f) ));
            Assert.IsTrue( IsSame( pg.Position(result[2]), new Vector2(3f,1f) ));
            Assert.IsTrue( IsSame( pg.Position(result[3]), new Vector2(5f,1f) ));
        }

        [Test]
        public void OverlappingLines()
        {
            var pg = new PlanarGraph(Epsilon);
            pg.AddLine(new(0f, 0f), new(10f, 0f));
            pg.AddLine(new(1f, 0f), new(2f, 0f));
            Assert.IsTrue(pg.NumVertices == 4);
            Assert.IsTrue(pg.NumEdges == 3);
            pg.Clear();
            
            pg.AddLine(new(1f, 0f), new(2f, 0f));
            pg.AddLine(new(0f, 0f), new(10f, 0f));
            Assert.IsTrue(pg.NumVertices == 4);
            Assert.IsTrue(pg.NumEdges == 3);
            pg.Clear();
            
            pg.AddLine(new(0f, 0f), new(2f, 0f));
            pg.AddLine(new(1f, 0f), new(3f, 0f));
            Assert.IsTrue(pg.NumVertices == 4);
            Assert.IsTrue(pg.NumEdges == 3);
            pg.Clear();
            
            pg.AddLine(new(0f, 0f), new(2f, 0f));
            pg.AddLine(new(1f, 0f), new(2f, 0f));
            Assert.IsTrue(pg.NumVertices == 3);
            Assert.IsTrue(pg.NumEdges == 2);
            pg.Clear();
            
            pg.AddLine(new(1f, 0f), new(2f, 0f));
            pg.AddLine(new(3f, 0f), new(4f, 0f));
            pg.AddLine(new(5f, 0f), new(6f, 0f));
            pg.AddLine(new(0f, 0f), new(6f, 0f));
            Assert.IsTrue(pg.NumVertices == 7);
            Assert.IsTrue(pg.NumEdges == 6);
            pg.Clear();

        }


        [Test]
        public void DeleteEdges()
        {
            var pg = CreateSquare();
            pg.AddLine(new(0f,0f), new(10f,10f));
            pg.AddLine(new(10f,0f), new(0f,10f));
            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 8 );
            
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(0f,0f)), 
                           pg.VertexAt(new Vector2(10f,0f)) );

            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 7 );
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(5f,5f)), 
                pg.VertexAt(new Vector2(10f,0f)) );
            
            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 6 );
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(10f,10f)), 
                pg.VertexAt(new Vector2(10f,0f)) );
            
            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 5 );
            
            pg.DeleteVerticesWithoutEdges();
            Assert.IsTrue( pg.NumVertices == 4 );
            Assert.IsTrue( pg.NumEdges == 5 );
        }

        [Test]
        public void DeletePoints()
        {
            var pg = CreateSquare();
            pg.AddLine(new(0f, 0f), new(10f, 10f));
            pg.AddLine(new(10f, 0f), new(0f, 10f));
            Assert.IsTrue(pg.NumVertices == 5);
            Assert.IsTrue(pg.NumEdges == 8);
            
            pg.DeleteVertex( pg.VertexAt(new Vector2(5f, 5f)) );
            
            Assert.IsTrue( pg.NumVertices == 4 );
            Assert.IsTrue( pg.NumEdges == 4 );

        }

        private bool IsSame(Vector2 a, Vector2 b) => Vector2.Distance(a, b) <= Epsilon;



        private PlanarGraph CreateSquare()
        {
            PlanarGraph p = new PlanarGraph(Epsilon);
            p.AddLine(_squareCorners[0], _squareCorners[1]);
            p.AddLine(_squareCorners[1], _squareCorners[2]);
            p.AddLine(_squareCorners[2], _squareCorners[3]);
            p.AddLine(_squareCorners[3], _squareCorners[0]);
            return p;
        }
    }
}