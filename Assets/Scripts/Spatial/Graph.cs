using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Supports unique edges only
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public class Graph<TNode, TEdge>
    {
	    public IEnumerable<TNode> Nodes => _nodes.Keys;
		public IEnumerable<(TNode from, TEdge edge, TNode to)> Edges
		{
			get
			{
				foreach ( var edge in _edges )
				{
					yield return ( edge.Value.From.Data, edge.Key, edge.Value.To.Data );
				}
			}
		}
        public int NodesCount => _nodes.Count;
        public int EdgesCount => _edges.Count;

        public Graph(  )
        {
        }

        public Node AddNode(TNode data)
        {
            var newNode = new Node(this);
            _nodes[data] = newNode;
            newNode.Data = data;
            return newNode;
        }

        public Edge AddEdge(TNode from, TNode to, TEdge data)
        {
	        if (from.Equals( to ))
		        throw new InvalidOperationException();

	        var from2 = _nodes[from];
	        var to2 = _nodes[to];
	        var newEdge = new Edge( this, from2, to2 );
	        newEdge.Data = data;
	        _edges[data] = newEdge;
	        return newEdge;
        }

        public Edge AddEdge(TNode from, TNode to, Func<Edge, TEdge> dataGenerator)
        {
	        if (from.Equals( to ))
		        throw new InvalidOperationException();

	        var from2   = _nodes[from];
	        var to2     = _nodes[to];
	        var newEdge = new Edge( this, from2, to2 );
	        newEdge.Data = dataGenerator(newEdge);
	        _edges[newEdge.Data] = newEdge;
	        return newEdge;
        }

        public Edge AddEdge(Node from, Node to, TEdge data)
        {
            if(from.Parent != this || to.Parent != this)
                throw new InvalidOperationException();

            if (from == to)
                throw new InvalidOperationException();

            var newEdge = new Edge( this, from, to );
            newEdge.Data = data;
            _edges[data] = newEdge;
            return newEdge;
        }

        private Node GetNode( TNode data )
        {
            return _nodes[data];
        }

        public IEnumerable<(TEdge edge, TNode neighbor)> GetNeighbors( TNode @from )
        {
            var internalNode = GetNode( @from );
            foreach ( var internalEdge in internalNode.Edges )
            {
                yield return ( internalEdge.Data, internalEdge.To.Data );
            }
        }

        //public Graph<C> Convert<C>()
        //{
        //    var result = new Graph<C>();
        //    var resultNodes = new Graph<C>.Node[Nodes.Count()];
        //    for (int i = 0; i < Nodes.Count(); i++)
        //    {
        //        var source = Nodes.ElementAt(i);
        //        resultNodes[i] = new Graph<C>.Node(source.Id, result);
        //    }

        //    //Convert edges info
        //    for (int i = 0; i < resultNodes.Length; i++)
        //        //foreach (var sourceNeighbor in Nodes.ElementAt(i).Neighbors)
        //            //resultNodes[i].Neighbors.Add(resultNodes[sourceNeighbor.Id]);
        //            ;

        //    return result;
        //}

        private readonly Dictionary<TNode, Node> _nodes = new Dictionary<TNode, Node>();
        private readonly Dictionary<TEdge, Edge> _edges = new Dictionary<TEdge, Edge>();

        public class Node
        {
            public TNode Data;
            public readonly Graph<TNode, TEdge> Parent;
			public readonly List<Edge> Edges = new List<Edge>();

            internal Node(Graph<TNode, TEdge> parentGraph)
            {
	            Parent = parentGraph;
            }
        }

        public class Edge
        {
	        public TEdge        Data;
	        public readonly Node        From;
	        public readonly Node        To;
	        public readonly Graph<TNode, TEdge> Parent;

	        internal Edge( Graph<TNode, TEdge> parent, Node from, Node to )
	        {
		        Parent = parent;
		        From = from;
		        To = to;
				from.Edges.Add( this );
	        }
        }
    }
}
