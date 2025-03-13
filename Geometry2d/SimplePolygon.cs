using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Clipper2Lib;

namespace RikusGameDevToolbox.Geometry2d
{
    /// A polygon in 2D space
    /// No holes, self-intersections or duplicate points allowed.
    /// Uses Angus Johnson's awesome Clipper2 library..
    [Serializable]
    public class SimplePolygon : Polygon, IEquatable<SimplePolygon>
    {
        [SerializeField]
        private List<Vector2> _points;
        private const float Epsilon = 0.01f;

        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        /// <summary>
        /// Constructor for a polygon with the given points. 
        /// </summary>
        /// <param name="points">Points of the of polygon in counter-clockwise order.</param>
        public SimplePolygon(IEnumerable<Vector2> points)
        {
            SetPoints(points);
        }
        
        public void SetContour(IEnumerable<Vector2> points)
        {
            SetPoints(points);
        }


         
        public bool IsIntersecting(SimplePolygon other)
        {
            var polygon = this;
            return _points.Any(p => other.IsPointInside(p) || other.IsPointOnEdge(p)) || 
                   other._points.Any(p => polygon.IsPointInside(p) || polygon.IsPointOnEdge(p));
        }



        /// <summary>
        /// Return true if all the points of this polygon are inside or on the edge of the other polygon. 
        /// </summary>
        public bool IsInsideOf(Polygon other)
        {
            return _points.All(p=> other.IsPointInside(p) || other.IsPointOnEdge(p));
        }
  
 

        public bool IsSharingVerticesWith(SimplePolygon other)
        {
           return _points.Any(point1 => other._points.Any(point2 => SamePoint(point1,point2)));
           
           bool SamePoint(Vector2 p1, Vector2 p2) => Vector2.Distance(p1, p2) < Epsilon;
        }
        
     

        public bool IsConvex()
        {
            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 p0 = _points[i];
                Vector2 p1 = _points[(i + 1) % _points.Count];
                Vector2 p2 = _points[(i + 2) % _points.Count];

                Vector2 v1 = p1 - p0;
                Vector2 v2 = p2 - p1;

                float crossProduct = v1.x * v2.y - v1.y * v2.x;
                if (crossProduct > 0f) return false;
            }
            return true;
        }

        public SimplePolygon Translate(Vector2 offset)
        {
            return ForEachPoint(p => p + offset);
        }

        public SimplePolygon Transform(Transform transform)
        {
            return ForEachPoint(p => transform.TransformPoint(p));
        }

        public SimplePolygon InverseTransform(Transform transform)
        {
            return ForEachPoint(p => transform.InverseTransformPoint(p));
        }

        public SimplePolygon MakeCopy()
        {
            return new SimplePolygon(_points);
        }

        /// <summary>
        /// Return new polygon with the given method applied to each point.
        /// </summary>
        public SimplePolygon ForEachPoint(Func<Vector2, Vector2> func)
        {
            return new SimplePolygon(_points.Select(func));
        }

        public int NumSharedVerticesWith(SimplePolygon other)
        {
            //Todo: Make more efficient
            int result = 0;
            foreach (var myPoint in _points)
            {
                foreach (var theirPoint in other._points)
                {
                    if (myPoint==theirPoint) result++;
                }
            }
            return result;
        }
        
        public Vector2 AverageOfPoints()
        {
            Vector2 sum = Vector2.zero;
            foreach (var point in _points)
            {
                sum += point;
            }
            return sum / _points.Count;
        }
        
        public Vector2 Centroid()
        {
            // https://stackoverflow.com/a/34732659

            var centroid = Vector2.zero;
            float area = 0;

            for (int i = 0; i < _points!.Count; i++)
            {
                int i2 = i == _points.Count - 1 ? 0 : i + 1;

                float xi = _points[i].x;
                float yi = _points[i].y;
                float xi2 = _points[i2].x;
                float yi2 = _points[i2].y;

                float mult = (xi * yi2 - xi2 * yi) / 3f;
        
                Vector2 add = mult * new Vector2( xi + xi2, yi + yi2 );
             
                float addArea = xi * yi2 - xi2 * yi;

                if (i == 0)
                {
                    centroid = add;
                    area = addArea;
                }
                else
                {
                    centroid += add;
                    area += addArea;
                }
            }
          
            centroid /= area;

            return centroid;
        }
        
        
        // TODO: Make more efficient
        public bool Equals(SimplePolygon other)
        {
            if (other == null) return false;
            bool sameNumberOfPoints = _points.Count == other._points.Count;
            bool samePoints = _points.All(point => other._points.Contains(point));
            return sameNumberOfPoints && samePoints;
        }
        

    
        
        #endregion
        
        #region ----------------------------------------- INTERNAL METHODS ---------------------------------------------

        internal SimplePolygon(PathD path)
        {
            if (!Clipper.IsPositive(path))
            {
                 Debug.LogError("Wrong winding order for creating SimplePolygon from pathD.");
            }
            // make copy of path
            PathD pathCopy = new PathD(path);
            Paths = new PathsD { pathCopy };
            _points = new List<Vector2>();
            foreach (var point in Paths[0])
            {
                _points.Add(new Vector2((float)point.x, (float)point.y));
            }
        }
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
      
        private void SetPoints(IEnumerable<Vector2> points)
        {
            _points = new List<Vector2>(points);
            if (_points.Count() < 3)
            {
                throw new ArgumentException("A polygon must have at least 3 sides.");
            }
            var path = ToPathD(_points);
            if (!Clipper.IsPositive(path)) throw new ArgumentException("Polygon's points must be given in counter-clockwise order.");
            Paths = new PathsD { path };
        }
           




   


        #endregion

        
        
        
    }
}