using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrainDemo.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace TerrainDemo.Assets.Scripts.Voronoi
{
    public class Mesh2<T>
    {
        public Cell2[] All;

        public Cell2 GetSomeInfo(Cell2 center)
        {
            return All.FirstOrDefault();
        }

        public class Cell2
        {
            public Cell2[] Neighbors;
            public T Data;
        }
    }

    public class ZoneMesh : Mesh2<ZoneLayout>
    {
        public ZoneMesh()
        {
            var n = new Cell2();
            var b = GetSomeInfo(n);
        }
    }

    public class Another
    {
        public Another()
        {
            var a = new ZoneMesh();
            var n = new ZoneMesh.Cell2();
            var b = a.GetSomeInfo(n);

            foreach (var neighbor in b.Neighbors)
            {
                Debug.Log(neighbor.Data);
            }
        }
    }
}
