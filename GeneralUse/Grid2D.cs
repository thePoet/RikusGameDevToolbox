using UnityEngine;
using UnityEngine.Assertions;

namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// Represents a finite 2d grid. The squares of the grid  are represented by their
    /// x and y coordinates (0..SizeSquares.x and 0..SizeSquares.y) or their index (0..SizeSquares.x * SizeSquares.y - 1).
    /// </summary>
    public record Grid2D
    {
        /// <summary> The total number of squares in the grid. </summary>
        public int NumberOfSquares => SizeSquares.x * SizeSquares.y;

        /// <summary> The number of squares in the x and y axis of the grid. </summary>
        public Vector2Int SizeSquares { get; }

        /// <summary> The side length of the squares. </summary>
        public float SquareSize { get; }

        private readonly Vector2 _minCorner;
        private readonly Vector2 _maxCorner;

        public Grid2D(Vector2 origin, Vector2Int sizeSquares, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell sizeSquares must be greater than 0");
            Assert.IsTrue(sizeSquares is { x: > 0, y: > 0 }, "SizeSquares in cells must be greater than 0");

            _minCorner = origin;
            _maxCorner = origin + new Vector2(sizeSquares.x * squareSize, sizeSquares.y * squareSize);
            SizeSquares = sizeSquares;
            SquareSize = squareSize;
        }

        public Grid2D(Vector2 cornerMin, Vector2 cornerMax, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell sizeSquares must be greater than 0");
            _minCorner = cornerMin;
            _maxCorner = cornerMax;
            SquareSize = squareSize;
            SizeSquares = SizeInSquares(cornerMin, cornerMax, squareSize);
        }

        public Grid2D(Rect rect, float squareSize)
        {
            Assert.IsTrue(squareSize > 0f, "Cell sizeSquares must be greater than 0");
            _minCorner = rect.min;
            _maxCorner = rect.max;
            SquareSize = squareSize;
            SizeSquares = SizeInSquares(rect.min, rect.max, squareSize);
        }

        public Vector2Int SquareCoordinates(Vector2 position)
        {
            Vector2 relativePosition = position - _minCorner;
            return new Vector2Int(
                Mathf.FloorToInt(relativePosition.x / SquareSize),
                Mathf.FloorToInt(relativePosition.y / SquareSize)
            );
        }

        public Vector2Int SquareCoordinates(int cellIndex)
        {
            return new Vector2Int(cellIndex % SizeSquares.x, cellIndex / SizeSquares.x);
        }

        public int[] SquareIndicesInRect(Rect rect)
        {
            Vector2Int minCorner = SquareCoordinates(rect.min);
            Vector2Int maxCorner = SquareCoordinates(rect.max);
            
            minCorner.Clamp(Vector2Int.zero, SizeSquares - Vector2Int.one);
            maxCorner.Clamp(Vector2Int.zero, SizeSquares - Vector2Int.one);
            
       
            int numSquares = (maxCorner.x - minCorner.x + 1) * (maxCorner.y - minCorner.y + 1);
            var result = new int[numSquares];
            int i = 0;
            for (int x = minCorner.x; x<=maxCorner.x; x++)
            {
                for (int y=minCorner.y; y<=maxCorner.y; y++)
                {
                    result[i] = SquareIndex(new Vector2Int(x, y));
                    i++;
                }
            }

            return result;
        }

        public bool IsInGrid(Vector2 position)
        {

            return position.x >= _minCorner.x && position.x <= _maxCorner.x &&
                   position.y >= _minCorner.y && position.y <= _maxCorner.y;
        }

        public bool IsValidSquare(Vector2Int cellCoordinates)
        {
            return cellCoordinates.x >= 0 && cellCoordinates.x < SizeSquares.x && cellCoordinates.y >= 0 &&
                   cellCoordinates.y < SizeSquares.y;
        }

        public bool IsValidSquareIndex(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < NumberOfSquares;
        }

        public int SquareIndex(Vector2 position)
        {
           return SquareIndex( SquareCoordinates(position));
        }
        
        public int SquareIndex(Vector2Int squareCoordinates)
        {
            return squareCoordinates.x + squareCoordinates.y * SizeSquares.x;
        }

        private static Vector2Int SizeInSquares(Vector2 cornerMin, Vector2 cornerMax, float squareSize)
        {
            Assert.IsTrue(cornerMin.x < cornerMax.x && cornerMin.y < cornerMax.y, "Invalid corners");

            return new Vector2Int(
                Mathf.CeilToInt((cornerMax.x - cornerMin.x) / squareSize),
                Mathf.CeilToInt((cornerMax.y - cornerMin.y) / squareSize)
            );
        }

    }
}
