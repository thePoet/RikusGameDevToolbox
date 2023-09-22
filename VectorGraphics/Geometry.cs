using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace RikusGameDevToolbox.VectorGraphics
{
    public static class Geometry 
    {
        public static List<Vector3> GearToothConical(float addendum, float dedendum)
        {
            var result = new List<Vector3>();
            result.Add(new Vector3(0f, addendum, 0f));
            result.Add( new Vector3(0.25f, addendum, 0f));
            result.Add( new Vector3(0.5f, -dedendum, 0f));
            result.Add( new Vector3(0.75f, -dedendum, 0f));
            return result;
        }

        public static List<Vector3> CircularArc(float radius, float degStart, float degEnd, int numVertices)
        {
            var result = new List<Vector3>();

            float stepDeg = 0f;
            if (numVertices!=1) stepDeg = (degEnd - degStart) / (numVertices-1);
            for (int i = 0; i < numVertices; i++)
            {
                float angle = degStart + i * stepDeg;
                result.Add( PointOnCircle(angle, radius) );
            }

            return result;
        }
        
        
        public static Vector3 PointOnCircle(float angleDeg, float radius)
        {
            float angleRad = Mathf.Deg2Rad * angleDeg;
            return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0f) * radius;
        }


        // INVOLUTE GEAR
        // Involutes of circle
        // x(t) = r(cos(t)+(t-a)sin(t))
        // y(t) = r(sin(t)-(t-a)cos(t))
        // jossa t on kulma ja a "langan etäisyys"
        // tai "triangle wave projected on the circumference of a circle"
    }
}
