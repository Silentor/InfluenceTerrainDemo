using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Materials : MonoBehaviour
    {
        public Material Grass;

        public static Materials Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<Materials>();

                return _instance;
            }
        }

        private static Materials _instance;
    }
}
