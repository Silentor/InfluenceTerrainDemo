using System;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using UnityEngine;

namespace TerrainDemo.Visualization
{
    [SelectionBase]
    public class ActorView : MonoBehaviour
    {
        public Actor Actor { get; private set; }

        public void Init([NotNull] Actor actor)
        {
            if (Actor != null) throw new InvalidOperationException("Double init is not allowed");
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));

            if (!Actor.IsHero)
            {
                var renderer = GetComponentInChildren<UnityEngine.Renderer>();
                renderer.material.color = Color.gray;
                gameObject.name = actor.Name;
            }
            else
            {
	            var renderer = GetComponentInChildren<UnityEngine.Renderer>();
	            renderer.material.color = Color.white;

                gameObject.name = actor.Name;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if(Actor == null) throw new ArgumentException("Actor must be initialized");
        }
    }

    
}
