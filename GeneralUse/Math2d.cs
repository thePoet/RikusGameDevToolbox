using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class Math2d
    {
        public static Vector2 RotateVector(Vector2 v, float degrees) => Quaternion.Euler(0, 0, degrees) * v;
        

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

        // Returns increment from angle linePointA to linePointB to positive direction
        public static float AngleIncrement(float a, float b)
        {
            float dif = Mathf.DeltaAngle(a, b);
            if (dif < 0f)
                dif += 360f;
            return dif;
        }

        // Written by AI, not tested
        public static Vector2 LineIntersectionWithCircle(Vector2 linePointA, Vector2 linePointB, Vector2 center,
            float radius)
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

        public static bool IsPointLeftOfLine(Vector2 point, Vector2 linePointA, Vector2 linePointB)
        {
            return (linePointB.x - linePointA.x) * (point.y - linePointA.y) -
                (linePointB.y - linePointA.y) * (point.x - linePointA.x) > 0;
        }


        // FROM: https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
        /// <summary>
        /// Determine whether 2 lines intersect, and give the intersection point if so.
        /// </summary>
        /// <param name="s1Start">Start point of the first line</param>
        /// <param name="s1End">End point of the first line</param>
        /// <param name="s2Start">Start point of the second line</param>
        /// <param name="s2End">End point of the second line</param>
        public static (bool AreIntersecting, Vector2 intersectionPoint) LineSegmentIntersection(Vector2 s1Start, Vector2 s1End, Vector2 s2Start, Vector2 s2End)
        {
            // Consider:
            //   p1start = p
            //   p1end = p + r
            //   p2start = q
            //   p2end = q + s
            // We want to find the intersection point where :
            //  p + t*r == q + u*s
            // So we need to solve for t and u
            var p = s1Start;
            var r = s1End - s1Start;
            var q = s2Start;
            var s = s2End - s2Start;
            var qminusp = q - p;

            float cross_rs = CrossProduct2D(r, s);

            if (Approximately(cross_rs, 0f))
            {
                // Parallel lines
                if (Approximately(CrossProduct2D(qminusp, r), 0f))
                {
                    // Co-linear lines, could overlap
                    float rdotr = Vector2.Dot(r, r);
                    float sdotr = Vector2.Dot(s, r);
                    // this means lines are co-linear
                    // they may or may not be overlapping
                    float t0 = Vector2.Dot(qminusp, r / rdotr);
                    float t1 = t0 + sdotr / rdotr;
                    if (sdotr < 0)
                    {
                        // lines were facing in different directions so t1 > t0, swap to simplify check
                        Swap(ref t0, ref t1);
                    }

                    if (t0 <= 1 && t1 >= 0)
                    {
                        // Nice half-way point intersection
                        float t = Mathf.Lerp(Mathf.Max(0, t0), Mathf.Min(1, t1), 0.5f);
                        return (true, p + t * r);
                    }
                    else
                    {
                        // Co-linear but disjoint
                        return (false, Vector2.zero);
                    }
                }
                else
                {
                    // Just parallel in different places, cannot intersect
                    return (false, Vector2.zero);
                }
            }
            else
            {
                // Not parallel, calculate t and u
                float t = CrossProduct2D(qminusp, s) / cross_rs;
                float u = CrossProduct2D(qminusp, r) / cross_rs;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                {
                    return (true, p + t * r);
                }
                else
                {
                    // Lines only cross outside segment range
                    return (false, Vector2.zero);
                }
            }
            
            void Swap<T>(ref T lhs, ref T rhs)
            {
                (lhs, rhs) = (rhs, lhs);
            }

            bool Approximately(float a, float b, float tolerance = 1e-5f)
            {
                return Mathf.Abs(a - b) <= tolerance;
            }

            float CrossProduct2D(Vector2 a, Vector2 b)
            {
                return a.x * b.y - b.x * a.y;
            }
        }
        
        public static bool AreLineSegmentsIntersecting(Vector2 s1Start, Vector2 s1End, Vector2 s2Start, Vector2 s2End)
        {
            return LineSegmentIntersection(s1Start, s1End, s2Start, s2End).AreIntersecting;
        }

    
    }
}