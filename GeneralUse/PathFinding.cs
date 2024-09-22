using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;


namespace RikusGameDevToolbox.GeneralUse
{

    /// <summary>
    /// Implements A* algorithm to find shortest path between nodes of the type T.
    /// Not particularly efficient, but works for small graphs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PathFinding<T> where T : IEquatable<T>
    {
        // Information attached to each node for the purpose
        // of tracking distances and the shortest known
        // way the to starting point.
        private class NodeTag
        {
            public T Node; 
            public float DistanceToStart; // Distance from start to this node
            public float DistanceEstimateToGoal; // Estimated distance from this position to goal
            public NodeTag Parent; // NodeTag that is the first step of
                                   // the fastest known wa to the start.

            public float SumDistances => DistanceToStart + DistanceEstimateToGoal;
        }

        private class NodeList
        {
            private readonly Dictionary<int, T> _nodes = new();

            public void Add(T node) => _nodes.Add(node.GetHashCode(), node);
            public void Remove(T node) => _nodes.Remove(node.GetHashCode());
            public bool Contains(T node) => _nodes.ContainsKey(node.GetHashCode());
            public int Count => _nodes.Count;
            public T Pop()
            {
                var first = _nodes.First();
                _nodes.Remove(first.Key);
                return first.Value;
            }
            
            public List<T> AsList()
            {
                List<T> result = new();
                foreach (var node in _nodes.Values)
                {
                    result.Add(node);
                }
                return result;
            }
        }

        public delegate float MovementCost(T from, T to);

        public delegate IEnumerable<T> TraversableNeighbours(T node);

        private readonly MovementCost _movementCost;
        private readonly MovementCost _movementCostEstimate;
        private readonly TraversableNeighbours _traversableNeighbours;

       
        
        /// <param name="traversableNodes">Method that returns IEnumerable of nodes that can be reached from the node
        /// given as parameter.</param>
        /// <param name="movementCost">Method that returns the movement cost between the two given adjacent nodes.</param>
        /// <param name="movementCostEstimate">Rough estimate of movement cost between two given nodes that
        /// aren't necessarily adjacent or even connected at all.</param>
        public PathFinding(TraversableNeighbours traversableNodes, MovementCost movementCost,
            MovementCost movementCostEstimate)
        {
            _traversableNeighbours = traversableNodes;
            _movementCost = movementCost;
            _movementCostEstimate = movementCostEstimate;
        }

        // Return a path between given position as a list of positions, including the start and goal.
        // Returns null if no path is found.
        public List<T> GetPath(T start, T goal, float maxDistance = float.PositiveInfinity)
        {
            NodeList open = new();
            NodeList closed = new();
            Dictionary<T, NodeTag> tags = new Dictionary<T, NodeTag>();

            open.Add(start);
            tags.Add(start, new NodeTag() { Node = start, DistanceToStart = 0, Parent = null });

            while (open.Count > 0)
            {
                NodeTag n = NodeWithShortestDistance(open);
                closed.Add(n.Node);
                open.Remove(n.Node);
            

                if (n.Node.Equals(goal)) return PathFromStartTo(n);
                

                foreach (var neighbour in _traversableNeighbours(n.Node))
                {
                    if (closed.Contains(neighbour)) continue;
                    float distanceToStart = n.DistanceToStart + _movementCost(n.Node, neighbour);
                    if (distanceToStart > maxDistance) continue;
                    
                    if (!open.Contains(neighbour))
                    {
                        NodeTag tag = new NodeTag()
                        {
                            Node = neighbour,
                            DistanceToStart = distanceToStart,
                            DistanceEstimateToGoal = _movementCostEstimate(neighbour, goal),
                            Parent = n
                        };
                        tags.Add(neighbour, tag);
                        open.Add(neighbour);
                        continue;
                    }

                    if (n.DistanceToStart > distanceToStart)
                    {
                        n.DistanceToStart = distanceToStart;
                        n.Parent = n;
                    }

                }
            }
            return null;

            // Returns node with shortest estimated distance from start to goal from the List
            // INEFFICIENT!
            NodeTag NodeWithShortestDistance(NodeList nodeTagList)
            {
                float smallestF = float.PositiveInfinity;
                T shortest = default(T);

                foreach (var node in nodeTagList.AsList())
                {
                    float f  = tags[node].SumDistances;
                    if (f < smallestF)
                    {
                        smallestF = f;
                        shortest = node;
                    }
                }
                if (shortest != null) return tags[shortest];
                return null;
            }
            
            // Returns a list of nodes from the start to the given node
            List<T> PathFromStartTo(NodeTag nodeTag)
            {
                List<T> result = new List<T> { nodeTag.Node };

                while (nodeTag.Parent != null)
                {
                    result.Insert(0, nodeTag.Parent.Node);
                    nodeTag = nodeTag.Parent;
                }
                return result;
            }
           
       
        }

        /// <summary>
        /// Includes the given node.
        /// </summary>
        public List<T> GetAllNodesConnectedTo(T node)
        {
            NodeList open = new(); 
            NodeList closed = new();

            open.Add(node);

            while (open.Count > 0)
            {
                node = open.Pop();
                closed.Add(node);

                foreach (var neighbour in _traversableNeighbours(node))
                {
                    if (!open.Contains(neighbour) && !closed.Contains(neighbour)) open.Add(neighbour);
                }
            }
            return closed.AsList();
        }
        

    }
}