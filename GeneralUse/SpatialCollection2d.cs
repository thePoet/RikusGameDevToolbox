using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public class SpatialCollection2d<T> : IEnumerable<T>
    {
        //TODO: Implement actual deletion
        
        private class Node
        {
            public Vector2 Point;
            public Node Left;
            public Node Right;
            public T Item;
       

            public Node(Vector2 point, T item)
            {
                Point = point;
                Item = item;
            }
        }
        
        private enum Dimension { X, Y }

        private Node _root;


        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------

        public void Insert(Vector2 position, T item)
        {
            _root = Insert(_root, position, item,  0);
        }

  
        public void Remove(Vector2 position, T item)
        {
            _root = Remove(_root, position, item, 0);
        }

        private Node Remove(Node node, Vector2 position, T item, int depth)
        {
            if (node == null) return null;

            if (node.Item.Equals(item))
            {
                if (node.Right != null)
                {
                    Node min = FindMinimum(node.Right, CuttingDimensionInDepth(depth), depth + 1);
                    node.Point = min.Point;
                    node.Item = min.Item;
                    node.Right = Remove(node.Right, min.Point, min.Item, depth + 1);
                }
                else if (node.Left != null)
                {
                    Node min = FindMinimum(node.Left, CuttingDimensionInDepth(depth), depth + 1);
                    node.Point = min.Point;
                    node.Item = min.Item;
                    node.Right = Remove(node.Left, min.Point, min.Item, depth + 1);
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
                    if (position.x < node.Point.x)
                        node.Left = Remove(node.Left, position, item, depth + 1);
                    else
                        node.Right = Remove(node.Right, position, item, depth + 1);
                }
                else
                {
                    if (position.y < node.Point.y)
                        node.Left = Remove(node.Left, position, item, depth + 1);
                    else
                        node.Right = Remove(node.Right, position, item, depth + 1);
                }
            }

            return node;
        }
        
        public List<T> ItemsIn(Rect area)
        {
            var result = new List<T>();
            RangeSearch(_root, area, 0, result);
            return result;
        }
        
        // Return the item closest to the given position
        public T Closest(Vector2 position)
        {
            if (_root == null) throw new InvalidOperationException("Collection is empty");
            return Closest(_root, position, 0, _root).Item;
        }

        #endregion

        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        private Node Closest(Node node, Vector2 position, int depth, Node best)
        {
            if (node == null) return best;

            if (SqrDistance(node.Point, position) < SqrDistance(best.Point, position))
            {
                best = node;
            }

            Node goodSide, badSide;
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (position.x < node.Point.x)
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
                if (position.y < node.Point.y)
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
                if (Math.Abs(position.x - node.Point.x) < Vector2.Distance(best.Point, position))
                {
                    best = Closest(badSide, position, depth + 1, best);
                }
            }
            else
            {
                if (Math.Abs(position.y - node.Point.y) < Vector2.Distance(best.Point, position))
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
                    return a.Point.x < b.Point.x ? a : b;
                } 
                return a.Point.y < b.Point.y ? a : b;
            }
        }
        
    

        private Node Insert(Node node, Vector2 point, T item, int depth)
        {
            if (node == null) return new Node(point, item);
          
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (point.x < node.Point.x)
                    node.Left = Insert(node.Left, point, item, depth + 1);
                else
                    node.Right = Insert(node.Right, point, item, depth + 1);
            }
            else
            {
                if (point.y < node.Point.y)
                    node.Left = Insert(node.Left, point, item, depth + 1);
                else
                    node.Right = Insert(node.Right, point, item, depth + 1);
            }

            return node;
        }

        
        


        private void RangeSearch(Node node, Rect range, int depth, List<T> result)
        {
            if (node == null) return;

            if (range.Contains(node.Point)) result.Add(node.Item);

           
            if (CuttingDimensionInDepth(depth) == Dimension.X)
            {
                if (range.xMin <= node.Point.x)
                    RangeSearch(node.Left, range, depth + 1, result);
                if (range.xMax >= node.Point.x)
                    RangeSearch(node.Right, range, depth + 1, result);
            }
            else
            {
                if (range.yMin <= node.Point.y)
                    RangeSearch(node.Left, range, depth + 1, result);
                if (range.yMax >= node.Point.y)
                    RangeSearch(node.Right, range, depth + 1, result);
            }
        }
        

       
 
 
        public IEnumerator<T> GetEnumerator()
        {
            foreach (Node node in NodeList())
            {
                yield return node.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        

        // TODO: Inefficient!!!
        private List<Node> NodeList()
        {
            var result = new List<Node>();
            Process(_root);
            return result;

            void Process(Node node)
            {
                if (node == null) return;
                result.Add(node);
                Process(node.Left);
                Process(node.Right);
            }
        }
        
        private Dimension CuttingDimensionInDepth(int depth) => depth % 2 == 0 ? Dimension.X : Dimension.Y;
        
        #endregion

  
    }
}