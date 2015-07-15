﻿using System.Linq;
using UnityEngine;

namespace Assets.Code
{
    public static class Mesher
    {
        public static GeneratorSettings[] Zones;

        public static Mesh Generate(Chunk chunk)
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
            for (int i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                var worldVert = chunk.Position*chunk.Size + new Vector2(vert.x, vert.z);
                colors[i] = Lerp(Zones, Generator.GetInfluence(worldVert));
            }
            mesh.colors = colors;

            mesh.RecalculateNormals();

            return mesh;
        }

        private static Color Lerp(GeneratorSettings[] zones, float[] ratio)
        {
            Color result = Color.black;
            for (int i = 0; i < zones.Length; i++)
                result += zones[i].LandColor_*ratio[i];

            return result;
        }
    }
}