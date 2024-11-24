using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class RectExtensions
    {
        public static Rect Grow(this Rect r, float amount)
        {
            Vector2 position = r.position - new Vector2(amount * 0.5f, amount * 0.5f);
            Vector2 size = r.size + new Vector2(amount, amount);
            return new Rect(position, size);
        }

        
        public static void Bound(this ref Rect r, IEnumerable<Vector2> points)
        {
            r.min = points.First();
            r.max = points.First();
            foreach (var point in points)
            {
                if (point.x < r.min.x) r.min = new Vector2(point.x, r.min.y);
                if (point.y < r.min.y) r.min = new Vector2(r.min.x, point.y);
                if (point.x > r.max.x) r.max = new Vector2(point.x, r.max.y);
                if (point.y > r.max.y) r.max = new Vector2(r.max.x, point.y);
            }
        }
     
        
        public static Rect Shrink(this Rect r, float amount) => Grow(r, -amount);
    }
}