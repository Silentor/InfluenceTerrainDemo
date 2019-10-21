using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Generators;
using TerrainDemo.Generators.MapObjects;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Visualization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Input = TerrainDemo.Hero.Input;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Quaternion = OpenToolkit.Mathematics.Quaternion;

namespace TerrainDemo
{
    public class TriRunner : MonoBehaviour
    {
        [Header("Generator settings")]
        public int Seed;
        public bool RandomizeSeed;
        public float LandSize = 100;
        public float CellSide = 10;
        public float GridPerturbFreq = 0.01f;
        public float GridPerturbPower = 10;
        public float InfluencePerturbFreq = 0.25f;
        public float InfluencePerturbPower = 4;
        public BiomeSettings[] Biomes = new BiomeSettings[0];

        [Header("Visualizer settings")]
        //public Renderer.TerrainRenderMode RenderMode = Renderer.TerrainRenderMode.Terrain;
        public Renderer.TerrainLayerToRender RenderLayer = Renderer.TerrainLayerToRender.Main;

        [Header("Macro")]
        public Renderer.MacroCellInfluenceMode MacroCellInfluenceVisualization;
        public Material VertexColoredMat;

        [Header("Micro")]
        public Renderer.BlockTextureMode TextureMode;
        public Material TexturedMat;
        public Material ObstaclesMat;

        [Header("Actors")]
        public ActorView SmallBipedActorPrefab;
        public ActorView MedBipedActorPrefab;
        public ActorView BigBipedActorPrefab;

        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        public NavigationMap NavMap { get; private set; }
        public MacroTemplate Land { get; private set; }

        public IReadOnlyCollection<BlockSettings> AllBlocks => _allBlocks;
        public IReadOnlyDictionary<BlockType, BlockSettings> AllBlocksDict 
        {
	        get
	        {
		        if ( _allBlocksDict == null )
		        {
			        _allBlocksDict = new Dictionary<BlockType, BlockSettings>();
			        foreach (var blockSettings in AllBlocks)
			        {
				        _allBlocksDict[blockSettings.Block] = blockSettings;
			        }
                }

		        return _allBlocksDict;
	        }
		} 

        private Tools.Random _random;
        private Renderer _renderer;
        private BlockSettings[] _allBlocks;
        private Dictionary<BlockType, BlockSettings> _allBlocksDict;
        private GUIStyle _defaultStyle;
        private ObjectMap _bridge;
        private Actor _hero;
        private ObserverController _observer;
        private Actor _npc;
        private Actor _npc2;

        public void Render(TriRunner renderSettings)
        {
            var timer = Stopwatch.StartNew();

            _renderer.Clear();

            //Visualization
            //if (renderSettings.RenderMode == Renderer.TerrainRenderMode.Macro)
                //_renderer.Render(Macro, renderSettings);
            //else
                //_renderer.Render(Micro, mode);
                _renderer.Render(Micro, renderSettings);               //Experimental renderer

            timer.Stop();

            Debug.LogFormat("Rendered all map in {0} msec", timer.ElapsedMilliseconds);
        }

        public void Generate()
        {
            Prepare();

            var template = new MacroTemplate(_random);
            Land = template;

            //Fully generate Macro Map
            Macro = template.CreateMacroMap(this);

            var microtimer = Stopwatch.StartNew();
            Micro = new MicroMap(Macro, this);

            foreach (var zone in Macro.Zones)
            {
                template.GenerateMicroZone2(Macro, zone, Micro);
            }
            Micro.GenerateHeightmap();

            microtimer.Stop();

            /*
            var laputaGenerator = new LaputaGenerator(5);
            var laputaData = laputaGenerator.Generate(new Vector2(-14, -14), 10);
            var laputa = new ObjectMap("Laputa", laputaData.Bounds, Micro);
            laputa.SetHeights(laputaData.VertexPositions, laputaData.Heightmap);
            laputa.SetBlocks(laputaData.BlockPositions, laputaData.Blockmap);
            Micro.AddChild(laputa);
            laputa.Snap();
            laputa.Changed += MicroOnChanged;
            
            
            var bridgeGenerator = new BridgeGenerator(20, 6, 6);
            var bridgeData = bridgeGenerator.Generate(new Vector2(0, 5), 4);
            var bridge = new ObjectMap("Bridge", bridgeData.Bounds, Micro);
            bridge.SetHeights(bridgeData.VertexPositions, bridgeData.Heightmap);
            bridge.SetBlocks(bridgeData.BlockPositions, bridgeData.Blockmap);
            Micro.AddChild(bridge);
            bridge.Snap();
            bridge.Changed += MicroOnChanged;

            var mountGenerator = new MountGenerator(10, 20, Micro);
            var mountData = mountGenerator.Generate(new Vector2(14.5f, -16), -0.5f);
            var mount = new ObjectMap("Mount", mountData.Bounds, Micro);
            mount.SetHeights(mountData.VertexPositions, mountData.Heightmap);
            mount.SetBlocks(mountData.BlockPositions, mountData.Blockmap);
            Micro.AddChild(mount);
            mount.Snap();
            mount.Changed += MicroOnChanged;

            var wallGenerator = new WallGenerator(10, 10, 5, Micro);
            var wallData = wallGenerator.Generate(new Vector2(0, -3), 0);
            var wall = new ObjectMap("Wall", wallData.Bounds, Micro);
            wall.SetHeights(wallData.VertexPositions, wallData.Heightmap);
            wall.SetBlocks(wallData.BlockPositions, wallData.Blockmap);
            Micro.AddChild(wall);
            wall.Snap();
            wall.Changed += MicroOnChanged;

            wallData = wallGenerator.Generate(new Vector2(-15, 0), 0);
            wall = new ObjectMap("Wall2", wallData.Bounds, Micro);
            wall.SetHeights(wallData.VertexPositions, wallData.Heightmap);
            wall.SetBlocks(wallData.BlockPositions, wallData.Blockmap);
            Micro.AddChild(wall);
            wall.Snap();
            wall.Changed += MicroOnChanged;

            wallData = wallGenerator.Generate(new Vector2(0, -18), 0);
            wall = new ObjectMap("Wall3", wallData.Bounds, Micro);
            wall.SetHeights(wallData.VertexPositions, wallData.Heightmap);
            wall.SetBlocks(wallData.BlockPositions, wallData.Blockmap);
            Micro.AddChild(wall);
            wall.Snap();
            wall.Changed += MicroOnChanged;

            wallData = wallGenerator.Generate(new Vector2(-18, -20), 0);
            wall = new ObjectMap("Wall4", wallData.Bounds, Micro);
            wall.SetHeights(wallData.VertexPositions, wallData.Heightmap);
            wall.SetBlocks(wallData.BlockPositions, wallData.Blockmap);
            Micro.AddChild(wall);
            wall.Snap();
            wall.Changed += MicroOnChanged;
            */

			NavMap = new NavigationMap( Macro, Micro, this );            

            _hero = new Actor(Micro, NavMap, (-15, 30), Quaternion.Identity, true, "Hero", BaseLocomotor.Type.Biped);
            Micro.AddActor(_hero);

            _npc = new Actor(Micro, NavMap, (-15, 25), Quaternion.Identity, false, "Npc", BaseLocomotor.Type.BigBiped);
            Micro.AddActor(_npc);
            //_npc2 = new Actor(Micro, NavMap, (-36, -58), Vector2.One, false, "Npc2", Locomotor.Type.Wheeled);
            //Micro.AddActor(_npc2);

			//_npc.Locomotor.LookAt((Vector2)_hero.Position);

            Micro.Changed += MicroOnChanged;

            Debug.LogFormat("Created micromap in {0} msec", microtimer.ElapsedMilliseconds);

            //StartCoroutine(AnimateTest(bridge));
        }

        private void MicroOnChanged()
        {
            //Visualization
            Render(this);
        }

        private void Prepare()
        {
            if (RandomizeSeed)
                Seed = UnityEngine.Random.Range(0, int.MaxValue);

            _random = new Tools.Random(Seed);

            for (var i = 0; i < Biomes.Length; i++)
                Biomes[i].Index = i;
        }

        private IEnumerator AnimateTest(ObjectMap obj)
        {
            yield return new WaitForSeconds(1);
            var startTimeOffset = Time.time;

            while (true)
            {
                const float deltaTime = 0.1f;
                const float amplitude = 5;
                obj.Translate((Mathf.Sin(Time.time - startTimeOffset)) * deltaTime * 4);

                yield return new WaitForSeconds(deltaTime);
                //yield return null;

                //Debug.Break();  to take manual screenshots :)
            }
        }

        private void InputSourceOnStop()
        {
            _hero.Stop();
        }

        private void InputSourceOnMove(Vector2 direction)
        {
            _hero.Locomotor.Move(direction);
        }

        private void InputSourceOnStopRotating()
        {
            _hero.Locomotor.Rotate(0);
        }

        private void InputSourceOnRotate(float direction)
        {
            _hero.Locomotor.Rotate(direction);
        }

        private void InputSourceOnFire()
        {
            var hitPoint = _observer.HitPoint;

            if (hitPoint.HasValue)
            {
	            foreach ( var actor in Micro.Actors )
	            {
		            actor.Nav.Go( hitPoint.Value.position );
	            }
            }
        }

        private void InputSourceOnZoom(float zoomDelta)
        {
            _observer.SetZoomDirection(zoomDelta);
        }

#region Unity

        void Awake()
        {
            _allBlocks = Resources.LoadAll<BlockSettings>("");
        }

        void Start()
        {
            Generate();

            _observer = FindObjectOfType<ObserverController>();

            _renderer = new Renderer(new Mesher(Macro, this), this);
            Render(this);

            var inputSource = FindObjectOfType<Input>();
            inputSource.Move += InputSourceOnMove;
            inputSource.StopMoving += InputSourceOnStop;
            inputSource.Rotate += InputSourceOnRotate;
            inputSource.StopRotating += InputSourceOnStopRotating;
            inputSource.Fire += InputSourceOnFire;
            inputSource.Zoom += InputSourceOnZoom;

            _observer.SetTarget(_hero);

            //StartCoroutine(DebugAStar());
        }

        //DEBUG
        //private IEnumerator DebugAStar()
        //{
        //    SceneView.duringSceneGui += SceneViewOnDuringSceneGui;
        //    _cost.Clear();
        //    _cameFrom.Clear();

        //    var astar         = new AStarSearch<NavGraph, NavigationCell>( NavMap.MacroGraph );
        //    var startNavCell  = (NavigationCell)NavMap.GetNavNode( _npc.BlockPosition );
        //    var finishPos     = new Vector2i( 45.5f, 23.5f );
        //    var finishNavCell = (NavigationCell)NavMap.GetNavNode( finishPos );

        //    foreach (var (current, next, came, cost) in astar.CreatePathStepByStep(_npc, startNavCell, finishNavCell, true))
        //    {
        //        _current = current;
        //        _next = next;
        //        _cameFrom = came;
        //        _cost = cost;
        //        yield return new WaitForSeconds(0.1f);
        //    }

        //    //SceneView.duringSceneGui -= SceneViewOnDuringSceneGui;
        //}

        //DEBUG

        private void OnValidate()
        {
            LandBounds = new Box2(-LandSize / 2, LandSize / 2, LandSize / 2, -LandSize / 2);
        }

        private void Update()
        {
			Micro.Update( Time.deltaTime );
        }
#endregion
    }
}
