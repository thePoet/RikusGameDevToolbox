using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// Undirected graph is a collection of nodes with undirected connections (links) between them.
    /// </summary>
    public class UndirectedGraph<T> where T : IEquatable<T>
    {
        private readonly Dictionary<T, HashSet<T>> _links = new();
        private readonly PathFinding<T> _pathFinding;
        
        public int NumNodes => _links.Count;
        public List<T> Nodes => _links.Keys.ToList();


        #region ------------------------------------------ PUBLIC METHODS -----------------------------------------------
        
        public UndirectedGraph()
        {
            _pathFinding = new PathFinding<T>
            (
                Links,
                (from, to) => 1f,
                (from, to) => 1f
            );
        }
        
        public void AddNode(T node)
        {
            if (!_links.TryAdd(node, new HashSet<T>()))
            {
                throw new ArgumentException("Node already exists in the graph.");
            }
        }
      
        public void RemoveNode(T node)
        {
            if (!_links.ContainsKey(node))
            {
                throw new ArgumentException("Tried to remove non-existing node from the graph.");
            }
            RemoveAllLinks(node);
            _links.Remove(node);
        }
        
        public bool ContainsNode(T node)
        {
            return _links.ContainsKey(node);
        }
        
        public void Clear()
        {
            _links.Clear();
        }
        
        /// <summary>
        /// Merges the nodes and links of the other graph into this graph.
        /// </summary>
        public void Merge(UndirectedGraph<T> other)
        {
            foreach (var node in other.Nodes)
            {
                AddNode(node);
            }

            foreach (var node in other.Nodes)
            {
                foreach (var linkedNode in other.Links(node))
                {
                    _links[node].Add(linkedNode);
                }
            }
        }
        
        public void AddLink(T node1, T node2)
        {
            if (!_links.ContainsKey(node1) || !_links.ContainsKey(node2))
            {
                throw new ArgumentException("Tried to add link with non-existing node(s).");
            }
            
            if (_links[node1].Contains(node2))
            {
                throw new ArgumentException("Link already exists.");
            }
            
            _links[node1].Add(node2);
            _links[node2].Add(node1);
        }

        public void RemoveLink(T id1, T id2)
        {
            if (!_links.ContainsKey(id1) || !_links.ContainsKey(id2))
            {
                throw new ArgumentException("Tried to remove link with non-existing node(s).");
            }
            
            if (!_links[id1].Contains(id2))
            {
                throw new ArgumentException("Tried to remove non-existing link.");
            }
            
            _links[id1].Remove(id2);
            _links[id2].Remove(id1);
        }

        /// <summary>
        /// Returns the nodes linked to the given node. 
        /// </summary>
        public List<T> Links(T node)
        {
            return _links[node].ToList();
        }
        
        public bool AreLinked(T node1, T node2)
        {
            return _links[node1].Contains(node2);
        }
        
        public bool PathExist(T node1, T node2)
        {
            return Path(node1,node2) != null;
        }
        
        /// <summary>
        /// Return a shortest path between the nodes (including the start and goal nodes).
        /// Returns null if there is no path.
        /// </summary>
        public List<T> Path(T startNode, T endNode)
        {
            return _pathFinding.GetPath(startNode, endNode);
        }
        
        /// <summary>
        /// Return true if all nodes in the graph are connected to each other (not necessary directly but via intermediate
        /// nodes). Return true if the graph is empty.
        /// </summary>
        public bool IsConnected()
        {
            if (_links.Count == 0) return true;
            return _pathFinding.GetAllNodesConnectedTo(_links.Keys.First()).Count == _links.Count;
        }

        /// <summary>
        /// Returns the graph as list of connected graphs. 
        /// </summary>
        public List<UndirectedGraph<T>> GraphsOfConnectedNodes()
        {
            var groups = ConnectedNodes();

            var result = new List<UndirectedGraph<T>>();

            // sort groups by descending number of nodes:
            groups.Sort((a, b) => b.Count - a.Count);

            foreach (var group in groups)
            {
                result.Add( CreateGraph(group) );
            }

            return result;
            
            
            // Returns a list of groups of connected nodes.
            List<List<T>> ConnectedNodes()
            {
                List<List<T>> list = new List<List<T>>();
                HashSet<T> toBeProcessed = new(_links.Keys);

                while (toBeProcessed.Count > 0)
                {
                    var group = _pathFinding.GetAllNodesConnectedTo(toBeProcessed.First());
                    toBeProcessed.ExceptWith(group);
                    list.Add(group);
                }

                return list;
            }
            
            UndirectedGraph<T> CreateGraph(List<T> nodes)
            {
                var graph = new UndirectedGraph<T>();
                foreach (var node in nodes)
                {
                    graph._links.Add(node, new HashSet<T>(_links[node]));
                }
                return graph;
            }
        }
        
        /// <summary>
        /// Returns a new graph with the same links but with the nodes transformed to a new type.
        /// </summary>
        /// <param name="nodeTransformer"></param>
        /// <typeparam name="TNew">New type for nodes</typeparam>
        /// <returns>The new UndirectedGraph</returns>
        public UndirectedGraph<TNew> ConvertNodes<TNew>(Func<T,TNew> nodeTransformer) where TNew : IEquatable<TNew>
        {
            var newGraph = new UndirectedGraph<TNew>();

            Dictionary<T, TNew> lookup = new Dictionary<T, TNew>();
                
            foreach (var node in Nodes)
            {
                var newNode = nodeTransformer(node);
                newGraph.AddNode(newNode);
                lookup.Add(node, newNode);
            }

            foreach (var node in Nodes)
            {
                TNew a = lookup[node];
                foreach (var linkedNode in Links(node))
                {
                    TNew b = lookup[linkedNode]; 
                    if (newGraph.AreLinked(a,b)) continue;
                    newGraph.AddLink(a,b);
                }
            }

            return newGraph;
        }

        #endregion
        #region ------------------------------------------ PRIVATE METHODS ----------------------------------------------
        
        private void RemoveAllLinks(T node)
        {
            while (_links[node].Count > 0)
            {
                var connected = _links[node].First();
                _links[connected].Remove(node);
                _links[node].Remove(connected);
            }
        }
        
        #endregion
        
      
   

    }

}