using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TerrainDemo.Editor
{
    /// <summary>
    /// Operates some Main Menu settings to control Scene View visualization
    /// </summary>
    [InitializeOnLoad]
    public static class RunnerEditorMainMenu
    {
        public static bool IsShowDelaunay { get; private set; }
        public static bool IsShowFill { get; private set; }
        public static bool IsShowCellId { get; private set; }

        public static bool IsShowClusterId { get; private set; }

        private const string ShowDelaunay = "Land/Show zones Delaunay";
        private const string ShowFill = "Land/Show zones fill";
        private const string ShowCellId = "Land/Show zones Id";
        private const string ShowClusterId = "Land/Show clusters Id";
        private static readonly Dictionary<string, bool> Settings;

        static RunnerEditorMainMenu()
        {
            Settings = new Dictionary<string, bool>()
            {
                {ShowDelaunay, EditorPrefs.GetBool(ShowDelaunay, false)},
                {ShowFill, EditorPrefs.GetBool(ShowFill, false)},
                {ShowCellId, EditorPrefs.GetBool(ShowCellId, false)},
                {ShowClusterId, EditorPrefs.GetBool(ShowClusterId, false)},
            };

            EditorApplication.delayCall += DelayCall;
        }


        private static void DelayCall()
        {
            foreach (var setting in Settings.ToArray())
            {
                ToggleFlag(setting.Key, setting.Value);
            }
        }

        [MenuItem(ShowDelaunay)]
        private static void MenuDelaunay()
        {
            ToggleFlag(ShowDelaunay, !Settings[ShowDelaunay]);
        }

        [MenuItem(ShowFill)]
        private static void MenuFill()
        {
            ToggleFlag(ShowFill, !Settings[ShowFill]);
        }

        [MenuItem(ShowCellId)]
        private static void MenuCellId()
        {
            ToggleFlag(ShowCellId, !Settings[ShowCellId]);
        }

        [MenuItem(ShowClusterId)]
        private static void MenuClusterId()
        {
            ToggleFlag(ShowClusterId, !Settings[ShowClusterId]);
        }

        private static void ToggleFlag(string key, bool value)
        {
            Settings[key] = value;
            Menu.SetChecked(key, value);
            EditorPrefs.SetBool(key, value);

            switch (key)
            {
                case ShowDelaunay:
                    IsShowDelaunay = value;
                    break;

                case ShowFill:
                    IsShowFill = value;
                    break;

                case ShowCellId:
                    IsShowCellId = value;
                    break;

                case ShowClusterId:
                    IsShowClusterId = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(key, "Is not defined");
            }
        }
    }
}
