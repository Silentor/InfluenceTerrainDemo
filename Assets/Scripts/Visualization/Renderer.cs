using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
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

        public Renderer(Mesher mesher, TriRunner settings)
        {
            _mesher = mesher;
            _settings = settings;
            _vertexColor = settings.VertexColoredMat;
            _textured = settings.TexturedMat;
        }

        public void Render(MacroMap map, TriRunner renderSettings)
        {
            var mesh = _mesher.CreateMacroMesh(map, renderSettings.MacroCellInfluenceVisualization);

            var meshGO = new GameObject("MacroMap");
            var filter = meshGO.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = meshGO.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _vertexColor;
            meshGO.transform.SetParent(GetMeshRoot());
        }

        public void Render(MicroMap map, TriRunner renderSettings)
        {
            Assert.IsTrue(renderSettings.RenderMode != TerrainRenderMode.Macro);

            //Break map to render chunks (rounded to ChunkSize)
            var microBounds = map.Bounds;
            var chunkMin = new Vector2i(SnapDown(microBounds.Min.X, ChunkSize), SnapDown(microBounds.Min.Z, ChunkSize));
            var chunkMax = new Vector2i(SnapUp(microBounds.Max.X, ChunkSize), SnapUp(microBounds.Max.Z, ChunkSize));

            //Iterate and generate render chunks
            for (int x = chunkMin.X; x < chunkMax.X; x += ChunkSize)
            {
                for (int z = chunkMin.Z; z < chunkMax.Z; z += ChunkSize)
                {
                    var meshGO = new GameObject("MicroMapChunk");
                    meshGO.transform.SetParent(GetMeshRoot());

                    var chunkBound = new Bounds2i(new Vector2i(x, z), ChunkSize, ChunkSize);
                    if (renderSettings.RenderMode == TerrainRenderMode.Blocks)
                    {
                        var chunkMeshes = _mesher.CreateMinecraftMesh(map, chunkBound, renderSettings);
                        CreateGameObject(chunkMeshes.Base.Item1, chunkMeshes.Base.Item2, "BaseMesh", meshGO.transform);
                        CreateGameObject(chunkMeshes.Under.Item1, chunkMeshes.Under.Item2, "UnderMesh", meshGO.transform);
                        CreateGameObject(chunkMeshes.Main.Item1, chunkMeshes.Main.Item2, "MainMesh", meshGO.transform);
                    }
                    else
                    {
                        var chunkMesh = _mesher.CreateTerrainMesh(map, chunkBound, renderSettings);
                        CreateGameObject(chunkMesh.mesh, chunkMesh.texture, "MainMesh", meshGO.transform);
                    }
                }
            }

            //Render map objects
            foreach (var mapChild in map.Childs)
            {
                if (renderSettings.RenderMode == TerrainRenderMode.Blocks)
                {
                    var objectMesh = _mesher.CreateMinecraftMesh(mapChild, mapChild.Bounds, _settings);
                    var objectRoot = new GameObject("Object");
                    objectRoot.transform.SetParent(GetMeshRoot());
                    CreateGameObject(objectMesh.Base.Item1, objectMesh.Base.Item2, "BaseMesh", objectRoot.transform);
                    CreateGameObject(objectMesh.Under.Item1, objectMesh.Under.Item2, "UnderMesh", objectRoot.transform);
                    CreateGameObject(objectMesh.Main.Item1, objectMesh.Main.Item2, "MainMesh", objectRoot.transform);
                }
                else
                {
                    var objectMesh = _mesher.CreateObjectMesh(mapChild, _settings);
                    var objectRoot = new GameObject("Object");
                    objectRoot.transform.SetParent(GetMeshRoot());
                    CreateGameObject(objectMesh.mesh, objectMesh.texture, "MainMesh", objectRoot.transform);
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
        private readonly TriRunner _settings;

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

        private void CreateGameObject(Mesh mesh, Texture texture, string name, Transform parent)
        {
            var go = new GameObject(name);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            go.transform.SetParent(parent);

            filter.mesh = mesh;
            var baseTexturedMat = new Material(_textured);
            baseTexturedMat.mainTexture = texture;
            renderer.material = baseTexturedMat;
        }

        public enum BlockTextureMode
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

        public enum TerrainRenderMode
        {
            Terrain,
            Blocks,
            Macro,
        }

        public enum TerrainLayerToRender
        {
            Base,
            Underground,
            Main,
        }
    }
}
