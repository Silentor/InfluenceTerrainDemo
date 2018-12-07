using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tri;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Object = UnityEngine.Object;

namespace TerrainDemo.Visualization
{
    /// <summary>
    /// Produce Unity gameobject for some terrain part
    /// </summary>
    public class Renderer
    {
        public Renderer(TriRunner settings, Mesher mesher)
        {
            _mesher = mesher;
            _vertexColor = settings.VertexColoredMat;
            _textured = settings.TexturedMat;
        }

        public void Render(MacroMap map, MacroCellInfluenceMode influence)
        {
            var meshGO = new GameObject("MacroMap");
            var filter = meshGO.AddComponent<MeshFilter>();
            var renderer = meshGO.AddComponent<MeshRenderer>();
            meshGO.transform.SetParent(GetMeshRoot());

            var mesh = _mesher.CreateMesh(map, influence);
            filter.mesh = mesh;
            renderer.sharedMaterial = _vertexColor;
        }

        public void Render(MicroMap map, MicroRenderMode mode)
        {
            //Break map to visualization chunks (rounded to ChunkSize)
            const ushort ChunkSize = 64;
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
                    var chunkMesh = _mesher.CreateMesh(map, chunkBound, mode);

                    filter.mesh = chunkMesh.Item1;
                    var texturedMat = new Material(_textured);
                    texturedMat.mainTexture = chunkMesh.Item2;
                    renderer.material = texturedMat;
                }
            }
        }

        public void Clear()
        {
            while (GetMeshRoot().childCount > 0)
            {
                var child = GetMeshRoot().GetChild(0).gameObject;
                child.transform.parent = null;

                var filter = child.GetComponent<MeshFilter>();
                Object.Destroy(filter.sharedMesh);
                filter.sharedMesh = null;

                var renderer = child.GetComponent<MeshRenderer>();
                if (renderer.sharedMaterial.mainTexture != null)
                {
                    Object.Destroy(renderer.sharedMaterial.mainTexture);
                    renderer.sharedMaterial.mainTexture = null;
                }


                Object.Destroy(child);
            }
        }

        private readonly Mesher _mesher;

        private readonly List<Tuple<Cell, GameObject>> _meshFilters = new List<Tuple<Cell, GameObject>>();
        private Transform _meshRoot;
        private readonly Material _vertexColor;
        private Material _textured;

        private Transform GetMeshRoot()
        {
            if (_meshRoot == null)
            {
                var oldObject = GameObject.Find("TerrainMesh");
                if(oldObject != null)
                    _meshRoot = oldObject.transform;

                if(_meshRoot == null)
                    _meshRoot = new GameObject("TerrainMesh").transform;
            }

            return _meshRoot;
        }

        private int FloorTo(int value, ushort floorStep)
        {
            return ((int)Math.Floor((double) value / floorStep)) * floorStep;
        }

        private int CeilTo(int value, ushort floorStep)
        {
            return ((int)Math.Ceiling((double)value / floorStep)) * floorStep;
        }

        [Serializable]
        public struct MicroRenderMode
        {
            public BlockRenderMode BlockMode;
            public bool RenderMainLayer;
            public bool RenderUnderLayer;

            public static readonly MicroRenderMode Default = new MicroRenderMode(){BlockMode = BlockRenderMode.Blocks, RenderMainLayer = true, RenderUnderLayer = true};
        }

        public enum BlockRenderMode
        {
            InfluenceHard,
            InfluenceDither,
            InfluenceSmooth,
            Blocks
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
