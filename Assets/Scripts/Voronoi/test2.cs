using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrainDemo.Layout;
using UnityEngine;

namespace TerrainDemo.Assets.Scripts.Voronoi
{
    public class BaseMesh3<T> where T : BaseMesh3<T>.BaseCell2
    {
        public T[] All;

        public BaseMesh3()
        {
            All = new T[10];
        }
        public void AddCell(T cell)
        {
            All[0] = cell;
        }

        public T GetSomeResult(T center)
        {
            return All.FirstOrDefault();
        }

        public class BaseCell2
        {
            public T[] Neighbors;

            public BaseCell2(T[] neighbors)
            {
                if (neighbors == null) throw new ArgumentNullException("neighbors");
                Neighbors = neighbors;
            }
        }

        public class FloodFiller
        {
            private T _start;
            private Predicate<T> _search;

            public FloodFiller(T startCell, Predicate<T> searchFor)
            {
                _start = startCell;
                _search = searchFor;
            }

            public IEnumerable<T> Find()
            {
                foreach (var neigh in _start.Neighbors)
                {
                    if (_search(neigh))
                        yield return neigh;
                }
            }
        }
    }

    public class Mesh3<T, J> : BaseMesh3<T> where T : Mesh3<T, J>.Cell2
    {
        public class Cell2 : BaseCell2
        {
            public J Data;

            public Cell2(T[] neighbors, J data) : base(neighbors)
            {
                if (data == null) throw new ArgumentNullException("data");
                Data = data;
            }
        }
    }


    public class SimpleChildMesh : BaseMesh3<SimpleChildMesh.SimpleCell>
    {
        public class SimpleCell : BaseCell2
        {
            public SimpleCell(SimpleCell[] neighbors) : base(neighbors)
            {
            }
        }
    }

    public class DataChildMesh : Mesh3<DataChildMesh.ChildCell, ZoneLayout>
    {
        public class ChildCell : Cell2
        {
            public ChildCell(ZoneLayout zone) : base(null, zone)
            {
                //Some init
            }
        }
    }

    public class GenericMesh<J> : Mesh3<GenericMesh<J>.GenericCell, J>
    {
        public class GenericCell : Cell2
        {
            public GenericCell(GenericCell[] neighbors, J data) : base(neighbors, data)
            {
            }
        }
    }

    public class Another2
    {
        public Another2()
        {
            var mesh = new DataChildMesh();
            var cell = new DataChildMesh.ChildCell(default(ZoneLayout));
            var r = mesh.GetSomeResult(cell);

            foreach (var neighbor in r.Neighbors)
            {
                Debug.Log(neighbor.Data);
            }

            var genMesh = new GenericMesh<ZoneLayout>();
            var genCell = new GenericMesh<ZoneLayout>.GenericCell(null, default(ZoneLayout));
            var filler = new GenericMesh<ZoneLayout>.FloodFiller(genCell, null);
            var result = filler.Find();

            //var 
        }
    }
}
