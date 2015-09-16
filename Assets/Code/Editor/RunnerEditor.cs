using System.Collections.Generic;
using Assets.Code.Layout;
using UnityEditor;
using UnityEngine;

namespace Assets.Code.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor 
    {
        private Runner _target;
        private IEnumerable<Zone> _layout;
        private Dictionary<Vector2i, Chunk> _map;
        private Land _land;

        void OnEnable()
        {
            _target = (Runner) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Create land"))
                {
                    _target.BuildAll();
                }

                if (GUILayout.Button("Recreate map"))
                {
                    _target.RecreateMap();
                }
            }
        }
    }
}
