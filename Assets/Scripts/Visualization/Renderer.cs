using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
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
        const ushort ChunkSize = 64;

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
            var microBounds = map.Bounds;
            var chunkMin = new Vector2i(SnapDown(microBounds.Min.X, ChunkSize), SnapDown(microBounds.Min.Z, ChunkSize)); 
            var chunkMax = new Vector2i(SnapUp(microBounds.Max.X, ChunkSize), SnapUp(microBounds.Max.Z, ChunkSize));

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

        public void Render2(MicroMap map, MicroRenderMode mode)
        {
            //Break map to visualization chunks (rounded to ChunkSize)
            var microBounds = map.Bounds;
            var chunkMin = new Vector2i(SnapDown(microBounds.Min.X, ChunkSize), SnapDown(microBounds.Min.Z, ChunkSize));
            var chunkMax = new Vector2i(SnapUp(microBounds.Max.X, ChunkSize), SnapUp(microBounds.Max.Z, ChunkSize));

            for (int x = chunkMin.X; x < chunkMax.X; x += ChunkSize)
            {
                for (int z = chunkMin.Z; z < chunkMax.Z; z += ChunkSize)
                {
                    var meshGO = new GameObject("MicroMapChunk");
                    meshGO.transform.SetParent(GetMeshRoot());

                    var chunkBound = new Bounds2i(new Vector2i(x, z), ChunkSize, ChunkSize);
                    var chunkMeshes = _mesher.CreateMesh2Minecraft(map, chunkBound, mode);

                    //Combined mesh mode
                    if (chunkMeshes.Base.Item1 == null)
                    {
                        var combinedMeshGO = new GameObject("CombinedMesh");
                        var combinedFilter = combinedMeshGO.AddComponent<MeshFilter>();
                        var combinedRenderer = combinedMeshGO.AddComponent<MeshRenderer>();
                        combinedMeshGO.transform.SetParent(meshGO.transform);

                        combinedFilter.mesh = chunkMeshes.Main.Item1;
                        var combinedTexturedMat = new Material(_textured);
                        combinedTexturedMat.mainTexture = chunkMeshes.Main.Item2;
                        combinedRenderer.material = combinedTexturedMat;

                    }
                    else
                    {
                        var baseMeshGO = new GameObject("BaseMesh");
                        var baseFilter = baseMeshGO.AddComponent<MeshFilter>();
                        var baseRenderer = baseMeshGO.AddComponent<MeshRenderer>();
                        baseMeshGO.transform.SetParent(meshGO.transform);

                        var underMeshGO = new GameObject("UnderMesh");
                        var underFilter = underMeshGO.AddComponent<MeshFilter>();
                        var underRenderer = underMeshGO.AddComponent<MeshRenderer>();
                        underMeshGO.transform.SetParent(meshGO.transform);

                        var mainMeshGO = new GameObject("MainMesh");
                        var mainFilter = mainMeshGO.AddComponent<MeshFilter>();
                        var mainRenderer = mainMeshGO.AddComponent<MeshRenderer>();
                        mainMeshGO.transform.SetParent(meshGO.transform);

                        baseFilter.mesh = chunkMeshes.Base.Item1;
                        var baseTexturedMat = new Material(_textured);
                        baseTexturedMat.mainTexture = chunkMeshes.Base.Item2;
                        baseRenderer.material = baseTexturedMat;

                        underFilter.mesh = chunkMeshes.Under.Item1;
                        var underTexturedMat = new Material(_textured);
                        underTexturedMat.mainTexture = chunkMeshes.Under.Item2;
                        underRenderer.material = underTexturedMat;

                        mainFilter.mesh = chunkMeshes.Main.Item1;
                        var mainTexturedMat = new Material(_textured);
                        mainTexturedMat.mainTexture = chunkMeshes.Main.Item2;
                        mainRenderer.material = mainTexturedMat;
                    }
                }
            }
        }

        public void Clear()
        {
            while (GetMeshRoot().childCount > 0)
            {
                var child = GetMeshRoot().GetChild(0).gameObject;
                child.transform.parent = null;

                var filters = child.GetComponentsInChildren<MeshFilter>();
                foreach (var filter in filters)
                {
                    Object.Destroy(filter.sharedMesh);
                    filter.sharedMesh = null;
                }

                var renderers = child.GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in renderers)
                {
                    if (meshRenderer.sharedMaterial.mainTexture != null)
                    {
                        Object.Destroy(meshRenderer.sharedMaterial.mainTexture);
                        meshRenderer.sharedMaterial.mainTexture = null;
                    }
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

        private int SnapDown(int value, ushort floorStep)
        {
            return ((int)Math.Floor((double) value / floorStep)) * floorStep;
        }

        private int SnapUp(int value, ushort floorStep)
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
