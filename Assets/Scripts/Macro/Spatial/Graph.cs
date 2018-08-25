using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace TerrainDemo.Voronoi
{
    /// <summary>
    /// Supports unique edges only
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class Graph<TData> : Network<Graph<TData>, Graph<TData>.Node, TData>
    {
        public Graph()
        {
            //Nodes = _nodes;
        }

        public Node Add(TData data)
        {
            var newNode = new Node(_nodes.Count, this);
            _nodes.Add(newNode);
            newNode.Data = data;
            return newNode;
        }

        public void AddEdge(Node from, Node to)
        {
            if(from.Parent != this || to.Parent != this)
                throw new InvalidOperationException();

            if (from == to)
                return;

            if ((from.Neighbors.Contains(from) && to.Neighbors.Contains(to))
                || (from.Neighbors.Contains(to) && to.Neighbors.Contains(from)))
                return;

                from.Neighbors.Add(to);
            to.Neighbors.Add(from);
        }

        public Graph<C> Convert<C>()
        {
            var result = new Graph<C>();
            var resultNodes = new Graph<C>.Node[Nodes.Count()];
            for (int i = 0; i < Nodes.Count(); i++)
            {
                var source = Nodes.ElementAt(i);
                resultNodes[i] = new Graph<C>.Node(source.Id, result);
            }

            //Convert edges info
            for (int i = 0; i < resultNodes.Length; i++)
                foreach (var sourceNeighbor in Nodes.ElementAt(i).Neighbors)
                    resultNodes[i].Neighbors.Add(resultNodes[sourceNeighbor.Id]);

            return result;
        }

        public class Node : BaseNode
        {
            public Node(int id, Graph<TData> parentGraph) : base(parentGraph, id)
            {
            }
        }
    }
}
