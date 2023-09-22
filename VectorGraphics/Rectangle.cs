using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
/*
    [ExecuteInEditMode]
    public class Rectangle : VectorGraphicsShape
    {
        public float width;
        public float height;

        public float roundedCornerRadius = 0f;
        public int roundedCornerVertices = 0;


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
                float angle = i * (2f*Mathf.PI / numSides);
                result.Add( PointOnCircle(angle, radius));
            }
            result.Add( result[0] ); // Close the shape
            return result;
            
            Vector3 PointOnCircle(float angle, float radius) => new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius;
        }


    }
    */
}
