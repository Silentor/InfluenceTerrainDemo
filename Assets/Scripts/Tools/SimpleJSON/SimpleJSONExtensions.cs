using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Tools.SimpleJSON
{
    /// <summary>
    /// Useful Unity extensions to cool SimpleJSON lib (http://wiki.unity3d.com/index.php/SimpleJSON)
    /// </summary>
    public static class SimpleJSONExtensions
    {
        public static Vector2 GetVector2(this JSONNode node)
        {
            return new Vector2(node["x"].AsFloat, node["y"].AsFloat);
        }

        public static JSONNode SetVector2(this JSONNode node, Vector2 value)
        {
            node = node.AsObject;                       //Because of clumsy lazy node implementation
            node["x"].AsFloat = value.x;
            node["y"].AsFloat = value.y;
            return node;
        }

        public static Vector3 GetVector3(this JSONNode node)
        {
            return new Vector3(node["x"].AsFloat, node["y"].AsFloat, node["z"].AsFloat);
        }

        public static void SetVector3(this JSONNode node, Vector3 value)
        {
            node = node.AsObject;                       //Because of clumsy lazy node implementation
            node["x"].AsFloat = value.x;
            node["y"].AsFloat = value.y;
            node["z"].AsFloat = value.z;
        }

        public static Bounds GetBounds(this JSONNode node)
        {
            return new Bounds(node["center"].GetVector3(), node["size"].GetVector3());
        }

        public static void SetBounds(this JSONNode node, Bounds value)
        {
            node = node.AsObject;                       //Because of clumsy lazy node implementation
            node["center"].SetVector3(value.center);
            node["size"].SetVector3(value.size);
        }

        public static T[] GetArray<T>(this JSONNode node, Func<JSONNode, T> parseFunc)
        {
            var items = node.AsArray;
            var result = new T[items.Count];

            for (int i = 0; i < items.Count; i++)
                result[i] = parseFunc(items[i]);

            return result;
        }

        public static void SetArray<T>(this JSONNode node, IEnumerable<T> value, Func<T, JSONNode> encodeFunc)
        {
            node = node.AsArray;                            //Because of clumsy lazy node implementation

            foreach (T val in value)
                node.Add(encodeFunc(val));
        }
    }
}
