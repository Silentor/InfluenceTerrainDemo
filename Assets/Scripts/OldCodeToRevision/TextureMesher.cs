using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.OldCodeToRevision
{
    /// <summary>
    /// Generate mesh with unique texture from chunk data
    /// </summary>
    public class TextureMesher //: BaseMesher
    {
        public AverageTimer MeshTimer { get { return _meshTimer;} }

        public AverageTimer TextureTimer { get { return _textureTimer; } }

        /*
        public TextureMesher(LandSettings settings, MesherSettings meshSettings)
        {
            _meshSettings = meshSettings;

            foreach (var block in meshSettings.Blocks)
            {
                _blockSettings.Add(block.Block, block);
            }
        }
        */

        ~TextureMesher()
        {
            //Dispose();            Needs to be call from MT
        }

        public ChunkModel Generate(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            _meshTimer.Start();

            var mesh = new Mesh();

            //Build vertices height map
            var verts = new Vector3[chunk.GridSize * chunk.GridSize];
            for (int z = 0; z < chunk.GridSize; z++)
                for (int x = 0; x < chunk.GridSize; x++)
                    verts[x + z * chunk.GridSize] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);
            mesh.vertices = verts;

            //Calcualate indices
            var indx = new int[(chunk.GridSize - 1) * (chunk.GridSize - 1) * 4];
            for (int z = 0; z < chunk.GridSize - 1; z++)
                for (int x = 0; x < chunk.GridSize - 1; x++)
                {
                    var index = (x + z * (chunk.GridSize - 1)) * 4;
                    indx[index + 0] = x + z * chunk.GridSize;
                    indx[index + 1] = x + (z + 1) * chunk.GridSize; 
                    indx[index + 2] = x + 1 + (z + 1) * chunk.GridSize;
                    indx[index + 3] = x + 1 + z * chunk.GridSize;
                }
            mesh.SetIndices(indx, MeshTopology.Quads, 0);

            //Calculate UV
            var uv = new Vector2[mesh.vertexCount];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(verts[i].x/(chunk.GridSize - 1), verts[i].z/(chunk.GridSize - 1));
            mesh.uv = uv;

            //Calculate normals
            //mesh.RecalculateNormals();
            mesh.normals = CalculateNormals(chunk, map);

            mesh.RecalculateBounds();

            _meshTimer.Stop();

            //Generate texture
            _textureTimer.Start();
            //var tex = GenerateTextureCPU(chunk);
            var tex = GenerateTextureShader(chunk, map);
            _textureTimer.Stop();

            var material = new Material(string.Empty/*_meshSettings.TexturedMaterial*/);
            material.mainTexture = tex;
            //material.SetTexture("_BumpMap", tex.Normal);

            return new ChunkModel {Mesh = mesh, Material = material };
        }

        public void Dispose()
        {
            foreach (var allocatedTexture in _allocatedTextures)
            {
                allocatedTexture.Release();
                _cachedTextures.Add(allocatedTexture);
            }

            _allocatedTextures.Clear();
        }

        public void DebugLogStatistic()
        {
            Debug.LogFormat("Mesh generation timings: mesh {0} ms, texture {1} ms, {2} ops", MeshTimer.AvgTimeMs, TextureTimer.AvgTimeMs, TextureTimer.SamplesCount);
        }

        //private readonly MesherSettings _meshSettings;

        private readonly AverageTimer _meshTimer = new AverageTimer();
        private readonly AverageTimer _textureTimer = new AverageTimer();
        
        private readonly Dictionary<BlockType, BlockRenderSettings> _blockSettings = new Dictionary<BlockType, BlockRenderSettings>();
        private readonly Dictionary<BlockType, Texture> _blockResult = new Dictionary<BlockType, Texture>();

        private readonly List<RenderTexture> _cachedTextures = new List<RenderTexture>();
        private readonly List<RenderTexture> _allocatedTextures = new List<RenderTexture>();

        //private Texture2D GenerateTextureCPU(Chunk chunk)
        //{
        //    var result = new Texture2D(1024, 1024, TextureFormat.RGB24, true);
        //    var pixels = result.GetPixels();

        //    for (int z = 0; z < 1024 - 1; z++)
        //        for (int x = 0; x < 1024 - 1; x++)
        //        {
        //            var blockType = chunk.BlockType[x/64, z/64];
        //            if (blockType == BlockType.Rock)
        //                pixels[x + z*1024] = _stone[x + z * 1024];
        //            else
        //                pixels[x + z*1024] = _grass[x + z * 1024];
        //        }

        //    result.SetPixels(pixels);
        //    result.Apply();

        //    return result;
        //}

        private RenderTexture GetRenderTexture()
        {
            if (_cachedTextures.Count == 0)
            {
                //Cant reuse texture if drawing many textures for one frame
                var renderTexture = new RenderTexture(/*_meshSettings.TextureSize, _meshSettings.TextureSize, 0*/0, 0, 0);
                renderTexture.wrapMode = TextureWrapMode.Clamp;
                renderTexture.enableRandomWrite = true;
                renderTexture.useMipMap = false;
                renderTexture.autoGenerateMips = false;
                renderTexture.Create();
                
                _cachedTextures.Add(renderTexture);
            }

            var allocated = _cachedTextures.Last();
            _cachedTextures.RemoveAt(_cachedTextures.Count - 1);
            _allocatedTextures.Add(allocated);
            return allocated;
        }

        private RenderTexture GetResultTexture()
        {
            var renderTex2 = new RenderTexture(/*_meshSettings.TextureSize, _meshSettings.TextureSize, 0*/0, 0, 0);
            renderTex2.useMipMap = true;
            renderTex2.autoGenerateMips = true;
            renderTex2.wrapMode = TextureWrapMode.Clamp;
            return renderTex2;
        }

        private Texture GetTextureFor(BlockType block, ComputeBuffer heightMap, TerrainMap normals, Vector2i chunkPos)
        {
            var settings = _blockSettings[block];
            Texture flat = settings.FlatTexture.Texture;
            Texture steep = settings.SteepTexture.Texture;

            /*
            var shader = _meshSettings.BlockShader;
            var kernelName = "Bare";

            if (!settings.FlatTexture.BypassMix)
                kernelName = "Mix";

            if (!settings.BypassTriplanar)
                kernelName += "Tri";

            if (!settings.BypassTint)
                kernelName += "Tint";

            var kernelId = shader.FindKernel(kernelName);
            shader.SetTexture(kernelId, "Texture", flat);

            if (!settings.FlatTexture.BypassMix)
            {
                shader.SetTexture(kernelId, "Noise", _meshSettings.NoiseTexture);
                //mixShader.SetTexture(kernelId, "MixTexture", settings.MixTexture);
                shader.SetFloat("MixNoiseScale", settings.FlatTexture.MixNoiseScale);
                shader.SetFloat("MixTextureScale", settings.FlatTexture.MixTextureScale);
                shader.SetFloat("MixTextureAngle", settings.FlatTexture.MixTextureAngle);
                shader.SetInts("ChunkPos", chunkPos.X, chunkPos.Z);
            }

            if (!settings.BypassTriplanar)
            {
                shader.SetTexture(kernelId, "SteepTexture", steep);
                shader.SetBuffer(kernelId, "HeightMap", heightMap);
                shader.SetTexture(kernelId, "Normals", normals.Map);
                shader.SetInt("Border", normals.Border);
                shader.SetFloat("SteepAngleFrom", settings.SteepAngles.x);
                shader.SetFloat("SteepAngleTo", settings.SteepAngles.y);
            }

            if (!settings.BypassTint)
            {
                shader.SetFloat("TintNoiseScale", settings.TintNoiseScale);
                shader.SetVector("FromColor", settings.TintFrom);
                shader.SetVector("ToColor", settings.TintTo);
            }

            var result = GetRenderTexture();
            shader.SetTexture(kernelId, "Result", result);
            //mixShader.SetTexture(kernelId, "ResultNormals", resultNrm);
            shader.Dispatch(kernelId, result.width / 8, result.height / 8, 1);

            return result;
            */

            return null;
        }

        private Texture GenerateTextureShader(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            /*
            var border = _meshSettings.MaskBorder;

            var mask = GetChunkBlocks(chunk, border, map);
            var geoMap = PrepareGeometryMap(mask, border);
            var heightMap = PrepareHeightMap(chunk);
            
            foreach (var blockType in _blockSettings.Keys)
            {
                var renderTriTex = GetTextureFor(blockType, heightMap, geoMap, chunk.Position);
                _blockResult[blockType] = renderTriTex;
            }

            heightMap.Dispose();

            var blockMask = PrepareBlockTypeMask(mask);
            var renderTex = GetRenderTexture();
            //var renderTexNrm = GetRenderTexture();
            //var renderTexNrm = GetRenderTexture();
            var shader = _meshSettings.TextureBlendShader;
            shader.SetTexture(0, "mask", blockMask);
            shader.SetInt("border", border);
            shader.SetFloat("turbulence", _meshSettings.Turbulence);
            shader.SetTexture(0, "noise", _meshSettings.NoiseTexture);
            if (_blockResult.ContainsKey(BlockType.Grass))
            {
                shader.SetTexture(0, "grass", _blockResult[BlockType.Grass]);
                //shader.SetTexture(0, "grassNrm", _blockResult[BlockType.Grass].Normal);
            }
            if (_blockResult.ContainsKey(BlockType.Rock))
            {
                shader.SetTexture(0, "stone", _blockResult[BlockType.Rock]);
                //shader.SetTexture(0, "stoneNrm", _blockResult[BlockType.Rock].Normal);
            }
            if (_blockResult.ContainsKey(BlockType.Sand))
            {
                shader.SetTexture(0, "sand", _blockResult[BlockType.Sand]);
                //shader.SetTexture(0, "sandNrm", _blockResult[BlockType.Sand].Normal);
            }
            if (_blockResult.ContainsKey(BlockType.Water))
            {
                shader.SetTexture(0, "water", _blockResult[BlockType.Water]);
                //shader.SetTexture(0, "waterNrm", _blockResult[BlockType.Water].Normal);
            }
            if (_blockResult.ContainsKey(BlockType.Snow))
            {
                shader.SetTexture(0, "snow", _blockResult[BlockType.Snow]);
                //shader.SetTexture(0, "snowNrm", _blockResult[BlockType.Snow].Normal);
            }
            shader.SetTexture(0, "result", renderTex);
            //shader.SetTexture(0, "resultNrm", renderTexNrm);
            shader.Dispatch(0, renderTex.width / 8, renderTex.height / 8, 1);

            //Render computed texture to another one to generate auto mipmaps
            //Todo use new 5.4 Graphics.CopyTexture() to copy to compressed Texture2D

            var renderTex2 = GetResultTexture();
            Graphics.Blit(renderTex, renderTex2);

            /*
            var renderTexNrm2 = new RenderTexture(renderTexNrm.width, renderTexNrm.height, 0);
            renderTexNrm2.useMipMap = true;
            renderTexNrm2.generateMips = true;
            renderTexNrm2.wrapMode = TextureWrapMode.Clamp;
            renderTexNrm2.Create();
            Graphics.Blit(renderTexNrm, renderTexNrm2);
            */

            //return renderTex2;

            return null;

        }

        private ComputeBuffer PrepareHeightMap(Chunk chunk)
        {
            var heightBuf = new ComputeBuffer(chunk.GridSize * chunk.GridSize, sizeof(float));
            var heightMap = new float[chunk.GridSize*chunk.GridSize];

            for (int z = 0; z < chunk.GridSize; z++)
                for (int x = 0; x < chunk.GridSize; x++)
                    heightMap[x + z*chunk.GridSize] = chunk.HeightMap[x, z];

            heightBuf.SetData(heightMap);
            return heightBuf;
        }

        private Texture2D PrepareBlockTypeMask(ChunkMaskBlock[,] blocks)
        {
            var result = new Color[blocks.Length];
            for (int z = 0; z <= blocks.GetUpperBound(1); z++)
                for (int x = 0; x <= blocks.GetUpperBound(0); x++)
                {
                    var blockType = blocks[x, z].Block;
                    if (blockType == BlockType.Grass)
                        result[x + z * blocks.GetLength(1)] = new Color(0, 1, 0, 0);
                    else if (blockType == BlockType.Sand)
                        result[x + z * blocks.GetLength(1)] = new Color(1, 0, 0, 0);
                    else if (blockType == BlockType.Water)
                        result[x + z * blocks.GetLength(1)] = new Color(0, 0, 1, 0);
                    else if (blockType == BlockType.Snow)
                        result[x + z * blocks.GetLength(1)] = new Color(0, 0, 0, 1);
                    //Stone - no color at all
                }

            var resultTexture = new Texture2D(blocks.GetLength(0), blocks.GetLength(1), TextureFormat.RGBA32, false);
            resultTexture.wrapMode = TextureWrapMode.Clamp;
            resultTexture.SetPixels(result);
            resultTexture.Apply(false, true);
            return resultTexture;
        }

        private TerrainMap PrepareGeometryMap(ChunkMaskBlock[,] blocks, int border)
        {
            var result = new Color[blocks.Length];
            for (int z = 0; z <= blocks.GetUpperBound(1); z++)
                for (int x = 0; x <= blocks.GetUpperBound(0); x++)
                {
                    var normal = blocks[x, z].Normal;
                    result[x + z * blocks.GetLength(1)].r = normal.x / 2 + 0.5f;
                    result[x + z * blocks.GetLength(1)].g = normal.y / 2 + 0.5f;
                    result[x + z * blocks.GetLength(1)].b = normal.z / 2 + 0.5f;
                    result[x + z * blocks.GetLength(1)].a = Vector3.Angle(normal, Vector3.up)/90f;
                }

            var resultTexture = new Texture2D(blocks.GetLength(0), blocks.GetLength(1), TextureFormat.RGBA32, false);
            resultTexture.wrapMode = TextureWrapMode.Clamp;
            resultTexture.SetPixels(result);
            resultTexture.Apply(false, true);
            return new TerrainMap() {Map = resultTexture, Border = border};
        }

        /// <summary>
        /// Get block map of Chunk with borders (borders used for texture blending)
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="border"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        private ChunkMaskBlock[,] GetChunkBlocks(Chunk chunk, int border, Dictionary<Vector2i, Chunk> map)
        {
            map.TryGetValue(chunk.Position + Vector2i.Forward, out var top);
            map.TryGetValue(chunk.Position + Vector2i.Back, out var bottom);
            map.TryGetValue(chunk.Position + Vector2i.Left, out var left);
            map.TryGetValue(chunk.Position + Vector2i.Right, out var right);
            map.TryGetValue(chunk.Position + Vector2i.Forward + Vector2i.Left, out var topleft);
            map.TryGetValue(chunk.Position + Vector2i.Back + Vector2i.Left, out var bottomleft);
            map.TryGetValue(chunk.Position + Vector2i.Forward + Vector2i.Right, out var topright);
            map.TryGetValue(chunk.Position + Vector2i.Back + Vector2i.Right, out var bottomright);

            var bc = chunk.BlocksCount;
            var blocks = new ChunkMaskBlock[chunk.BlocksCount + 2*border, chunk.BlocksCount + 2*border];

            CopyBlocks(chunk, blocks, new Bound2i(GridPos.Zero, new GridPos(bc - 1)), new GridPos(border));

            if (border > 0)
            {
                if(bottomleft != null)
                    CopyBlocks(bottomleft, blocks, new Bound2i(new GridPos (bc - border), border, border), GridPos.Zero);
                if (bottom != null)
                    CopyBlocks(bottom, blocks, new Bound2i(new GridPos(0, bc - border), bc, border), new GridPos(border, 0));
                if (bottomright != null)
                    CopyBlocks(bottomright, blocks, new Bound2i(new GridPos(0, bc - border), border, border), new GridPos(bc + border, 0));
                if (left != null)
                    CopyBlocks(left, blocks, new Bound2i(new GridPos(bc - border, 0), border, bc), new GridPos(0, border));
                if (right != null)
                    CopyBlocks(right, blocks, new Bound2i(new GridPos(0, 0), border, bc), new GridPos(bc+border, border));
                if (topleft != null)
                    CopyBlocks(topleft, blocks, new Bound2i(new GridPos(bc - border, 0), border, border), new GridPos(0, bc + border));
                if (top != null)
                    CopyBlocks(top, blocks, new Bound2i(new GridPos(0, 0), bc, border), new GridPos(border, bc+border));
                if (topright != null)
                    CopyBlocks(topright, blocks, new Bound2i(GridPos.Zero, border, border), new GridPos(bc + border));
            }

            return blocks;
        }

        private void CopyBlocks(Chunk src, ChunkMaskBlock[,] dest, Bound2i srcBounds, GridPos destPosition)
        {
            for (int z = srcBounds.Min.Z; z <= srcBounds.Max.Z; z++)
            {
                var destPosZ = destPosition.Z + (z - srcBounds.Min.Z);
                for (int x = srcBounds.Min.X; x <= srcBounds.Max.X; x++)
                {
                    var destPosX = destPosition.X + (x - srcBounds.Min.X);
                    dest[destPosX, destPosZ].Block = src.BlockType[x, z];
                    dest[destPosX, destPosZ].Normal = src.NormalMap[x, z];
                }
            }
        }

        Vector3[] CalculateNormals(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            map.TryGetValue(chunk.Position + Vector2i.Forward, out var top);
            map.TryGetValue(chunk.Position + Vector2i.Back, out var bottom);
            map.TryGetValue(chunk.Position + Vector2i.Left, out var left);
            map.TryGetValue(chunk.Position + Vector2i.Right, out var right);

            Vector3[] result = new Vector3[chunk.GridSize * chunk.GridSize];

            //Inner loop
            for (int z = 1; z < chunk.GridSize - 1; z++)
                for (int x = 1; x < chunk.GridSize - 1; x++)
                    result[x + z*chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z], chunk.HeightMap[x + 1, z],
                        chunk.HeightMap[x, z - 1], chunk.HeightMap[x, z + 1]);

            //Outer loops
            for (int i = 1; i < chunk.GridSize - 1; i++)
            {
                var x = 0;
                var z = i;
                if (left != null)
                    result[x + z*chunk.GridSize] = CalculateNormal(left.HeightMap[chunk.GridSize - 2, z],
                        chunk.HeightMap[x + 1, z], chunk.HeightMap[x, z - 1], chunk.HeightMap[x, z + 1]);
                x = chunk.GridSize - 1;
                if (right != null)
                    result[x + z * chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z],  
                        right.HeightMap[1, z], chunk.HeightMap[x, z - 1], chunk.HeightMap[x, z + 1]);
                x = i;
                z = 0;
                if (bottom != null)
                    result[x + z * chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z], chunk.HeightMap[x + 1, z], 
                        bottom.HeightMap[x, chunk.GridSize - 2], chunk.HeightMap[x, z + 1]);
                z = chunk.GridSize - 1;
                if (top != null)
                    result[x + z * chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z], chunk.HeightMap[x + 1, z],
                        chunk.HeightMap[x, z - 1], top.HeightMap[x, 1]);
            }

            //Corners
            if (bottom != null && left != null)
            {
                var x = 0;
                var z = 0;
                result[x + z * chunk.GridSize] = CalculateNormal(left.HeightMap[chunk.GridSize - 2, z], chunk.HeightMap[x + 1, z],
                    bottom.HeightMap[x, chunk.GridSize - 2], chunk.HeightMap[x, z + 1]);
            }
            if (top != null && left != null)
            {
                var x = 0;
                var z = chunk.GridSize - 1;
                result[x + z * chunk.GridSize] = CalculateNormal(left.HeightMap[chunk.GridSize - 2, z], chunk.HeightMap[x + 1, z],
                    chunk.HeightMap[x, z - 1], top.HeightMap[x, 1]);
            }
            if (top != null && right != null)
            {
                var x = chunk.GridSize - 1;
                var z = chunk.GridSize - 1;
                result[x + z * chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z], right.HeightMap[1, z],
                    chunk.HeightMap[x, z - 1], top.HeightMap[x, 1]);
            }
            if (bottom != null && right != null)
            {
                var x = chunk.GridSize - 1;
                var z = 0;
                result[x + z * chunk.GridSize] = CalculateNormal(chunk.HeightMap[x - 1, z], right.HeightMap[1, z],
                    bottom.HeightMap[x, chunk.GridSize - 2], chunk.HeightMap[x, z + 1]);
            }

            return result;
        }

        private Vector3 CalculateNormal(float heightX0, float heightx1, float heightZ0, float heightZ1)
        {
            //Based on http://gamedev.stackexchange.com/questions/70546/problem-calculating-normals-for-heightmaps
            var sx = heightX0 - heightx1;
            var sy = heightZ0 - heightZ1;

            return new Vector3(sx, 2, sy).normalized;
        }

        public struct ChunkMaskBlock
        {
            public BlockType Block;
            //public float Inclination;
            public Vector3 Normal;
        }

        public struct Textures
        {
            public Texture Diffuse;
            public Texture Normal;
        }

        /// <summary>
        /// 2d texture map of blocks properties (normal for now)
        /// </summary>
        public struct TerrainMap
        {
            public Texture2D Map;
            public int Border;                      //Map is larger of place of interest by given border (for proper filtering)
        }
    }
}
