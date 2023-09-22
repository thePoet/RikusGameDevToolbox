using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
    [ExecuteInEditMode]
    public class Ellipse : VectorGraphicsShape
    {
        public int numSides = 20;
        public float a = 2f;
        public float b = 3f;


        protected override void UpdateLineRenderer()
        {
            base.UpdateLineRenderer();
            SetVertices( Vertices() );
        }

        private List<Vector3> Vertices()
        {
            var result = new List<Vector3>();
            for (int i = 0; i < numSides; i++)
            {
                float angle = i * (2f*Mathf.PI / numSides);
                result.Add( PointOnEllipse(angle, a, b));
            }
            result.Add( result[0] ); // Close the shape
            return result;
            
            Vector3 PointOnEllipse(float angle, float a, float b) => new Vector3(a * Mathf.Sin(angle), b * Mathf.Cos(angle), 0f) ;
        }


    }
}
