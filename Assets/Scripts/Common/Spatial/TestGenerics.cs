using System.Collections.Generic;
using System.Linq;

//Experiments with different generic approaches for Network->Graph->Mesh hierarchy
namespace TerrainDemo.Spatial
{
    public class Mesh<TNodeData>
    {
        public IEnumerable<Cell<TNodeData>> Cells;

        public  void Add(Cell<TNodeData> cell) { }
    }

    public class Cell<TData>
    {
        public TData Data;

        public Mesh<TData> Parent;

        //public Mesh2<Cell<TData>, TData> Parent2;
    }

    public class Mesh2<TNode, TNodeData> where TNode : Cell2<TNodeData>
    {
        public IEnumerable<TNode> Cells;

        public void Add(TNode cell) { }
    }

    public class Cell2<TData> 
    {
        public TData Data;
        public Mesh2<Cell2<TData>, TData> Parent3;
    }

    public class Mesh3<TParent, TNode, TNodeData> where TNode : Mesh3<TParent, TNode, TNodeData>.Cell3
                                                  where TParent : Mesh3<TParent, TNode, TNodeData>
    {
        public IEnumerable<TNode> Cells;

        public void Add(TNode cell) { }

        public class Cell3
        {
            public TNodeData Data;
            public TParent Parent;
        }
    }

    public class Mesh3Desc<TParent, TNode, TNodeData> : Mesh3<TParent, TNode, TNodeData> where TNode : Mesh3Desc<TParent, TNode, TNodeData>.Cell3Desc
                                                                                where TParent : Mesh3Desc<TParent, TNode, TNodeData>
    {
        public class Cell3Desc : Cell3
        {
            public TNodeData Data2;
        }
    }

    public class Mesh3Desc2<TParent, TNode, TNodeData> : Mesh3Desc<TParent, TNode, TNodeData> where TNode : Mesh3Desc2<TParent, TNode, TNodeData>.Cell3Desc2
                                                        where TParent : Mesh3Desc2<TParent, TNode, TNodeData>
    {
        public class Cell3Desc2 : Cell3Desc
        {
            public TNodeData Data3;
        }
    }

    public class IntMesh3 : Mesh3Desc2<IntMesh3, IntMesh3.IntCell3, int>
    {
        public class IntCell3 : Cell3Desc2
        {
            public int Data4;
        }
    }

    //public class IntCell : Cell<int> { }

    //public class IntCell2 : Cell2<int, int> { }

    //public class IntMesh2 : Mesh2<Cell2<int, Mesh2<Cell2<, >>>, > { }

    public class TestRef
    {
        public List<TestRef> Parent;

        public TestRef()
        {
            var a = new List<TestRef>();
        }
    }

    public class Main
    {
        public Main()
        {
            var mesh = new Mesh<int>();
            var mesh2 = new Mesh2<Cell2<int>, int>();
            var mesh3 = new IntMesh3();

            var cell = new Cell<int>();
            var cell2 = new Cell2<int>();
            var intCell3 = new IntMesh3.IntCell3();
            //var intCell = new IntCell();

            mesh.Add(cell);
            mesh2.Add(cell2);
            mesh3.Add(intCell3);

            var test = mesh3.Cells.First().Data;
            test = mesh3.Cells.First().Data2;
            test = mesh3.Cells.First().Data3;
            test = mesh3.Cells.First().Data4;

            cell.Parent = mesh;
            cell2.Parent3 = mesh2;
            intCell3.Parent = mesh3;
            //intCell.Parent2 = mesh2;

        }
    }
}
