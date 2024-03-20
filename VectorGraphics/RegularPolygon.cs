using System.Collections.Generic;
using RikusGameDevToolbox.VectorGraphics;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{

    [ExecuteInEditMode]
    public class RegularPolygon : VectorGraphicsShape
    {
        public int numSides = 5;
        public float radius = 2f;
        //public float roundedCornerRadius = 0f;
        //public int roundedCornerVertices = 0;


        protected override void UpdateLineRenderer()
        {
            base.UpdateLineRenderer();
            SetVertices( Vertices(radius, numSides) );
        }

        private List<Vector3> Vertices(float radius, int numSides)
        {
            var result = new List<Vector3>();
            for (int i = 0; i < numSides; i++)
            {
                float angle = i * (360f / numSides);
                result.Add( Geometry.PointOnCircle(angle, radius));
            }
            result.Add( result[0] ); // Close the shape
            return result;
            
        }


    }
}
