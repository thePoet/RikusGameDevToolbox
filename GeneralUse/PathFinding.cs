using System;
using System.Collections.Generic;



namespace RikusGameDevToolbox.GeneralUse
{
    // Implements A* algorithm to find shortest path between nodes of the type T
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
            public NodeTag parent; // NodeTag that is the first step of
                                   // the fastest known wa to the start.

            public float SumDistances => DistanceToStart + DistanceEstimateToGoal;
        }

        public delegate float MovementCost(T from, T to);

        public delegate IEnumerable<T> TraversableNeighbours(T node);

        private MovementCost _movementCost;
        private MovementCost _movementCostEstimate;
        private TraversableNeighbours _traversableNeighbours;

       
        
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

        // Return a path between given position as a list
        // of positions, including the start and goal.
        // Returns null if no path is found.
        public List<T> GetPath(T start, T goal, float maxDistance = float.PositiveInfinity)
        {
            List<T> open = new List<T>();
            List<T> closed = new List<T>();
            Dictionary<T, NodeTag> tags = new Dictionary<T, NodeTag>();

            open.Add(start);
            tags.Add(start, new NodeTag() { Node = start, DistanceToStart = 0, parent = null });



            while (open.Count > 0)
            {
                NodeTag n = NodeWithShortestDistance(open);
                closed.Add(n.Node);
                open.Remove(n.Node);
            

                if (n.Node.Equals(goal)) return PathToStartFrom(n);
                

                foreach (var neighbour in _traversableNeighbours(n.Node))
                {
                    if (IsInClosedList(neighbour)) continue;
                    float distanceToStart = n.DistanceToStart + _movementCost(n.Node, neighbour);
                    if (distanceToStart > maxDistance) continue;
                    
                    if (!IsInOpenList(neighbour))
                    {
                        NodeTag tag = new NodeTag()
                        {
                            Node = neighbour,
                            DistanceToStart = distanceToStart,
                            DistanceEstimateToGoal = _movementCostEstimate(neighbour, goal),
                            parent = n
                        };
                        tags.Add(neighbour, tag);
                        open.Add(neighbour);
                        continue;
                    }

                    if (n.DistanceToStart > distanceToStart)
                    {
                        n.DistanceToStart = distanceToStart;
                        n.parent = n;
                    }

                }
            }
            return null;

            // Returns node with shortest estimated distance from start to goal from the List
            NodeTag NodeWithShortestDistance(List<T> list)
            {
                float smallestF = float.PositiveInfinity;
                T node = default(T);

                for (int i = 0; i < list.Count; i++)
                {
                    float f  = tags[list[i]].SumDistances;
                    if (f < smallestF)
                    {
                        smallestF = f;
                        node = list[i];
                    }
                }

                return tags[node];
            }
            
            List<T> PathToStartFrom(NodeTag nodeTag)
            {
                List<T> result = new List<T>();
                result.Add(nodeTag.Node);

                while (nodeTag.parent != null)
                {
                    result.Insert(0, nodeTag.parent.Node);
                    nodeTag = nodeTag.parent;
                }
                return result;
            }
            
            bool IsInClosedList(T node) => closed.IndexOf(node) != -1;
            bool IsInOpenList(T node) => open.IndexOf(node) != -1;
        }
    }
}