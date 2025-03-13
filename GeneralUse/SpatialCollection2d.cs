using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    // This was spat out by AI, not sure if it works
    
    
    public class SpatialCollection2d<T>
    {
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

        private Node _root;

        public void Insert(Vector2 position, T item)
        {
            _root = Insert(_root, position, item,  0);
        }
        
        public List<T> ItemsIn(Rect area)
        {
            var result = new List<T>();
            RangeSearch(_root, area, 0, result);
            return result;
        }
        
        public List<T> ToList()
        {
            var result = new List<T>();
            Process(_root);
            return result;

            void Process(Node node)
            {
                if (node == null) return;
                result.Add(node.Item);
                Process(node.Left);
                Process(node.Right);
            }
        }

        private Node Insert(Node node, Vector2 point, T item, int depth)
        {
            if (node == null)
                return new Node(point, item);

            int axis = depth % 2;
            if (axis == 0)
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
            if (node == null)
                return;

            if (range.Contains(node.Point))
                result.Add(node.Item);

            int axis = depth % 2;
            if (axis == 0)
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
    }
}