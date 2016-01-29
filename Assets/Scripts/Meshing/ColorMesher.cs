using System.Linq;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Simple mesher to draw zone influences by color (block type is ignored)
    /// </summary>
    public class ColorMesher
    {
        public ColorMesher(ILandSettings settings)
        {
            _settings = settings;
            _blocksColors = new Color[(int)settings.Blocks.Max(z => z.Block) + 1];
            foreach (var blockColor in settings.Blocks)
                _blocksColors[(int) blockColor.Block] = blockColor.Color;
            _zoneTypes = _settings.ZoneTypes.Select(z => z.Type).ToArray();
        }

        public Mesh Generate(Chunk chunk)
        {
            var mesh = new Mesh();

            var verts = new Vector3[chunk.BlocksCount * chunk.BlocksCount * 4];
            for (int x = 0; x < chunk.BlocksCount; x++)
                for (int z = 0; z < chunk.BlocksCount; z++)
                {
                    //Get neighbor chunks to form a tile
                    var vertIndex = (z + x*chunk.BlocksCount) * 4;
                    verts[vertIndex] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);
                    verts[vertIndex + 1] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z + 1], (z + 1) * chunk.BlockSize);
                    verts[vertIndex + 2] = new Vector3((x + 1) * chunk.BlockSize, chunk.HeightMap[x + 1, z], z * chunk.BlockSize);
                    verts[vertIndex + 3] = new Vector3((x + 1) * chunk.BlockSize, chunk.HeightMap[x + 1, z + 1], (z + 1) * chunk.BlockSize);
                }

            mesh.vertices = verts;

            var indx = new int[chunk.BlocksCount * chunk.BlocksCount *4];
            for (int x = 0; x < chunk.BlocksCount; x++)
                for (int z = 0; z < chunk.BlocksCount; z++)
                {
                    var index = (z + x*chunk.BlocksCount)*4;
                    indx[index + 0] = index;
                    indx[index + 1] = index + 1;
                    indx[index + 2] = index + 3;
                    indx[index + 3] = index + 2;
                }

            mesh.SetIndices(indx, MeshTopology.Quads, 0);

            var colors = new Color[mesh.vertexCount];
            for (int x = 0; x < chunk.BlocksCount; x++)
                for (int z = 0; z < chunk.BlocksCount; z++)
                {
                    var block = chunk.BlockType[x, z];
                    var color = _blocksColors[(int) block];
                    var index = (z + x * chunk.BlocksCount) * 4;

                    colors[index + 0] = color;
                    colors[index + 1] = color;
                    colors[index + 2] = color;
                    colors[index + 3] = color;
                }
            
            mesh.colors = colors;

            //Manual normals calculation
            //No lighting normals
            //var normals = new Vector3[mesh.vertexCount];
            //for (int i = 0; i < normals.Length; i++)
            //    normals[i] = Vector3.up;
            //mesh.normals = normals;
            //Traditional normals
            mesh.RecalculateNormals();

            return mesh;
        }

        private readonly ILandSettings _settings;
        private readonly Color[] _blocksColors;
        private readonly ZoneType[] _zoneTypes;

        private Color Lerp(ZoneRatio ratio)
        {
            var result = Color.black;
            for (var i = 0; i < _zoneTypes.Length; i++)
            {
                var zoneType = _zoneTypes[i];
                result += _blocksColors[(int)zoneType] * ratio[zoneType];
            }

            return result;
        }
    }
}
