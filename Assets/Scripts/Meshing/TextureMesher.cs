using System.Collections.Generic;
using System.Linq;
using Assets.Code.Tools;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    public class TextureMesher
    {
        public AverageTimer MeshTimer { get { return _meshTimer;} }
        public AverageTimer TextureTimer { get { return _textureTimer; } }

        public TextureMesher(ILandSettings settings)
        {
            _grassTex =
                settings.Blocks.Where(b => b.Block == BlockType.Grass).Select(b => b.Texture).First();
            _stoneTex = settings.Blocks.Where(b => b.Block == BlockType.Rock).Select(b => b.Texture).First();
            _waterTex = settings.Blocks.Where(b => b.Block == BlockType.Water).Select(b => b.Texture).First();
            _sandTex = settings.Blocks.Where(b => b.Block == BlockType.Sand).Select(b => b.Texture).First();

            //Legacy texture generation
            //_grass = _grassTex.GetPixels();
            //_stone = _stoneTex.GetPixels();
        }

        ~TextureMesher()
        {
            //if(_renderTexture != null)                    //todo Call from main thread
            //    _renderTexture.Release();     
        }

        public ChunkModel Generate(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            _meshTimer.Start();

            var mesh = new Mesh();

            var verts = new Vector3[chunk.GridSize * chunk.GridSize];
            for (int z = 0; z < chunk.GridSize; z++)
                for (int x = 0; x < chunk.GridSize; x++)
                    verts[x + z * chunk.GridSize] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);
            mesh.vertices = verts;

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

            var uv = new Vector2[mesh.vertexCount];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(verts[i].x/(chunk.GridSize - 1), verts[i].z/(chunk.GridSize - 1));
            mesh.uv = uv;

            //mesh.RecalculateNormals();
            mesh.normals = CalculateNormals(chunk, map);

            mesh.RecalculateBounds();

            _meshTimer.Stop();

            //Generate texture
            _textureTimer.Start();
            //var tex = GenerateTextureCPU(chunk);
            var tex = GenerateTextureShader(chunk);
            _textureTimer.Stop();

            var material = new Material(Materials.Instance.Grass);
            material.mainTexture = tex;

            return new ChunkModel {Mesh = mesh, Material = material };
        }

        private readonly Color[] _grass;
        private readonly Color[] _stone;
        private readonly Texture2D _grassTex;
        private readonly Texture2D _stoneTex;
        private readonly Texture2D _waterTex;
        private readonly Texture2D _sandTex;
        private RenderTexture _renderTexture;
        private readonly AverageTimer _meshTimer = new AverageTimer();
        private readonly AverageTimer _textureTimer = new AverageTimer();

        private Texture2D GenerateTextureCPU(Chunk chunk)
        {
            var result = new Texture2D(1024, 1024, TextureFormat.RGB24, true);
            var pixels = result.GetPixels();

            for (int z = 0; z < 1024 - 1; z++)
                for (int x = 0; x < 1024 - 1; x++)
                {
                    var blockType = chunk.BlockType[x/64, z/64];
                    if (blockType == BlockType.Rock)
                        pixels[x + z*1024] = _stone[x + z * 1024];
                    else
                        pixels[x + z*1024] = _grass[x + z * 1024];
                }

            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        private RenderTexture GetRenderTexture()
        {
            //if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(1024, 1024, 0);
                _renderTexture.wrapMode = TextureWrapMode.Clamp;
                _renderTexture.enableRandomWrite = true;
                _renderTexture.useMipMap = false;
                _renderTexture.generateMips = false;
                _renderTexture.Create();
            }

            return _renderTexture;
        }

        private Texture GenerateTextureShader(Chunk chunk)
        {
            var mask = new Texture2D(chunk.BlocksCount, chunk.BlocksCount, TextureFormat.RGB24, false);      //todo consider pass mask as ComputeBuffer, to avoid create texture costs
            mask.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[chunk.BlocksCount * chunk.BlocksCount];
            for (int z = 0; z < chunk.BlocksCount; z++)
                for (int x = 0; x < chunk.BlocksCount; x++)
                {
                    var blockType = chunk.BlockType[x, z];
                    if (blockType == BlockType.Rock)
                        pixels[x + z*16] = new Color(1, 0, 0, 0);
                    else if(blockType == BlockType.Water)
                        pixels[x + z*16] = new Color(0, 1, 0, 0);
                    else if (blockType == BlockType.Sand)
                        pixels[x + z * 16] = new Color(0, 0, 1, 0);
                    else
                        pixels[x + z * 16] = new Color(0, 0, 0, 1);
                }
            mask.SetPixels(pixels);
            mask.Apply(false);

            var renderTex = GetRenderTexture();
            var shader = Materials.Instance.TextureBlendShader;
            shader.SetTexture(0, "mask", mask);
            shader.SetTexture(0, "grass", _grassTex);
            shader.SetTexture(0, "stone", _stoneTex);
            shader.SetTexture(0, "sand", _sandTex);
            shader.SetTexture(0, "water", _waterTex);
            shader.SetTexture(0, "result", renderTex);
            shader.Dispatch(0, renderTex.width / 8, renderTex.height / 8, 1);

            //Render computed texture to another one to generate auto mipmaps
            var renderTex2 = new RenderTexture(renderTex.width, renderTex.height, 0);
            renderTex2.useMipMap = true;
            renderTex2.generateMips = true;
            renderTex2.wrapMode = renderTex.wrapMode;
            renderTex2.Create();
            Graphics.Blit(renderTex, renderTex2);

            //Destroy old texture
            renderTex.Release();
            Object.Destroy(renderTex);

            return renderTex2;
        }

        Vector3[] CalculateNormals(Chunk chunk, Dictionary<Vector2i, Chunk> map)
        {
            Chunk top, bottom, left, right;

            map.TryGetValue(chunk.Position + Vector2i.Forward, out top);
            map.TryGetValue(chunk.Position + Vector2i.Back, out bottom);
            map.TryGetValue(chunk.Position + Vector2i.Left, out left);
            map.TryGetValue(chunk.Position + Vector2i.Right, out right);

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

        public struct ChunkModel
        {
            public Mesh Mesh;
            public Material Material;
        }

        public class ChunkHeightmapExtender
        {
            private readonly Chunk _mainChunk;
            private readonly Chunk _topChunk;

            public ChunkHeightmapExtender(Chunk mainChunk, Dictionary<Vector2i, Chunk> map)
            {
                _mainChunk = mainChunk;
            }
        }
    }
}
