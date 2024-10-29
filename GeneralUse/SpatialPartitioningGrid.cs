using System;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public class SpatialPartitioningGrid<T>
    {
        private readonly T[] _entities;
        private readonly Vector2[] _positions;
        private readonly int[] _numEntitiesInSquare;
        private readonly int _maxNumEntitiesInSquare;
        private readonly Grid2D _grid;


        public SpatialPartitioningGrid(Rect area, float squareSize, int maxNumEntitiesInSquare)
        {
            _grid = new Grid2D(area, squareSize);
            _maxNumEntitiesInSquare = maxNumEntitiesInSquare;
            _entities = new T[_grid.NumberOfSquares * maxNumEntitiesInSquare];
            _positions = new Vector2[_grid.NumberOfSquares * maxNumEntitiesInSquare];
            _numEntitiesInSquare = new int[_grid.NumberOfSquares];

            Clear();
        }
        

        public void Clear()
        {
            for (int i = 0; i < _numEntitiesInSquare.Length; i++)
            {
                _numEntitiesInSquare[i] = 0;
            }
        }



        /// <summary>
        ///  returns entities inside the given rectangle
        /// </summary>
        public T[] RectangleContents(Rect rect)
        {
            List<T> result = new List<T>();
        
            foreach (var squareIdx in _grid.SquareIndicesInRect(rect))
            {
                int firstIdx = squareIdx * _maxNumEntitiesInSquare;
            
                for (int i=0; i<_numEntitiesInSquare[squareIdx]; i++)
                {
                    int idx = firstIdx + i;
                    if (rect.Contains(_positions[idx]))
                    {
                        result.Add(_entities[idx]);
                    }
                }
            }

            return result.ToArray();
        }
    
        /// <summary>
        /// Return entities within given radius of a position.
        /// </summary>
        public T[] CircleContents(Vector2 position, float radius)
        {
            var minCorner = position - Vector2.one * radius;
            var maxCorner = position + Vector2.one * radius;
            Rect rect = new Rect(minCorner, maxCorner - minCorner);

            List<T> result = new List<T>();
        
            foreach (var squareIdx in _grid.SquareIndicesInRect(rect))
            {
                int firstIdx = squareIdx * _maxNumEntitiesInSquare;
            
                for (int i=0; i<_numEntitiesInSquare[squareIdx]; i++)
                {
                    int idx = firstIdx + i;
                    if (Vector2.Distance(position, _positions[idx]) <= radius)
                    {
                        result.Add(_entities[idx]);
                    }
                }
            }
        
            return result.ToArray();
        }

        
        public void Add(T entity, Vector2 position, bool ignoreIfOutsideGrid=false)
        {
            if (!_grid.IsInGrid(position))
            {
                if (!ignoreIfOutsideGrid) Debug.LogWarning("Entity is outside the grid");
                return;
            }

            int cellIndex = _grid.SquareIndex(position);

            if (_numEntitiesInSquare[cellIndex] >= _maxNumEntitiesInSquare)
            {
                Debug.LogWarning("Too many entities in square");
                return;
            }

            int hash = cellIndex * _maxNumEntitiesInSquare + _numEntitiesInSquare[cellIndex];
            _entities[hash] = entity;
            _positions[hash] = position;
 
            _numEntitiesInSquare[cellIndex]++;
        }
        
        private Span<T> GridSquareContents(int squareIndex)
        {
            return new Span<T>(_entities, squareIndex * _maxNumEntitiesInSquare, _numEntitiesInSquare[squareIndex]);
        }

       
        
        
        public int NumSquares => _grid.NumberOfSquares;
    

    
   
    }
}