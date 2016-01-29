using System.Linq;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Simple mesher to draw zone influences by color (block type is ignored)
    /// </summary>
    public class InfluenceMesher
    {
        public InfluenceMesher(ILandSettings settings)
        {
            _settings = settings;
            _zoneInfluenceColors = new Color[(int)settings.ZoneTypes.Max(z => z.Type) + 1];
            foreach (var zoneSettingse in settings.ZoneTypes)
                _zoneInfluenceColors[(int) zoneSettingse.Type] = zoneSettingse.LandColor;
            _zoneTypes = _settings.ZoneTypes.Select(z => z.Type).ToArray();
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
                var influence = chunk.Influence[i/chunk.GridSize, i%chunk.GridSize];
                if (_settings.InfluenceLimit == 0)
                    influence = influence.Pack(_settings.InfluenceThreshold);
                else
                    influence = influence.Pack(_settings.InfluenceLimit);
                colors[i] = Lerp(influence);
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
        private readonly Color[] _zoneInfluenceColors;
        private readonly ZoneType[] _zoneTypes;

        private Color Lerp(ZoneRatio ratio)
        {
            var result = Color.black;
            for (var i = 0; i < _zoneTypes.Length; i++)
            {
                var zoneType = _zoneTypes[i];
                result += _zoneInfluenceColors[(int)zoneType] * ratio[zoneType];
            }

            return result;
        }
    }
}
