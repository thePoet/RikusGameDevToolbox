using UnityEngine;
using System.Collections;
namespace RikusGameDevToolbox.GeneralUse
{
    public static class Math2d
    {
        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            return Quaternion.Euler(0, 0, degrees) * v;
        }

        // Returns signed angle between vectors
        // (Untested)
        public static float GetAngle(Vector2 v1, Vector2 v2)
        {
            var sign = Mathf.Sign(v1.x * v2.y - v1.y * v2.x);

            return Vector2.Angle(v1, v2) * sign;
        }

        // TODO: Move to Angle and simplify
        public static float ClampAngle(float angle, float min, float max)
        {
            angle = Mathf.Repeat(angle, 360);
            min = Mathf.Repeat(min, 360);
            max = Mathf.Repeat(max, 360);
            bool inverse = false;
            var tmin = min;
            var tangle = angle;
            if (min > 180)
            {
                inverse = !inverse;
                tmin -= 180;
            }

            if (angle > 180)
            {
                inverse = !inverse;
                tangle -= 180;
            }

            var result = !inverse ? tangle > tmin : tangle < tmin;
            if (!result)
                angle = min;

            inverse = false;
            tangle = angle;
            var tmax = max;
            if (angle > 180)
            {
                inverse = !inverse;
                tangle -= 180;
            }

            if (max > 180)
            {
                inverse = !inverse;
                tmax -= 180;
            }

            result = !inverse ? tangle < tmax : tangle > tmax;
            if (!result)
                angle = max;
            return angle;
        }

        // Return an angle of triangle (in degrees) when given lengths of three sides
        public static float SolveAngle(float sideAgainst, float sideNextA, float sideNextB)
        {
            return Mathf.Acos((sideNextA * sideNextA + sideNextB * sideNextB - sideAgainst * sideAgainst)
                              / (2f * sideNextA * sideNextB)) * Mathf.Rad2Deg;


        }

        // Clamps the angle to the closest constraint
        // TODO: Is this the same as above?
        // Move to Angle or delete
        public static float ClampAngleToClosest(float angle, float min, float max)
        {
            if (AngleIncrement(angle, max) < AngleIncrement(angle, min))
                return angle;

            if (Mathf.Abs(Mathf.DeltaAngle(angle, min)) < Mathf.Abs(Mathf.DeltaAngle(angle, max)))
                return min;
            return max;
        }

        // Returns increment from angle a to b to positive direction
        public static float AngleIncrement(float a, float b)
        {
            float dif = Mathf.DeltaAngle(a, b);
            if (dif < 0f)
                dif += 360f;
            return dif;
        }
        
        // Written by AI, not tested
        public static Vector2 LineIntersectionWithCircle(Vector2 linePointA, Vector2 linePointB, Vector2 center, float radius)
        {
            Vector2 d = linePointB - linePointA;
            Vector2 f = linePointA - center;
            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - radius * radius;
            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                return Vector2.positiveInfinity;
            }
            discriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);
            if (t1 >= 0 && t1 <= 1)
            {
                return linePointA + t1 * d;
            }
            if (t2 >= 0 && t2 <= 1)
            {
                return linePointA + t2 * d;
            }
            return Vector2.positiveInfinity;
                
        }

    }

}
