
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class RectExtensions
    {
        // HELPER FUNCTIONS:
        
        
        public static Rect CreateRectToEncapsulate(IEnumerable<Vector2> points)
        {
            if (!points.Any()) throw new ArgumentException("Cannot create a rect from an empty set of points.");
            Rect r = new Rect(points.First(), Vector2.zero);
            foreach (var point in points)
            {
                r.GrowToEncapsulate(point);
            }
            return r;
        }
        
        public static Rect CreateRectToEncapsulate(Vector2 pointA, Vector2 pointB)
        {
            Rect r = new Rect(pointA, Vector2.zero);
            r.GrowToEncapsulate(pointB);
            return r;
        }
        
        public static Rect CreateRectToEncapsulate(IEnumerable<Rect> rects)
        {
            if (!rects.Any()) throw new ArgumentException("Cannot create a rect from an empty set of rects.");
            Rect result = new Rect(rects.First());
            foreach (var rect in rects)
            {
                result.GrowToEncapsulate(rect);
            }
            return result;
        }


        // EXTENSIONS:
        /// <summary>
        /// Returns the common area (intersection) with the other rect (. If there is no common area, returns Rect.zero.
        /// </summary>
        public static Rect IntersectionWith(this Rect r, Rect other)
        {
            Vector2 min = new Vector2(Mathf.Max(r.min.x, other.min.x), Mathf.Max(r.min.y, other.min.y));
            Vector2 max = new Vector2(Mathf.Min(r.max.x, other.max.x), Mathf.Min(r.max.y, other.max.y));
            if (min.x>max.x || min.y>max.y) return Rect.zero;
            return CreateRectToEncapsulate(min, max);
        }

        /// <summary>
        /// Return the rect grown by the given amount in x and y directions i.e.
        /// all the rect sides will move by half of the amount.
        /// </summary>
        public static Rect Grow(this Rect r, float amount)
        {
            Vector2 position = r.position - new Vector2(amount * 0.5f, amount * 0.5f);
            Vector2 size = r.size + new Vector2(amount, amount);
            return new Rect(position, size);
        }

        /// <summary>
        /// Grow the Rect so that it encapsulates the given point.
        /// </summary>
        public static void GrowToEncapsulate(this ref Rect r, Vector2 point)
        {
           if (point.x < r.min.x) r.min = new Vector2(point.x, r.min.y);
           if (point.y < r.min.y) r.min = new Vector2(r.min.x, point.y);
           if (point.x > r.max.x) r.max = new Vector2(point.x, r.max.y);
           if (point.y > r.max.y) r.max = new Vector2(r.max.x, point.y);
        }
        
        /// <summary>
        /// Grow the Rect so that it encapsulates the other rect.
        /// </summary>
        public static void GrowToEncapsulate(this ref Rect r, Rect other)
        {
            if (other.min.x < r.min.x) r.min = new Vector2(other.min.x, r.min.y);
            if (other.min.y < r.min.y) r.min = new Vector2(r.min.x, other.min.y);
            if (other.max.x > r.max.x) r.max = new Vector2(other.max.x, r.max.y);
            if (other.max.y > r.max.y) r.max = new Vector2(r.max.x, other.max.y);
        }
       


        public static Vector2 RandomPointInside(this Rect r)
        {
            return new Vector2(Random.Range(r.min.x, r.max.x), Random.Range(r.min.y, r.max.y));
        }
     
        
        public static Rect Shrink(this Rect r, float amount) => Grow(r, -amount);
    }
}