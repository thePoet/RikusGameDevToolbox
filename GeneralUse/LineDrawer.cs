using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class LineDrawer 
    {
        public static IEnumerable<Vector2Int> PointsInLine(Vector2Int from, Vector2Int to)
        {
            // Bresenham's line algorithm adapted from:
            // https://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
            int w = to.x - from.x;
            int h = to.y - from.y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Mathf.Abs(w);
            int shortest = Mathf.Abs(h);
            if (!(longest > shortest))
            {
                longest = Mathf.Abs(h);
                shortest = Mathf.Abs(w);
                if (h < 0) dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            int numerator = longest >> 1;
            Vector2Int[] result = new Vector2Int[longest + 1];
            for (int i = 0; i <= longest; i++)
            {
                result[i] = new Vector2Int(from.x, from.y);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    from.x += dx1;
                    from.y += dy1;
                }
                else
                {
                    from.x += dx2;
                    from.y += dy2;
                }
            }
            return result;
        }
    }
}