using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Tools;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;

namespace TerrainDemo.Tri
{
    /// <summary>
    /// Produce Unity gameobject for some terrain part
    /// </summary>
    public class TriRenderer
    {
        public TriRenderer(TriRunner settings, TriMesher triMesher)
        {
            _triMesher = triMesher;
            _vertexColor = settings.VertexColoredMat;
        }

        public void Render(MacroMap map, MacroCellInfluenceMode influence, MacroCellReliefMode relief)
        {
            var meshGO = new GameObject("MacroMap");
            var filter = meshGO.AddComponent<MeshFilter>();
            var renderer = meshGO.AddComponent<MeshRenderer>();
            meshGO.transform.SetParent(GetMeshRoot());

            var mesh = _triMesher.CreateMesh(map, influence, relief);
            filter.mesh = mesh;
            renderer.sharedMaterial = _vertexColor;
        }

        public void Render(MicroMap map, MicroInfluenceRenderMode influenceMode)
        {
            //Break map to visualization chunks (rounded to ChunkSize)
            const int ChunkSize = 64;
            var microBounds = map.Bounds;
            var chunkMin = new Vector2i(FloorTo(microBounds.Min.X, ChunkSize), FloorTo(microBounds.Min.Z, ChunkSize));
            var chunkMax = new Vector2i(CeilTo(microBounds.Max.X, ChunkSize), CeilTo(microBounds.Max.Z, ChunkSize));

            for (int x = chunkMin.X; x < chunkMax.X; x += ChunkSize)
            {
                for (int z = chunkMin.Z; z < chunkMax.Z; z += ChunkSize)
                {
                    var meshGO = new GameObject("MicroMapChunk");
                    var filter = meshGO.AddComponent<MeshFilter>();
                    var renderer = meshGO.AddComponent<MeshRenderer>();
                    meshGO.transform.SetParent(GetMeshRoot());

                    var chunkBound = new Bounds2i(new Vector2i(x, z), ChunkSize, ChunkSize );
                    var chunkMesh = _triMesher.CreateMesh(map, chunkBound, influenceMode);

                    filter.mesh = chunkMesh;
                    renderer.sharedMaterial = _vertexColor;

                }
            }
        }

        private readonly TriMesher _triMesher;

        private readonly List<Tuple<Cell, GameObject>> _meshFilters = new List<Tuple<Cell, GameObject>>();
        private Transform _meshRoot;
        private readonly Material _vertexColor;

        private Transform GetMeshRoot()
        {
            if (_meshRoot == null)
                _meshRoot = new GameObject("TerrainMesh").transform;

            return _meshRoot;
        }

        private int FloorTo(int value, int floorStep)
        {
            return ((int)Math.Floor((double) value / floorStep)) * floorStep;
        }

        private int CeilTo(int value, int floorStep)
        {
            return ((int)Math.Ceiling((double)value / floorStep)) * floorStep;
        }

        public enum MicroInfluenceRenderMode
        {
            Hard,
            Dither_80,
            Smooth
        }

        public enum MacroCellInfluenceMode
        {
            Strict,
            Interpolated
        }

        public enum MacroCellReliefMode
        {
            Flat,
            Rude
        }
    }
}
