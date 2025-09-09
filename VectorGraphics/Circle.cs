using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
    [ExecuteInEditMode]
    public class Circle : VectorGraphicsShape
    {
        public int numVertices = 20;
        public float radius = 1f;
        public float startDegrees = 0f;
        public float endDegrees = 360f;

        // TODO: this should happen automatically
        public void UpdateVisual()
        {
            UpdateLineRenderer();
        }

        public override void UpdateLineRenderer()
        {
            base.UpdateLineRenderer();
            SetVertices( Vertices() );
        }

        private List<Vector3> Vertices()
        {
            return Geometry.CircularArc(radius, startDegrees, endDegrees, numVertices);
        }
        
        


    }
}