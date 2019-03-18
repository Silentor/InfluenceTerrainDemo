﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using JetBrains.Annotations;
//using NUnit.Framework;
using TerrainDemo.Macro;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Micro
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Blocks : IEquatable<Blocks>
    {
        public readonly Heights Height;
        public readonly BlockType Base;
        public readonly BlockType Underground;
        public readonly BlockType Ground;
        /// <summary>
        /// How blocks overlapped between main map and objects maps
        /// nnnn nnxx - id of another object map with overlapped block
        /// xxxx xxnn - overlap state <see cref="BlockOverlapState"/>
        /// 0000 0000 - no overlap at all
        /// nnnn nn00 - object block hidden underground
        /// nnnn nn01 - object block overlap main map block
        /// nnnn nn10 - object block float over main map block
        /// nnnn nn11 - reserved
        /// </summary>
        private readonly byte OverlapState;  

        public BlockType Top => Ground 
                                != BlockType.Empty ? Ground : Underground 
                                                              != BlockType.Empty ? Underground : Base;

        public bool IsEmpty => Ground == BlockType.Empty && Underground == BlockType.Empty && Base == BlockType.Empty;

        public bool IsOverlapped => OverlapState == 0;

        public static readonly Blocks Empty;

        public Blocks(BlockType ground, BlockType underground, in Heights heights) : this(ground, underground, heights, 0)
        {
        }

        private Blocks(BlockType ground, BlockType underground, in Heights heights, byte overlapState)
        {
            //Fix heights if needed
            float underHeight = heights.Underground, groundHeight = heights.Main;
            var heightFixed = false;

            if (underground != BlockType.Empty && underHeight == heights.Base)
                underground = BlockType.Empty;

            if (underground == BlockType.Empty && underHeight > heights.Base)
            {
                underHeight = heights.Base;
                heightFixed = true;
            }

            if (ground != BlockType.Empty && groundHeight == underHeight)
            {
                ground = BlockType.Empty;
            }

            if (ground == BlockType.Empty && groundHeight > underHeight)
            {
                groundHeight = underHeight;
                heightFixed = true;
            }

            Base = BlockType.Bedrock;
            Underground = underground;
            Ground = ground;

            if (heightFixed)
                Height = new Heights(groundHeight, underHeight, heights.Base);
            else
                Height = heights;

            OverlapState = overlapState;

            Assert.IsTrue(Ground != BlockType.Empty || Height.Main - Height.Underground == 0, $"Block is wrong");
            Assert.IsTrue(Underground != BlockType.Empty || Height.Underground - Height.Base == 0, $"Block is wrong");
        }

        [Pure]
        public Blocks MutateHeight(in Heights heights)
        {
            return new Blocks(Ground, Underground, heights);
        }

        [Pure]
        public Blocks MutateOverlapState(int objectMapId, BlockOverlapState newState)  //todo consider implement TryMutateOverlapState to not instantiate another Blocks if overlap state is not changed
        {
            if(objectMapId < 0 || objectMapId > 62) throw new ArgumentOutOfRangeException(nameof(objectMapId));

            if (newState == BlockOverlapState.None)
                objectMapId = 0;
            if (newState != BlockOverlapState.None)
            {
                newState = newState - 1;
                objectMapId += 1;
            }
            return new Blocks(Ground, Underground, Height, (byte)((objectMapId << 2) | (int)newState));
        }

        [Pure]
        public float GetNominalHeight()
        {
            //Simplified after strict layers ordering
            return Height.Main;
            /*
            if (Ground != BlockType.Empty)
                return Heights.Layer1Height;
            else if (Underground != BlockType.Empty)
                return Heights.UndergroundHeight;
            else
                return Heights.BaseHeight;
                */
        }

        [Pure]
        public Interval GetMainLayerWidth()
        {
            return IsEmpty ? Interval.Empty : new Interval(Height.Underground, Height.Main);
        }

        [Pure]
        public Interval GetUnderLayerWidth()
        {
            return IsEmpty ? Interval.Empty : new Interval(Height.Base, Height.Underground);
        }

        [Pure]
        public Interval GetTotalWidth()
        {
            return IsEmpty ? Interval.Empty : new Interval(Height.Base, Height.Main);
        }

        public (int mapId, BlockOverlapState state) GetOverlapState()
        {
            if (OverlapState == 0)
                return (0, BlockOverlapState.None);
            else
            {
                var mapId = (OverlapState >> 2) - 1;
                var state = (OverlapState & 0x03) + 1;
                return (mapId, (BlockOverlapState) state);
            }
        }

        public override string ToString()
        {
            if (IsEmpty) return "(Empty)";
            return string.Format("({0} {1} {2})",
                Ground != BlockType.Empty ? Ground + ":" + Height.Main.ToString("N1") : "Empty",
                Underground != BlockType.Empty ? Underground + ":" + Height.Underground.ToString("N1") : "Empty",
                Base + ":" + Height.Base.ToString("N1"));

        }

        public override bool Equals(object obj)
        {
            if (!(obj is Blocks))
            {
                return false;
            }

            var blocks = (Blocks)obj;
            return Base == blocks.Base 
                   && Underground == blocks.Underground
                   && Ground == blocks.Ground;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Height.GetHashCode();
                hashCode = hashCode * -1521134295 + Base.GetHashCode();
                hashCode = hashCode * -1521134295 + Underground.GetHashCode();
                hashCode = hashCode * -1521134295 + Ground.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(Blocks other)
        {
            return Base == other.Base && Underground == other.Underground && Ground == other.Ground && Height.Equals(other.Height);
        }

        public static bool operator ==(Blocks left, Blocks right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Blocks left, Blocks right)
        {
            return !left.Equals(right);
        }
    }
}
