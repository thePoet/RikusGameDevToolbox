using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using System.Collections.Generic;


namespace RikusGameDevToolbox.Tests
{
    public class PlanarDivision2Tests
    {
        private readonly Vector2[] _squareCorners = { new(0f, 0f), new (10f, 0f), new (10f, 10f), new (0f, 10f) };
        
        [Test]
        public void SplittingSquare()
        {
            var pd = new PlanarDivision2();
            
            pd.AddLine(_squareCorners[0], _squareCorners[1]);
            pd.AddLine(_squareCorners[1], _squareCorners[2]);
            pd.AddLine(_squareCorners[2], _squareCorners[3]);
            pd.AddLine(_squareCorners[3], _squareCorners[0]);
            
            
            Assert.IsTrue(pd.NumFaces==1);
            Assert.IsTrue(pd.NumEdges==4);
            Assert.IsTrue(pd.NumVertices==4);
            Assert.IsFalse(pd.FaceAt(new(5f,5f)) == FaceId.Empty);
            Assert.IsTrue(pd.FaceAt(new(11f,11f)) == FaceId.Empty);
            
            
            // Split diagonally
            pd.AddLine(_squareCorners[0], _squareCorners[2]);
            Assert.IsTrue(pd.NumFaces==2);
            Assert.IsTrue(pd.NumEdges==5);
            Assert.IsTrue(pd.NumVertices==4);
            var face1 = pd.FaceAt(new Vector2(1f, 9f));
            var face2 = pd.FaceAt(new Vector2(9f, 1f));
            Assert.IsTrue(face1!=FaceId.Empty);
            Assert.IsTrue(face2!=FaceId.Empty);
            Assert.IsTrue(face1!=face2);
            
            
            // split horizontal from middle of side edge
            pd.AddLine(new(0f,5f), new(10f,5f));
            Assert.IsTrue(pd.NumFaces==4);
            Assert.IsTrue(pd.NumVertices==7);
            Assert.IsTrue(pd.NumEdges==10);
  
            HashSet<FaceId> faces = new()
            {
                pd.FaceAt(new Vector2(1f, 9f)),
                pd.FaceAt(new Vector2(9f, 1f)),
                pd.FaceAt(new Vector2(1f, 2f)),
                pd.FaceAt(new Vector2(6f, 5.1f))
            };
            
            Assert.IsTrue(faces.Count==4);
            Assert.IsFalse(faces.Contains(FaceId.Empty));


        }
    }
}