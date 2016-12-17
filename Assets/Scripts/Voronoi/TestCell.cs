using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerrainDemo.Assets.Scripts.Voronoi
{


    public class SimpleCell : TestMesh<SimpleCell>.TestCell
    {
        public static TestMesh<SimpleCell> Load()
        {
            return null;
        }
    }


    public class TestMesh<T> where T : TestMesh<T>.TestCell
    {
        public IEnumerable<T> FloodFill(T center)
        {
            yield break;
        }

        public abstract class TestCell
        {
            public IEnumerable<T> Neighbors;
        }
    }

    public class TestZone : TestMesh<TestZone>.TestCell
    {
        public void Test()
        {
            var a = Neighbors.ToArray();
        }
    }

    public class TestZoneMesh : TestMesh<TestZone>
    {
        
    }

    public class Interpolator<T> where T : TestMesh<SimpleCell>.TestCell
    {
        
    }


}
