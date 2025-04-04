using System;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    /// <summary>
    /// Utility functions for working with paths of polygons
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Return the lenght of the path (which is considered a closed loop).
        /// </summary>
        public static float Length(Vector2[] path)
        {
            float length = 0;
            for (int i = 0; i < path.Length; i++)
            {
                int nextIndex = (i + 1) % path.Length;
                length += Vector2.Distance(path[i], path[nextIndex]);
            }
            return length;
        }

       

        public static bool IsCounterClockwise(Vector2[] path) => IsCounterClockwise(ToPathD(path));
        public static bool IsClockWise(Vector2[] path) => IsClockwise(ToPathD(path));

        
        
        internal static Vector2[] ToVector2Array(PathD path)
        {
            return path.Select(p => new Vector2((float)p.x, (float)p.y)).ToArray();
        }
        
        internal static bool IsCounterClockwise(PathD path) => Clipper.IsPositive(path);
        internal static bool IsClockwise(PathD path) => !Clipper.IsPositive(path);
        
        internal static PathD ToPathD(IEnumerable<Vector2> points) => new (points.Select(ToPointD));

        private static PointD ToPointD(Vector2 point) => new (point.x, point.y);

    }
}