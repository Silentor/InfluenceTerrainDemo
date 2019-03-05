using UnityEngine;
using Renderer = TerrainDemo.Visualization.Renderer;

namespace TerrainDemo.Settings
{
    public class GameSettings
    {
        public static GameSettings Instance { get; }

        public Renderer.TerrainRenderMode RenderMode
        {
            get => (Renderer.TerrainRenderMode) PlayerPrefs.GetInt("RenderMode");
            set => PlayerPrefs.SetInt("RenderMode", (int)value);
        }

        public Renderer.TerrainLayerToRender RenderLayer
        {
            get => (Renderer.TerrainLayerToRender)PlayerPrefs.GetInt("RenderLayer");
            set => PlayerPrefs.SetInt("RenderLayer", (int)value);
        }


        static GameSettings()
        {
            Instance = new GameSettings();
        }
    }
}
