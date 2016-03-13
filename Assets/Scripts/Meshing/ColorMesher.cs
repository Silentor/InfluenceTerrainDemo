using System.Linq;
using Assets.Code.Tools;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Simple mesher, generate block hard colors as vertex color (needs appropriate shader )
    /// </summary>
    public class ColorMesher
    {
        public ColorMesher(ILandSettings settings, MesherSettings mesherSettings)
        {
            _settings = settings;
            _blocksColors = new Color[(int)mesherSettings.Blocks.Max(z => z.Block) + 1];
            foreach (var blockColor in mesherSettings.Blocks)
                _blocksColors[(int) blockColor.Block] = blockColor.Color;
        }

        public Mesh Generate(Chunk chunk)
        {
            var mesh = new Mesh();
            var normalsCache = PrecalculateNormals(chunk);
            var verts = new Vector3[chunk.BlocksCount * chunk.BlocksCount * 4];
            var normals = new Vector3[chunk.BlocksCount * chunk.BlocksCount * 4];
            for (int z = 0; z < chunk.BlocksCount; z++)
                for (int x = 0; x < chunk.BlocksCount; x++)
                {
                    //Get neighbor chunks to form a tile
                    var vertIndex = (x + z*chunk.BlocksCount) * 4;
                    verts[vertIndex] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);
                    verts[vertIndex + 1] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z + 1], (z + 1) * chunk.BlockSize);
                    verts[vertIndex + 2] = new Vector3((x + 1) * chunk.BlockSize, chunk.HeightMap[x + 1, z], z * chunk.BlockSize);
                    verts[vertIndex + 3] = new Vector3((x + 1) * chunk.BlockSize, chunk.HeightMap[x + 1, z + 1], (z + 1) * chunk.BlockSize);
                    normals[vertIndex] = normalsCache[x, z];
                    normals[vertIndex + 1] = normalsCache[x, z + 1];
                    normals[vertIndex + 2] = normalsCache[x + 1, z];
                    normals[vertIndex + 3] = normalsCache[x + 1, z + 1];
                }

            mesh.vertices = verts;

            var indx = new int[chunk.BlocksCount * chunk.BlocksCount *4];
            for (int z = 0; z < chunk.BlocksCount; z++)
                for (int x = 0; x < chunk.BlocksCount; x++)
                {
                    var index = (x + z*chunk.BlocksCount)*4;
                    indx[index + 0] = index;
                    indx[index + 1] = index + 1;
                    indx[index + 2] = index + 3;
                    indx[index + 3] = index + 2;
                }

            mesh.SetIndices(indx, MeshTopology.Quads, 0);

            var colors = new Color[mesh.vertexCount];
            for (int z = 0; z < chunk.BlocksCount; z++)
                for (int x = 0; x < chunk.BlocksCount; x++)
                {
                    var block = chunk.BlockType[x, z];
                    var color = _blocksColors[(int) block];
                    var index = (x + z * chunk.BlocksCount) * 4;

                    colors[index + 0] = color;
                    colors[index + 1] = color;
                    colors[index + 2] = color;
                    colors[index + 3] = color;
                }
            
            mesh.colors = colors;
            mesh.normals = normals;

            return mesh;
        }

        private readonly ILandSettings _settings;
        private readonly Color[] _blocksColors;

        private Vector3[,] PrecalculateNormals(Chunk chunk)
        {
            //Based on http://gamedev.stackexchange.com/questions/70546/problem-calculating-normals-for-heightmaps
            var result = new Vector3[chunk.GridSize, chunk.GridSize];
            for (int z = 0; z < chunk.GridSize; z++)
                for (int x = 0; x < chunk.GridSize; x++)
                {
                    float sx = chunk.HeightMap[x < chunk.GridSize - 1 ? x + 1 : x, z] - chunk.HeightMap[x > 0 ? x - 1 : x, z];
                    if (x == 0 || x == chunk.GridSize - 1)
                        sx *= 2;

                    float sy = chunk.HeightMap[x, z < chunk.GridSize - 1 ? z + 1 : z] - chunk.HeightMap[x, z > 0 ? z - 1 : z];
                    if (z == 0 || z == chunk.GridSize - 1)
                        sy *= 2;

                    result[x, z] = new Vector3(-sx, 2.0f, -sy).normalized;
                }

            return result;
        }

    }
}
