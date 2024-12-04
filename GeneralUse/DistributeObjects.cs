using System;
using System.Collections.Generic;
using System.Linq;
using RikusGameDevToolbox.Geometry2d;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class DistributeObjects
    {
        /// <summary>
        /// Returns a list of Vector2 positions that are randomly yet tightly spaced in a area using
        /// Poisson disk sampling algorithm. 
        /// </summary>
        /// <param name="area"></param>
        /// <param name="minSpacing">Minimum distance between positions.</param>
        /// <param name="existingPoints">Already existing positions. Note that these are not included in the result.</param>
        public static List<Vector2> InRectangle(Rect area, float minSpacing, IEnumerable<Vector2> existingPoints = null)
        {
            Vector2 RandomPointInRect() => new Vector2(Random.Range(area.xMin, area.xMax),
                Random.Range(area.yMin, area.yMax));
            bool IsInArea(Vector2 position) => area.Contains(position);
            return Poisson(RandomPointInRect, minSpacing, IsInArea, existingPoints);
        }

        /// <summary>
        /// Returns a list of Vector2 positions that are randomly yet tightly spaced in a circle using
        /// Poisson disk sampling algorithm. 
        /// </summary>
        /// <param name="center">Center of the circle</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="minSpacing">Minimum distance between positions.</param>
        /// <param name="existingPoints">Already existing positions. Note that these are not included in the result.</param>
        public static List<Vector2> InCircle(Vector2 center, float radius, float minSpacing, IEnumerable<Vector2> existingPoints = null)
        {
            Vector2 RandomPointInCircle() => Random.insideUnitCircle * radius + center;
            bool IsInCircle(Vector2 position) => Vector2.Distance(position, center) < radius;
            return Poisson(RandomPointInCircle, minSpacing, IsInCircle, existingPoints);
        }

        /// <summary>
        /// Returns a list of Vector2 positions that are inside polygon using
        /// Poisson disk sampling algorithm.
        /// NOTE: This does not work well with polygons with complicated shapes.
        /// </summary>
        /// <param name="polygon">The polygon</param>
        /// <param name="minSpacing">Minimum distance between positions.</param>
        /// <param name="existingPoints">Already existing positions. Note that these are not included in the result.</param>
        public static List<Vector2> InPolygon(Polygon polygon, float minSpacing, IEnumerable<Vector2> existingPoints = null)
        {
            // TODO: the random function is not very good if polygon is small compared to it's bounding box
            Vector2 RandomPoint() => polygon.Bounds().RandomPointInside();
            bool IsInside(Vector2 position) => polygon.IsPointInside(position);
            return Poisson(RandomPoint, minSpacing, IsInside, existingPoints);
        }


        // Poisson disk sampling algorithm based on: http://devmag.org.za/2009/05/03/poisson-disk-sampling/
        private static List<Vector2> Poisson(Func<Vector2> randomPoint, float minSpacing, Func<Vector2, bool> isInArea,
        IEnumerable<Vector2> existingPoints = null, int numTriesToGeneratePoint = 50)
        {
            float cellSize = minSpacing / Mathf.Sqrt(2);
            Dictionary<(int, int), Vector2> grid = new();
            List<Vector2> existingPointsNotFittingInGrid = new();
            
            if (existingPoints != null)
            {
                foreach (var point in existingPoints)
                {
                    if (grid.ContainsKey(GridIndex(point)))
                    {
                        existingPointsNotFittingInGrid.Add(point);
                    }
                    else
                    {
                        grid.Add(GridIndex(point), point);
                    }
                }
            }
            
            List<Vector2> processList = new();
            List<Vector2> samplePoints = new();


            // The poinst are generating starting from the point randomized below.
            // TODO: If the rest of the area is not easily reachable from the first point, the result will be bad.
            for (int i = 0; i < numTriesToGeneratePoint; i++)
            {
                Vector2 firstPoint = randomPoint();
                if (!IsTooCloseToExistingPoints(firstPoint) && isInArea(firstPoint))
                {
                    processList.Add(firstPoint);
                    samplePoints.Add(firstPoint);
                    grid.Add(GridIndex(firstPoint), firstPoint);
                    break;
                }
            }

            while (processList.Count != 0)
            {
                var point = PopRandomFrom(processList);
                
                for (int i = 0; i < numTriesToGeneratePoint; i++)
                {
                    var newPoint = RandomPointInDoughnut(point, minSpacing, minSpacing*2f);
                    if (isInArea(newPoint) && !IsTooCloseToExistingPoints(newPoint))
                    {
                        processList.Add(newPoint);
                        samplePoints.Add(newPoint);
                        grid.Add(GridIndex(newPoint), newPoint);
                    }
         
                }
            }
         
            return samplePoints;
            

            (int, int) GridIndex(Vector2 point) => ((int)Mathf.Round(point.x / cellSize), (int)Mathf.Round(point.y / cellSize));
            
            Vector2 PopRandomFrom(List<Vector2> list)
            {
                int index = Random.Range(0, list.Count);
                Vector2 result = list[index];
                list.RemoveAt(index);
                return result;
            }

            bool IsTooCloseToExistingPoints(Vector2 point)
            {
                if (NearbyPoints(point).Any(p => IsTooClose(p,point))) return true;
                return existingPointsNotFittingInGrid.Any(p => IsTooClose(p,point));
            }
            
            bool IsTooClose(Vector2 point1, Vector2 point2) => Vector2.Distance(point1, point2) < minSpacing;
            
            // Returns points on grid that are within 2 grid cells of the given point i.e.
            // possibly within minSpacing distance.
            IEnumerable<Vector2> NearbyPoints(Vector2 point)
            {
                (int x, int y) = GridIndex(point);
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        if (grid.TryGetValue((x + i, y + j), out Vector2 nearbyPoint))
                        {
                            yield return nearbyPoint;
                        }
                    }
                }
            }
            
            Vector2 RandomPointInDoughnut(Vector2 center, float innerRadius, float outerRadius)
            { 
                // Distribution is non-uniform, favours points closer to the inner ring, which leads to denser packing.
                float radius = Random.Range(innerRadius, outerRadius);
                float angle = Random.Range(0, 2 * Mathf.PI);
                return new Vector2(center.x + radius * Mathf.Cos(angle), 
                    center.y + radius * Mathf.Sin(angle));
            }
        }
    }
}
