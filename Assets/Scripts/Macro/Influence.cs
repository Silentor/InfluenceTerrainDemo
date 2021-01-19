using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TerrainDemo.Macro
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Influence : IEnumerable<ValueTuple<int, float>>
    {
        public const byte InvalidZoneId = unchecked((byte)Zone.InvalidId);
        public const int Capacity = 4;

        public readonly byte Zone1Id;
        public readonly byte Zone2Id;
        public readonly byte Zone3Id;
        public readonly byte Zone4Id;

        public readonly float Zone1Weight;
        public readonly float Zone2Weight;
        public readonly float Zone3Weight;
        public readonly float Zone4Weight;

        public int GetZoneId(int index)
        {
            if (index < Count)
            {
                if (index == 0)
                    return Zone1Id;
                if(index == 1)
                    return Zone2Id;
                if (index == 2)
                    return Zone3Id;
                if (index == 3)
                    return Zone4Id;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public float GetWeight(int index)
        {
            if (index < Count)
            {
                if (index == 0)
                    return Zone1Weight;
                if (index == 1)
                    return Zone2Weight;
                if (index == 2)
                    return Zone3Weight;
                if (index == 3)
                    return Zone4Weight;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Returns zone id and weight
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public (int ZoneId, float Weight) GetInfluence(int index)
        {
            switch (index)
            {
                case 0:
                    return (Zone1Id, Zone1Weight);
                case 1:
                    return (Zone2Id, Zone2Weight);
                case 2:
                    return (Zone3Id, Zone3Weight);
                case 3:
                    return (Zone4Id, Zone4Weight);
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }


        public static readonly Influence Empty = new Influence(new List<(Cell, float)>(0));

        public bool IsEmpty => Count == 0;

        public int Count
        {
            get
            {
                if (_count == 255)
                {
                    if (Zone1Id == InvalidZoneId)
                        _count = 0;
                    else if (Zone2Id == InvalidZoneId)
                        _count = 1;
                    else if (Zone3Id == InvalidZoneId)
                        _count = 2;
                    else if (Zone4Id == InvalidZoneId)
                        _count = 3;
                    else
                        _count = 4;
                }

                return _count;
            }
        }

        public int GetMostInfluenceZone()
        {
            if (_maxInfluenceIndex == InvalidZoneId)
            {
                _maxInfluenceIndex = Zone1Id;
                var maxInfluenceValue = Zone1Weight;

                if (Zone2Weight > maxInfluenceValue)
                {
                    maxInfluenceValue = Zone2Weight;
                    _maxInfluenceIndex = Zone2Id;
                }

                if (Zone3Weight > maxInfluenceValue)
                {
                    maxInfluenceValue = Zone3Weight;
                    _maxInfluenceIndex = Zone3Id;
                }

                if (Zone4Weight > maxInfluenceValue)
                {
                    //maxInfluenceValue = Zone4Weight;
                    _maxInfluenceIndex = Zone4Id;
                }
            }

            return _maxInfluenceIndex;
        }

        public Influence(uint id)
        {
            Zone1Id = (byte)id;
            Zone1Weight = 1;
            Zone2Id = InvalidZoneId;
            Zone2Weight = 0;
            Zone3Id = InvalidZoneId;
            Zone3Weight = 0;
            Zone4Id = InvalidZoneId;
            Zone4Weight = 0;

            _count = 1;
            _maxInfluenceIndex = Zone1Id;
        }

        public Influence(List<(Cell, float)> cellsAndWeights)
        {
            Zone1Id = InvalidZoneId;
            Zone1Weight = 0;
            Zone2Id = InvalidZoneId;
            Zone2Weight = 0;
            Zone3Id = InvalidZoneId;
            Zone3Weight = 0;
            Zone4Id = InvalidZoneId;
            Zone4Weight = 0;

            //todo Just to be sure, but after KD tree query zones already may be sorted by weight
            cellsAndWeights.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            for (int i = 0; i < cellsAndWeights.Count; i++)
            {
                var cw = cellsAndWeights[i];
                if (AddCell(cw, ref Zone1Id, ref Zone1Weight))
                    continue;
                if (AddCell(cw, ref Zone2Id, ref Zone2Weight))
                    continue;
                if (AddCell(cw, ref Zone3Id, ref Zone3Weight))
                    continue;
                if (AddCell(cw, ref Zone4Id, ref Zone4Weight))
                    continue;

                //Influence is full, no more zones
                break;
            }

            //Normalize todo probably should be normalized when needed, not on create
            var sum = Zone1Weight + Zone2Weight + Zone3Weight + Zone4Weight;
            if (sum > 0)
            {
                Zone1Weight /= sum;
                Zone2Weight /= sum;
                Zone3Weight /= sum;
                Zone4Weight /= sum;
            }

            _count = 255;
            _maxInfluenceIndex = InvalidZoneId;
        }

        public override string ToString()
        {
            switch (Count)
            {
                default:
                    return "()";
                case 1:
                    return $"({Zone1Id}:{Zone1Weight:F2})";
                case 2:
                    return $"({Zone1Id}:{Zone1Weight:F2}, {Zone2Id}:{Zone2Weight:F2})";
                case 3:
                    return $"({Zone1Id}:{Zone1Weight:F2}, {Zone2Id}:{Zone2Weight:F2}, {Zone3Id}:{Zone3Weight:F2})";
                case 4:
                    return $"({Zone1Id}:{Zone1Weight:F2}, {Zone2Id}:{Zone2Weight:F2}, {Zone3Id}:{Zone3Weight:F2}, {Zone4Id}:{Zone4Weight:F2})";
            }
        }

        private byte _count;
        private byte _maxInfluenceIndex;

        private static bool AddCell((Cell, float) cw, ref byte zoneId, ref float zoneWeight)
        {
            if (zoneId == cw.Item1.ZoneId)
            {
                zoneWeight += cw.Item2;
                return true;
            }
            else if (zoneId == InvalidZoneId)
            {
                zoneId = (byte)cw.Item1.ZoneId;
                zoneWeight = cw.Item2;
                return true;
            }

            return false;
        }

        #region Enumerable

        public IEnumerator<(int, float)> GetEnumerator()
        {
            if(Zone1Id != InvalidZoneId)
                yield return (Zone1Id, Zone1Weight);
            if (Zone2Id != InvalidZoneId)
                yield return (Zone2Id, Zone2Weight);
            if (Zone3Id != InvalidZoneId)
                yield return (Zone3Id, Zone3Weight);
            if (Zone4Id != InvalidZoneId)
                yield return (Zone4Id, Zone4Weight);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
