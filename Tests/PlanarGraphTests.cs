using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;

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
            Assert.IsTrue( IsSame(pg.Position(middleVertex.Value), new(5f, 5f)) );

            Assert.IsTrue( pg.ConnectedVertices(middleVertex.Value).Count()==4);
            var corner = pg.VertexAt(new(10f,10f));
            Assert.IsTrue( pg.ConnectedVertices(corner.Value).Count()==3);
            var oppositeCorner = pg.VertexAt(new(0f,0f));

            
            Assert.IsTrue( pg.IsEdgeBetween(middleVertex.Value, corner.Value) );
            Assert.IsFalse( pg.IsEdgeBetween(oppositeCorner.Value, corner.Value) );
        }

        [Test]
        public void EdgeStartingFromEdge()
        {
            var pg = CreateSquare();
            Assert.IsTrue( pg.NumVertices == 4 );
            Assert.IsTrue( pg.NumEdges == 4 );
            pg.AddLine(new(0f,5f), new(10f,5f));
            Assert.IsTrue( pg.NumVertices == 6 );
            Assert.IsTrue( pg.NumEdges == 7 );
            var v = pg.VertexAt(new Vector2(10f, 5f));
            Assert.IsTrue(pg.ConnectedVertices(v.Value).Count()==3);

        }

        [Test]
        public void SplitTwoEdges()
        {
            //       B   D
            //       |   |
            //    E--G---H---F
            //       |   |
            //       A   C
            Vector2 a = new(2f, 0f);
            Vector2 b = new(2f, 2f);
            Vector2 c = new(3f, 0f);
            Vector2 d = new(3f, 2f);
            Vector2 e = new(0f, 1f);
            Vector2 f = new(5f, 1f);
            Vector2 g = new(2f, 1f);
            Vector2 h = new(3f, 1f);
            
            var pg = new PlanarGraph(Epsilon);
            pg.AddLine(a,b);
            pg.AddLine(c,d);
            var result = pg.AddLine(e,f);
            
            Assert.IsTrue( pg.NumVertices == 8 );
            Assert.IsTrue( pg.NumEdges == 7 );
            
            Assert.IsTrue( IsSame( pg.Position(result[0]), e ));
            Assert.IsTrue( IsSame( pg.Position(result[1]), g ));
            Assert.IsTrue( IsSame( pg.Position(result[2]), h ));
            Assert.IsTrue( IsSame( pg.Position(result[3]), f ));
            
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(e).Value, pg.VertexAt(g).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(g).Value, pg.VertexAt(h).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(h).Value, pg.VertexAt(f).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(a).Value, pg.VertexAt(g).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(g).Value, pg.VertexAt(b).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(c).Value, pg.VertexAt(h).Value) );
            Assert.IsTrue( pg.IsEdgeBetween(pg.VertexAt(h).Value, pg.VertexAt(d).Value) );

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
            Assert.IsTrue(pg.NumVertices == 0);
            Assert.IsTrue(pg.NumEdges == 0);
            
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
            
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(0f,0f)).Value, 
                           pg.VertexAt(new Vector2(10f,0f)).Value );

            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 7 );
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(5f,5f)).Value, 
                pg.VertexAt(new Vector2(10f,0f)).Value );
            
            Assert.IsTrue( pg.NumVertices == 5 );
            Assert.IsTrue( pg.NumEdges == 6 );
            
            pg.DeleteEdge( pg.VertexAt(new Vector2(10f,10f)).Value, 
                pg.VertexAt(new Vector2(10f,0f)).Value );
            
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
            
            pg.DeleteVertex( pg.VertexAt(new Vector2(5f, 5f)).Value );
            
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