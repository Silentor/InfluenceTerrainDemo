using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Code
{
    /// <summary>
    /// Simple mesher to draw zone influences by color (block type is ignored)
    /// </summary>
    public class InfluenceMesher
    {
        private readonly Color[] _zoneInfluenceColors;

        public InfluenceMesher(IEnumerable<IZoneSettings> zones)
        {
            _zoneInfluenceColors = zones.Select(z => z.LandColor).ToArray();
        }

        public Mesh Generate(Chunk chunk)
        {
            var mesh = new Mesh();

            var verts = new Vector3[chunk.GridSize * chunk.GridSize];
            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                    verts[z + x*chunk.GridSize] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);

            mesh.vertices = verts;

            var indx = new int[(chunk.GridSize - 1)*(chunk.GridSize - 1)*4];
            for (int x = 0; x < chunk.GridSize - 1; x++)
                for (int z = 0; z < chunk.GridSize - 1; z++)
                {
                    var index = (z + x*(chunk.GridSize - 1))*4;
                    indx[index + 0] = z + x*chunk.GridSize;
                    indx[index + 1] = z + 1 + x*chunk.GridSize;
                    indx[index + 2] = z + 1 + (x + 1)*chunk.GridSize;
                    indx[index + 3] = z + (x + 1)*chunk.GridSize;
                }

            mesh.SetIndices(indx, MeshTopology.Quads, 0);

            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Lerp(_zoneInfluenceColors, chunk.Influence[i / chunk.GridSize, i % chunk.GridSize]);
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

        private static Color Lerp(Color[] zonesColor, ZoneRatio ratio)
        {
            Color result = Color.black;
            for (int i = 0; i < zonesColor.Length; i++)
                result += zonesColor[i]*ratio[i];

            return result;
        }
    }
}
