using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
    [ExecuteInEditMode]
    public class Gear : VectorGraphicsShape
    {
        public int numTeeth = 5;
        public float pitchCircleRadius = 2f;
        public float addendum = 0.3f;
        public float dedendum = 0.3f;

        protected override void UpdateLineRenderer()
        {
            base.UpdateLineRenderer();

            var vertices = Vertices(pitchCircleRadius, addendum, dedendum, numTeeth);
            SetVertices(vertices);
        }



        private List<Vector3> Vertices(float pitchCircleRadius, float addendum, float dedendum, int numTeeth)
        {
            var vertices = new List<Vector3>();
            
            // Pitch is the distance between two teeth measured along the pitch circle.
            float pitch = Circumference(pitchCircleRadius) / numTeeth;
            
            var toothGeometry = Geometry.GearToothConical(addendum, dedendum);
            
            for (int t = 0; t < numTeeth ; t++)
            {
                foreach (Vector3 toothVertex in toothGeometry)
                {
                    float angle = Angle(toothVertex.x, t, numTeeth);
                    float radius = pitchCircleRadius + toothVertex.y;
                    vertices.Add( CreateVertex(radius,angle));
                }
            }

            return vertices;
            
            float Circumference(float radius) => 2f * Mathf.PI * radius;

            float Angle(float pitchPos, int numTooth, int numTotalTeeth)
            {
                float relativePositionOnPitchCircle = (numTooth + pitchPos) / numTotalTeeth;
                return 2f * Mathf.PI * relativePositionOnPitchCircle;
            }
                
            Vector3 CreateVertex(float radius, float angle)
            {
                return new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius;
            }
        }
        
   
    }
}
