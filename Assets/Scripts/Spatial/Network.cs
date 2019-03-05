using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace TerrainDemo.Spatial
{
    public abstract class Network<TParent, TNode> : IEnumerable<TNode> where TNode : Network<TParent, TNode>.BaseNode
        where TParent : Network<TParent, TNode>
    {
        /// <summary>
        /// Find node by id
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TNode this[int index] { get { return _nodes[index]; } }

        /// <summary>
        /// All nodes
        /// </summary>
        public IEnumerable<TNode> Nodes { get { return _nodes; } }

        /// <summary>
        /// Get direct neighbors, neighbors of neighbors, etc step by step
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public virtual FloodFiller FloodFill([NotNull] TNode start, Predicate<TNode> allowFill = null)
        {
            return new FloodFiller(this, start, allowFill);
        }

        /// <summary>
        /// Get direct neighbors, neighbors of neighbors, etc step by step
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public virtual FloodFiller FloodFill([NotNull] IEnumerable<TNode> start, Predicate<TNode> allowFill = null)
        {
            return new FloodFiller(this, start, allowFill);
        }

        public virtual bool Contains(TNode node)
        {
            return node.Parent == this;
        }

        protected readonly List<TNode> _nodes = new List<TNode>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TNode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        public abstract class BaseNode
        {
            public readonly int Id;
            public readonly TParent Parent;
            //public abstract IEnumerable<TNode> Neighbors { get; }
            public static readonly IdComparer IdIncComparer = new IdComparer();

            public BaseNode([NotNull] TParent parent, int id)
            {
                if (parent == null) throw new ArgumentNullException(nameof(parent));

                Parent = parent;
                Id = id;
            }

            /// <summary>
            /// Compare cells id
            /// </summary>
            public class IdComparer : IComparer<BaseNode>
            {
                public int Compare(BaseNode x, BaseNode y)
                {
                    if (x.Id < y.Id)
                        return -1;
                    else if (x.Id > y.Id)
                        return 1;
                    else return 0;
                }
            }

        }

        /// <summary>
        /// For searching cells using flood-fill algorithm
        /// </summary>
        public class FloodFiller
        {
            private readonly Network<TParent, TNode> _parent;
            private readonly Predicate<TNode> _searchFor;
            private readonly List<List<TNode>> _neighbors = new List<List<TNode>>();

            /// <summary>
            /// Create flood-fill around <see cref="start"/> cell
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="start"></param>
            public FloodFiller(Network<TParent, TNode> parent, TNode start, Predicate<TNode> searchFor = null)
            {
                Assert.IsTrue(parent.Contains(start));

                _parent = parent;
                _searchFor = searchFor;
                _neighbors.Add(new List<TNode> { start });
            }

            /// <summary>
            /// Create flood-fill around <see cref="start"/> cells
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="start"></param>
            public FloodFiller(Network<TParent, TNode> parent, IEnumerable<TNode> start, Predicate<TNode> searchFor = null)
            {
                Assert.IsTrue(start.All(parent.Contains));

                _parent = parent;
                _searchFor = searchFor;
                var startStep = new List<TNode>();
                startStep.AddRange(start);
                _neighbors.Add(startStep);
            }


            /// <summary>
            /// Get neighbors of cell(s)
            /// </summary>
            /// <param name="step">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
            /// <returns></returns>
            public IEnumerable<TNode> GetNeighbors(int step)
            {
                if (step == 0)
                    return _neighbors[0];

                if (step < _neighbors.Count)
                    return _neighbors[step];

                //Calculate neighbors
                if (step - 1 < _neighbors.Count)
                {
                    var processedCellsIndex = Math.Max(0, step - 2);
                    var result = GetNeighbors(_neighbors[step - 1], _neighbors[processedCellsIndex]);
                    _neighbors.Add(result);
                    return result;
                }
                else
                {
                    //Calculate previous steps (because result of step=n used for step=n+1)
                    for (int i = _neighbors.Count; i < step; i++)
                        GetNeighbors(i);
                    return GetNeighbors(step);
                }
            }

            /// <summary>
            /// Get neighbors of <see cref="prevNeighbors"/> doesnt contained in <see cref="alreadyProcessed"/>
            /// </summary>
            /// <param name="prevNeighbors"></param>
            /// <param name="alreadyProcessed"></param>
            /// <returns></returns>
            private List<TNode> GetNeighbors(List<TNode> prevNeighbors, List<TNode> alreadyProcessed)
            {
                var result = new List<TNode>();
                foreach (var neigh1 in prevNeighbors)
                {
                    BaseNode test = prevNeighbors[0]; //debug

                    //foreach (var neigh2 in neigh1.Neighbors)
                    //{
                    //    if ((_searchFor == null || _searchFor(neigh2))          //check search for condition
                    //        && !result.Contains(neigh2) && !prevNeighbors.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
                    //        result.Add(neigh2);
                    //}
                }

                //return result;
                return null;
            }
        }
    }
}
