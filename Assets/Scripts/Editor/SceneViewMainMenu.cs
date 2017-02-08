using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TerrainDemo.Editor
{
    /// <summary>
    /// Operates some Main Menu settings to control Scene View visualization
    /// </summary>
    [InitializeOnLoad]
    public static class SceneViewMainMenu
    {
        private const string ShowDelaunay = "Land/Show zones Delaunay";
        private const string ShowFill = "Land/Show zones fill";
        private static bool _delaunayState;
        private static bool _fillState;

        static SceneViewMainMenu()
        {
            _delaunayState = EditorPrefs.GetBool(ShowDelaunay, false);
            _fillState = EditorPrefs.GetBool(ShowFill, false);

            EditorApplication.delayCall += DelayCall;
        }


        private static void DelayCall()
        {
            ToggleDelaunay(_delaunayState);
            ToggleFill(_fillState);
        }

        [MenuItem(ShowDelaunay)]
        private static void MenuDelaunay()
        {
            ToggleDelaunay(!_delaunayState);
        }

        [MenuItem(ShowFill)]
        private static void MenuFill()
        {
            ToggleFill(!_fillState);
        }

        private static void ToggleDelaunay(bool state)
        {
            Menu.SetChecked(ShowDelaunay, state);
            EditorPrefs.SetBool(ShowDelaunay, state);
            _delaunayState = state;
            Object.FindObjectOfType<Runner>().ShowDelaunay = state;
        }

        private static void ToggleFill(bool state)
        {
            Menu.SetChecked(ShowFill, state);
            EditorPrefs.SetBool(ShowFill, state);
            _fillState = state;
            Object.FindObjectOfType<Runner>().ShowFill = state;

        }
    }
}
