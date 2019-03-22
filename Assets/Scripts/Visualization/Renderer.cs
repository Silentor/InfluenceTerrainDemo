using System;
using System.Collections.Generic;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Object = UnityEngine.Object;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

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
                        CreateRenderingGameObject(chunkMeshes.Base.Item1, chunkMeshes.Base.Item2, "BaseMesh", meshGO.transform);
                        CreateRenderingGameObject(chunkMeshes.Under.Item1, chunkMeshes.Under.Item2, "UnderMesh", meshGO.transform);
                        CreateRenderingGameObject(chunkMeshes.Main.Item1, chunkMeshes.Main.Item2, "MainMesh", meshGO.transform);
                    }
                    else
                    {
                        var chunkMesh = _mesher.CreateTerrainMesh(map, chunkBound, renderSettings);
                        CreateRenderingGameObject(chunkMesh.mesh, chunkMesh.texture, "MainMesh", meshGO.transform);
                    }
                }
            }

            //Render map objects
            foreach (var mapChild in map.Childs)
            {
                if (renderSettings.RenderMode == TerrainRenderMode.Blocks)
                {
                    var objectMesh = _mesher.CreateMinecraftMesh(mapChild, mapChild.Bounds, _settings);
                    var objectRoot = new GameObject(mapChild.Name);
                    objectRoot.transform.SetParent(GetMeshRoot());
                    CreateRenderingGameObject(objectMesh.Base.Item1, objectMesh.Base.Item2, "BaseMesh", objectRoot.transform);
                    CreateRenderingGameObject(objectMesh.Under.Item1, objectMesh.Under.Item2, "UnderMesh", objectRoot.transform);
                    CreateRenderingGameObject(objectMesh.Main.Item1, objectMesh.Main.Item2, "MainMesh", objectRoot.transform);
                }
                else
                {
                    var objectMesh = _mesher.CreateObjectMesh((ObjectMap)mapChild, _settings);
                    var objectRoot = new GameObject(mapChild.Name);
                    objectRoot.transform.SetParent(GetMeshRoot());
                    CreateRenderingGameObject(objectMesh.mesh, objectMesh.texture, "MainMesh", objectRoot.transform);
                }
            }

            //Render actors
            foreach (var actor in map.Actors)
            {
                var actorObject = _mesher.CreateActorObject(actor, renderSettings);
                actorObject.transform.SetParent(GetMeshRoot(), true);
                _renderActorCache[actor] = actorObject;
                actor.Changed += ActorOnChanged;
            }

            UpdateObserver();
        }

        public void AssingCamera(Camera camera, Actor actor)
        {
            _observer = camera;
            _observed = actor;
        }

        public void Clear()
        {
            while (GetMeshRoot().childCount > 0)
            {
                var child = GetMeshRoot().GetChild(0).gameObject;
                child.transform.parent = null;

                //Because Actors if prefab instantiated.
                //Need script for each render object with overloaded Clear :/
                if (child.GetComponent<ActorView>() == null)
                {
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
                }

                Object.Destroy(child);
            }

            _renderActorCache.Clear();
        }

        private readonly Mesher _mesher;
        private readonly TriRunner _settings;

        private readonly List<Tuple<Cell, GameObject>> _meshFilters = new List<Tuple<Cell, GameObject>>();
        private Transform _meshRoot;
        private readonly Material _vertexColor;
        private readonly Material _textured;

        private readonly Dictionary<Vector2i, GameObject> _renderChunksCache = new Dictionary<Vector2i, GameObject>();
        private readonly Dictionary<ObjectMap, GameObject> _renderObjectCache = new Dictionary<ObjectMap, GameObject>();
        private readonly Dictionary<Actor, GameObject> _renderActorCache = new Dictionary<Actor, GameObject>();
        private Camera _observer;
        private Actor _observed;

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

        private void CreateRenderingGameObject(Mesh mesh, Texture texture, string name, Transform parent)
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

        private void ActorOnChanged(Actor actor)
        {
            var actorObject = _renderActorCache[actor];
            actorObject.transform.SetPositionAndRotation(actor.Position, actor.Rotation);
            
            if(actor == _observed)
                UpdateObserver();
        }

        private void UpdateObserver()
        {
            if (_observer && _observed != null)
            {
                DebugExtension.DebugArrow(_observed.Position + Vector3.UnitY, _observed.Forward * 2, Color.green);

                //Place camera behind and above actor
                var cameraPosition = -5 * _observed.Forward + 3 * Vector3.UnitY + _observed.Position;

                //Look at ground in front of actor
                var cameraLookAt = _observed.Forward * 3 + _observed.Position;

                _observer.transform.position = cameraPosition;
                _observer.transform.LookAt(cameraLookAt);
            }
        }

        /*
        private void ClearRenderingGameObject(GameObject renderingGO)
        {
            renderingGO.transform.parent = null;

            var filters = renderingGO.GetComponentsInChildren<MeshFilter>();
            foreach (var filter in filters)
            {
                Object.Destroy(filter.sharedMesh);
                filter.sharedMesh = null;
            }

            var renderers = renderingGO.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in renderers)
            {
                if (meshRenderer.sharedMaterial.mainTexture != null)
                {
                    Object.Destroy(meshRenderer.sharedMaterial.mainTexture);
                    meshRenderer.sharedMaterial.mainTexture = null;
                }
            }

            Object.Destroy(renderingGO);
        }
        */

        /*
        /// <summary>
        /// Get render holder object for main map
        /// </summary>
        /// <param name="chunkPosition"></param>
        /// <param name="getClearObject"></param>
        /// <returns></returns>
        private GameObject GetRenderHolder(Vector2i chunkPosition, bool getClearObject)
        {
            if (_renderChunksCache.TryGetValue(chunkPosition, out var result))
            {
                if (getClearObject)
                {
                    //Clear old data
                    while (result.transform.childCount > 0)
                    {
                        var child = result.transform.GetChild(0).gameObject;
                        ClearRenderingGameObject(child);
                    }
                }
            }
            else
            {
                //Create new render chunk object
                result = new GameObject(chunkPosition.ToString());
                result.transform.parent = GetMeshRoot();
                _renderChunksCache[chunkPosition] = result;
            }

            return result;
        }

        /// <summary>
        /// Get render holder object for object
        /// </summary>
        private GameObject GetRenderHolder(ObjectMap obj, bool getClearObject)
        {
            if (_renderObjectCache.TryGetValue(obj, out var result))
            {
                if (getClearObject)
                {
                    //Clear old data
                    while (result.transform.childCount > 0)
                    {
                        var child = result.transform.GetChild(0).gameObject;
                        ClearRenderingGameObject(child);
                    }
                }
            }
            else
            {
                //Create new render chunk object
                result = new GameObject(obj.Name);
                result.transform.parent = GetMeshRoot();
                _renderObjectCache[obj] = result;
            }

            return result;
        }
        */

        /*
        private void RenderMicroMapRegion(MicroMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            //Break map to render chunks (rounded to ChunkSize)
            var microBounds = bounds;
            var chunkMin = new Vector2i(SnapDown(microBounds.Min.X, ChunkSize), SnapDown(microBounds.Min.Z, ChunkSize));
            var chunkMax = new Vector2i(SnapUp(microBounds.Max.X, ChunkSize), SnapUp(microBounds.Max.Z, ChunkSize));

            //Iterate and generate render chunks
            for (int x = chunkMin.X; x < chunkMax.X; x += ChunkSize)
            {
                for (int z = chunkMin.Z; z < chunkMax.Z; z += ChunkSize)
                {
                    var chunkStartPosition = new Vector2i(x, z);

                    var meshGO = GetRenderHolder(chunkStartPosition, true);
                    
                    var chunkBound = new Bounds2i(chunkStartPosition, ChunkSize, ChunkSize);
                    if (renderSettings.RenderMode == TerrainRenderMode.Blocks)
                    {
                        var chunkMeshes = _mesher.CreateMinecraftMesh(map, chunkBound, renderSettings);
                        CreateRenderingGameObject(chunkMeshes.Base.Item1, chunkMeshes.Base.Item2, "BaseMesh", meshGO.transform);
                        CreateRenderingGameObject(chunkMeshes.Under.Item1, chunkMeshes.Under.Item2, "UnderMesh", meshGO.transform);
                        CreateRenderingGameObject(chunkMeshes.Main.Item1, chunkMeshes.Main.Item2, "MainMesh", meshGO.transform);
                    }
                    else
                    {
                        var chunkMesh = _mesher.CreateTerrainMesh(map, chunkBound, renderSettings);
                        CreateRenderingGameObject(chunkMesh.mesh, chunkMesh.texture, "MainMesh", meshGO.transform);
                    }
                }
            }
        }
*/

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
