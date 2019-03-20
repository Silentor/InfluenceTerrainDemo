using TerrainDemo.Hero;
using TerrainDemo.Visualization;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(ActorView))]
    public class ActorInspector : UnityEditor.Editor
    {
        private Actor _actor;

        private void OnEnable()
        {
            _actor = ((ActorView) target).Actor;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Label($"Map: {_actor.Map.Name}");
            GUILayout.Label($"Blocks: {_actor.Block}");
        }
    }
}
