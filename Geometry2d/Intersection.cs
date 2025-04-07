
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    
    public static class Intersection
    {

        /// <summary>
        /// Determine whether 2 line segments intersect.
        /// </summary>
        /// <param name="s1Start">Start point of the first line</param>
        /// <param name="s1End">End point of the first line</param>
        /// <param name="s2Start">Start point of the second line</param>
        /// <param name="s2End">End point of the second line</param>
        /// <param name="tolerance"></param>
        public static bool LineSegments(Vector2 s1Start, Vector2 s1End, Vector2 s2Start, Vector2 s2End,
            float tolerance = 1e-5f)
        {
            return LineSegmentPosition(s1Start, s1End, s2Start, s2End, tolerance) != null;
        }


        // FROM: https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
        /// <summary>
        /// Determine whether 2 line segments intersect, and give the intersection point if so.
        /// </summary>
        /// <param name="s1Start">Start point of the first line</param>
        /// <param name="s1End">End point of the first line</param>
        /// <param name="s2Start">Start point of the second line</param>
        /// <param name="s2End">End point of the second line</param>
        /// <param name="tolerance"></param>
        /// <returns>Intersection position, null if there is none.</returns>
        public static Vector2? LineSegmentPosition(Vector2 s1Start, Vector2 s1End, Vector2 s2Start, Vector2 s2End,
            float tolerance = 1e-5f)
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
                        return p + t * r;
                    }
                    else
                    {
                        // Co-linear but disjoint
                        return null;
                    }
                }
                else
                {
                    // Just parallel in different places, cannot intersect
                    return null;
                }
            }
            else
            {
                // Not parallel, calculate t and u
                float t = CrossProduct2D(qminusp, s) / cross_rs;
                float u = CrossProduct2D(qminusp, r) / cross_rs;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                {
                    return p + t * r;
                }
                else
                {
                    // Lines only cross outside segment range
                    return null;
                }
            }

            void Swap<T>(ref T lhs, ref T rhs)
            {
                (lhs, rhs) = (rhs, lhs);
            }

            bool Approximately(float a, float b)
            {
                return Mathf.Abs(a - b) <= tolerance;
            }

            float CrossProduct2D(Vector2 a, Vector2 b)
            {
                return a.x * b.y - b.x * a.y;
            }
        }
    }
}