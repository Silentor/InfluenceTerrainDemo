using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Simple mesher, generate zone influences by vertex color (needs appropriate shader )
    /// </summary>
    public class InfluenceMesher : BaseMesher
    {
        public InfluenceMesher(LandSettings settings, MesherSettings meshSettings)
        {
            _settings = settings;
            _meshSettings = meshSettings;
            _zoneInfluenceColors = new Color[(int)settings.Clusters.Max(z => z.Type) + 1];
            foreach (var zoneSettingse in settings.Clusters)
                _zoneInfluenceColors[(int) zoneSettingse.Type] = zoneSettingse.LandColor;
            _zoneTypes = _settings.Clusters.Select(z => z.Type).ToArray();
        }

        public override ChunkModel Generate(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            var mesh = new Mesh();

            var verts = new Vector3[chunk.GridSize * chunk.GridSize];
            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                    verts[z + x*chunk.GridSize] = new Vector3(x * chunk.BlockSize, _meshSettings.BypassHeightMap ? 0 : chunk.HeightMap[x, z], z * chunk.BlockSize);

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
            
            mesh.RecalculateNormals();

            return new ChunkModel() { Material = _meshSettings.VertexColoredMaterial, Mesh = mesh };
        }

        private readonly LandSettings _settings;
        private readonly MesherSettings _meshSettings;
        private readonly Color[] _zoneInfluenceColors;
        private readonly ClusterType[] _zoneTypes;

        private Color Lerp(ZoneRatio ratio)
        {
            var result = Color.black;

            if(!ratio.IsEmpty)
                for (var i = 0; i < _zoneTypes.Length; i++)         //todo iterate for ratio
                {
                    var zoneType = _zoneTypes[i];
                    result += _zoneInfluenceColors[(int)zoneType] * (float)ratio[zoneType];
                }

            return result;
        }
    }
}
