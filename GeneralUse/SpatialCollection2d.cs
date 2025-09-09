using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// A collection that organizes items based on their 2d position thus allowing for fast queries for
    /// items in given area. The collection is implemented as k-d tree.
    /// </summary>
    public class SpatialCollection2d<T> : IEnumerable<T>
    {
        private class Node
        {
            public Vector2 Position;
            public Node Left;
            public Node Right;
            public T Item;
        }
        
        private enum Dimension { X, Y }

        private Node _root;
        private bool _nodeWasFound;


        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public void Add(Vector2 position, T item)
        {
            _root = Insert(_root, position, item,  0);
        }

  
        /// <summary>
        /// Removes an item at given position. Throws an exception if the item is not found.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Remove(Vector2 position, T item)
        {
            _nodeWasFound = false;
            _root = Remove(_root, position, item, 0);
            if (!_nodeWasFound) throw new InvalidOperationException("Item not found");
        }

        private Node Remove(Node node, Vector2 position, T item, int depth)
        {
            if (node == null) return null;

            if (node.Item.Equals(item))
            {
                _nodeWasFound = true;
                if (node.Right != null)
                {
                    Node min = FindMinimum(node.Right, CuttingDimensionInDepth(depth), depth + 1);
                    node.Position = min.Position;
                    node.Item = min.Item;
                    node.Right = Remove(node.Right, min.Position, min.Item, depth + 1);
                }
                else if (node.Left != null)
                {
                    Node min = FindMinimum(node.Left, CuttingDimensionInDepth(depth), depth + 1);
                    node.Position = min.Position;
                    node.Item = min.Item;
                    node.Right = Remove(node.Left, min.Position, min.Item, depth + 1);
                    node.Left = null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (CuttingDimensionInDepth(depth) == Dimension.X)
                {
                    if (position.x < node.Position.x)
                        node.Left = Remove(node.Left, position, item, depth + 1);
                    else
                        node.Right = Remove(node.Right, position, item, depth + 1);
                }
                else
                {
                    if (position.y < node.Position.y)
                        node.Left = Remove(node.Left, position, item, depth + 1);
                    else
                        node.Right = Remove(node.Right, position, item, depth + 1);
                }
            }

            return node;
        }

        public void Clear()
        {
            _root = null;
        }
        
        public List<T> ItemsInRectangle(Rect area)
        {
            var result = new List<Node>();
            RangeSearch(_root, area, 0, result);
            return result.Select(node => node.Item).ToList();
        }
        
        public List<T> ItemsInCircle(Vector2 center, float radius)
        {
            var result = new List<Node>();
            RangeSearch(_root, new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), 0, result);
            return result.Where(node => Vector2.Distance(center, node.Position) <= radius)
                         .Select(node => node.Item)
                         .ToList();
        }

    
        /// <summary> Returns the item closest to the given position. Returns null if the collection is empty.</summary>
        public T Closest(Vector2 position)
        {
            return Closest(_root, position, 0, _root).Item;
        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        private Node Closest(Node node, Vector2 position, int depth, Node best)
        {
            if (node == null) return best;

            if (SqrDistance(node.Position, position) < SqrDistance(best.Position, position))
            {
                best = node;
            }

            Node goodSide, badSide;
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (position.x < node.Position.x)
                {
                    (goodSide, badSide) = (node.Left, node.Right);
                }
                else
                {
                    (goodSide, badSide) = (node.Right, node.Left);
                }
            }
            else
            {
                if (position.y < node.Position.y)
                {
                    (goodSide, badSide) = (node.Left, node.Right);
                }
                else
                {
                    (goodSide, badSide) = (node.Right, node.Left);
                }
            }

            best = Closest(goodSide, position, depth + 1, best);

            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (Math.Abs(position.x - node.Position.x) < Vector2.Distance(best.Position, position))
                {
                    best = Closest(badSide, position, depth + 1, best);
                }
            }
            else
            {
                if (Math.Abs(position.y - node.Position.y) < Vector2.Distance(best.Position, position))
                {
                    best = Closest(badSide, position, depth + 1, best);
                }
            }

            return best;
            
            
            float SqrDistance(Vector2 a, Vector2 b) => (a - b).sqrMagnitude;
        }
        
        private Node FindMinimum(Node node, Dimension dimension, int currentDepth)
        {
            if (node == null)
                return null;
        
            if (CuttingDimensionInDepth(currentDepth) == dimension)
            {
                if (node.Left == null)
                    return node;
                return FindMinimum(node.Left, dimension,currentDepth + 1);
            }
                
            Node left = FindMinimum(node.Left, dimension,currentDepth + 1);
            Node right = FindMinimum(node.Right, dimension,currentDepth + 1);
            Node smallerChild = Minimum(left, right);
            return Minimum(node, smallerChild);

            Node Minimum(Node a, Node b)
            {
                if (a==null && b==null) return null;
                if (a == null) return b;
                if (b == null) return a;
                
                if (dimension == Dimension.X)
                {
                    return a.Position.x < b.Position.x ? a : b;
                } 
                return a.Position.y < b.Position.y ? a : b;
            }
        }
        
    

        private Node Insert(Node node, Vector2 point, T item, int depth)
        {
            if (node == null) return new Node{Position = point, Item = item};
          
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (point.x < node.Position.x)
                    node.Left = Insert(node.Left, point, item, depth + 1);
                else
                    node.Right = Insert(node.Right, point, item, depth + 1);
            }
            else
            {
                if (point.y < node.Position.y)
                    node.Left = Insert(node.Left, point, item, depth + 1);
                else
                    node.Right = Insert(node.Right, point, item, depth + 1);
            }

            return node;
        }

        
        


        private void RangeSearch(Node node, Rect range, int depth, List<Node> result)
        {
            if (node == null) return;

            if (range.Contains(node.Position)) result.Add(node);

           
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (range.xMin <= node.Position.x)
                    RangeSearch(node.Left, range, depth + 1, result);
                if (range.xMax >= node.Position.x)
                    RangeSearch(node.Right, range, depth + 1, result);
            }
            else
            {
                if (range.yMin <= node.Position.y)
                    RangeSearch(node.Left, range, depth + 1, result);
                if (range.yMax >= node.Position.y)
                    RangeSearch(node.Right, range, depth + 1, result);
            }
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            return Process(_root).GetEnumerator();

            IEnumerable<T> Process(Node node)
            {
                if (node == null) yield break;
                yield return node.Item;
                foreach (var item in Process(node.Left)) yield return item;
                foreach (var item in Process(node.Right)) yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        
        
        private Dimension CuttingDimensionInDepth(int depth) => depth % 2 == 0 ? Dimension.X : Dimension.Y;
        
        #endregion

  
    }
}