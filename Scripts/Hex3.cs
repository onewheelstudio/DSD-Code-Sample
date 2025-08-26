using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HexGame.Resources;
using System.Linq;
using System;

namespace HexGame.Grid
{
    [HideReferenceObjectPicker]
    [System.Serializable]
    public struct Hex3 : IEquatable<Hex3>
    {
        public Hex3(int q, int r, int s)

        {
            this.q = q;
            this.r = r;
            this.s = s;

            if (q + r + s != 0)
            {
                Debug.LogWarning($"Invalid Hex Coordinates {q} {r} {s}");
                if (Application.isEditor)
                    Debug.Break();
            }
        }

        public int q;
        public int r;
        public int s;

        public const float SQRT3 = 1.73205080757f;
        public static Hex3[] neighborVectors = {new Hex3(1,0,-1), new Hex3(1,-1,0), new Hex3(0,-1,1),
                                      new Hex3(-1,0,+1), new Hex3(-1,1,0), new Hex3(0,1,-1)};
        public static readonly Vector3 qBasis = new Vector3(SQRT3, 0, 0);
        public static readonly Vector3 rBasis = new Vector3(0.5f * SQRT3, 0, -1.5f);
        public static Vector3[] edgeMidpoints = { new Vector3(SQRT3 / 4, 0f, 0.75f), new Vector3(SQRT3 / 2, 0, 0), 
                                                    new Vector3(SQRT3 / 4, 0f, -0.75f), new Vector3(-SQRT3 / 4, 0f, -0.75f), 
                                                    new Vector3(-SQRT3 / 2, 0, 0), new Vector3(-SQRT3 / 4, 0f, 0.75f) };
        public static Vector3[] vertices = {new Vector3(0,0,1), new Vector3(SQRT3/2, 0, 0.5f),
                                            new Vector3(SQRT3 / 2, 0, -0.5f), new Vector3(0,0,-1),
                                            new Vector3(-SQRT3/2, 0, -0.5f), new Vector3(-SQRT3/2, 0,0.5f)};

        public static readonly Vector3 qBasisFlat = new Vector3(1.5f, SQRT3/2, 0);
        public static readonly Vector3 rBasisFlat = new Vector3(0, SQRT3, 0);

        public static Hex3 Zero => new Hex3(0, 0, 0);

        public static Hex3 operator +(Hex3 h1, Hex3 h2)
        {
            return new Hex3(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }
        public static Hex3 operator -(Hex3 h1, Hex3 h2)
        {
            return new Hex3(h1.q - h2.q, h1.r - h2.r, h1.s - h2.s);
        }
        public static bool operator ==(Hex3 h1, Hex3 h2)
        {
            return h1.q == h2.q && h1.r == h2.r && h1.s == h2.s;
        }
        public static bool operator !=(Hex3 h1, Hex3 h2)
        {
            return !(h1 == h2);
        }

        public static Hex3 operator *(Hex3 h, int factor)
        {
            return new Hex3(h.q * factor, h.r * factor, h.s * factor);
        }

        public static implicit operator Hex3(Vector3 position)
        {
            return Hex3.Vector3ToHex3(position);
        }

        public static implicit operator Vector3(Hex3 location)
        {
            return Hex3.Hex3ToVector3(location);
        }

        public bool Equals(Hex3 other)
        {
            return this.q == other.q && this.r == other.r && this.s == other.s;
        }

        public override bool Equals(object obj)
        {
            return obj is Hex3 hex && Equals(hex);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(q, r, s);
        }

        public override string ToString()
        {
            return $"Hex ({q},{r},{s})";
        }

        public string StringCoordinates()
        {
            return $"({q},{r},{s})";
        }

        public string ToNumber()
        {
            return $"{Mathf.Abs(q)}{Mathf.Abs(r)}{Mathf.Abs(s)}";
        }

        public static Hex3 Vector3ToHex3(Vector3 position)
        {
            float q = SQRT3 / 3f * position.x + position.z / 3f;
            float r = -2f / 3f * position.z;
            float s = -r - q;

            return Hex3Round(q, r, s);
        }

        public static Hex3 Vector3ToFlatHex3(Vector3 position)
        {
            float q = 2f / 3f * position.x;
            float r = -position.x / 3f + SQRT3 / 3f * position.z ;
            float s = -r - q;

            return Hex3Round(q, r, s);
        }

        public static Hex3 Round(Hex3 hex)
        {
            return Hex3Round(hex.q, hex.r, hex.s);
        }

        public static Hex3 Hex3Round(float q, float r, float s)
        {
            int qInt = Mathf.RoundToInt(q);
            int rInt = Mathf.RoundToInt(r);
            int sInt = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(q - qInt);
            float rDiff = Mathf.Abs(r - rInt);
            float sDiff = Mathf.Abs(s - sInt);

            if (qDiff > rDiff && qDiff > sDiff)
                qInt = -rInt - sInt;
            else if (rDiff > sDiff)
                rInt = -qInt - sInt;
            else
                sInt = -qInt - rInt;

            return new Hex3(qInt, rInt, sInt);
        }

        public static List<Hex3> GetNeighborLocations(Hex3 location)
        {
            List<Hex3> tiles = new List<Hex3>();

            for (int i = 0; i < 6; i++)
            {
                Hex3 hex = location + neighborVectors[i];
                tiles.Add(hex);
            }

            return tiles;
        }

        public static List<Vector3> GetVectices(Hex3 location)
        {
            List<Vector3> verts = new List<Vector3>();
            Vector3 center = Hex3ToVector3(location);
            for (int i = 0;i < 6;i++)
            {
                verts.Add(center + vertices[i]);
            }

            return verts;
        }

        public static List<Vector3> GetEdgeMidPoints(Hex3 location)
        {
            List<Vector3> verts = new List<Vector3>();
            Vector3 center = Hex3ToVector3(location);
            for (int i = 0; i < 6; i++)
            {
                verts.Add(center + edgeMidpoints[i]);
            }

            return verts;
        }

        public static Hex3 GetNeighbor(Hex3 hex3, int neighbor)
        {
            return hex3 + Hex3.neighborVectors[neighbor];
        }

        public static List<Hex3> GenerateRing(Hex3 center, int radius = 1)
        {
            List<Hex3> tiles = new List<Hex3>();
            Hex3 hex = center + neighborVectors[4] * radius;
            tiles.Add(hex);

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < radius; j++)
                {
                    hex = GetNeighbor(hex, i);
                    tiles.Add(hex);
                }
            }

            return tiles;
        }

        public static int DistanceBetween(Hex3 a, Hex3 b)
        {
            return Mathf.RoundToInt(Mathf.Max(Mathf.Abs(a.q - b.q), Mathf.Abs(a.r - b.r), Mathf.Abs(a.s - b.s)));
        }

        public static List<Hex3> GetHexBetweenPoints(Vector3 start, Vector3 end)
        {
            Hex3 startHex = Vector3ToHex3(start);
            Hex3 endHex = Vector3ToHex3(end);

            List<Hex3> hexLine = new List<Hex3>();
            int cubeDistance = DistanceBetween(startHex, endHex);

            for (int i = 0; i < cubeDistance; i++)
            {
                Hex3 hex = HexLerp(startHex, endHex, 1f / cubeDistance * i);
                hexLine.Add(hex);
            }
            hexLine.Add(endHex);
            return hexLine;
        }

        public static Hex3 HexLerp(Hex3 a, Hex3 b, float t)
        {
            float qLerp = Mathf.Lerp(a.q, b.q, t);
            float rLerp = Mathf.Lerp(a.r, b.r, t);
            float sLerp = Mathf.Lerp(a.s, b.s, t);

            return Hex3Round(qLerp, rLerp, sLerp);
        }

        public static List<Hex3> CreateSpiral(Hex3 center, int radius = 1)
        {
            List<Hex3> tiles = new List<Hex3>();
            tiles.Add(center);

            for (int i = 1; i < radius + 1; i++)
            {
                tiles.AddRange(GenerateRing(center, i));
            }

            return tiles;
        }

        public static Vector3 Hex3ToVector3(Hex3 tile)
        {
            return tile.q * qBasis + tile.r * rBasis;
        }

        public static Vector3 ToVector3(Hex3 tile)
        {
            return tile.q * qBasis + tile.r * rBasis;
        }

        public static Vector3 FlatHex3ToVector3(Hex3 tile)
        {
            return tile.q * new Vector3(3f / 2f, 0f, SQRT3 / 2f) + tile.r * new Vector3(0f, 0f, SQRT3);
        }

        public static bool HasNeighbor(Hex3 hex, Dictionary<Hex3, HexTile> hexTiles)
        {
            foreach (Hex3 hex3 in Hex3.GetNeighborLocations(hex))
            {
                if (hexTiles.ContainsKey(hex3))
                    return true;
            }
            return false;
        }

        public static bool FitsNeighbors(Hex3 hex, HexTile tile, Dictionary<Hex3, HexTile> hexTiles)
        {
            HexTile.HexTileSideData[] neighborSideData = new HexTile.HexTileSideData[6];

            for (int i = 0; i < 6; i++)
            {
                Hex3 neighbor = hex.GetNeighbor(i);

                if (hexTiles.TryGetValue(neighbor, out HexTile neighborHexTile))
                    neighborSideData[i] = neighborHexTile.GetRotataDataForNeighbor(i);
            }

            List<int> possibleRotation = new List<int>();
            //for each possible rotation
            for (int i = 0; i < 6; i++)
            {
                HexTile.HexTileSideData[] hexSideData = tile.RotatedData(i);
                //for each side
                for (int j = 0; j < 6; j++)
                {
                    if (hexSideData[j] != neighborSideData[j]
                        && neighborSideData[j] != HexTile.HexTileSideData.none)
                        break;
                    //if it makes it all the way its a match
                    if (j == 5)
                        possibleRotation.Add(i);
                }
            }

            if (possibleRotation.Count > 0)
                tile.Rotate(possibleRotation[0]);

            return possibleRotation.Count > 0;
        }

        public static List<Hex3> GetNeighborsInRange(Hex3 center, int range)
        {
            List<Hex3> neighbors = new List<Hex3>();

            for (int q = -range; q < range + 1; q++)
            {
                for (int r = Mathf.Max(-range, -q-range); r < Mathf.Min(range, -q + range) + 1; r++)
                {
                    neighbors.Add(new Hex3(q, r, -q - r) + center);
                }
            }

            return neighbors;
        }



        public static List<Hex3> GetNeighborsAtDistance(Hex3 center, int range)
        {
            int neighborCount = range == 0 ? 1 : 6 * range;
            List<Hex3> neighbors = new List<Hex3>(neighborCount);

            for (int i = 0; i < range; i++)
            {
                neighbors.Add(center - new Hex3(-range, i, range - i));
                neighbors.Add(center - new Hex3(range, -i, -range + i));

                neighbors.Add(center - new Hex3(range - i, -range, i));
                neighbors.Add(center - new Hex3(-range + i, range, -i));

                neighbors.Add(center - new Hex3(i, range - i, -range));
                neighbors.Add(center - new Hex3(-i, -range + i, range));
            }
            return neighbors;
        }
        
        public static List<Hex3> GetNeighborsAtDistance(Hex3 center, int innerRange, int outRange)
        {
            int neighborCount = outRange == 0 ? 1 : 6 * outRange;
            List<Hex3> neighbors = new List<Hex3>(neighborCount);

            for (int i = innerRange; i <= outRange; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    neighbors.Add(center - new Hex3(-i, j, i - j));
                    neighbors.Add(center - new Hex3(i, -j, -i + j));

                    neighbors.Add(center - new Hex3(i - j, -i, j));
                    neighbors.Add(center - new Hex3(-i + j, i, -j));

                    neighbors.Add(center - new Hex3(j, i - j, -i));
                    neighbors.Add(center - new Hex3(-j, -i + j, i));
                }
            }
            return neighbors;
        }

        /// <summary>
        /// A reduced GC version of GetNeighborsAtDistance
        /// </summary>
        /// <param name="center"></param>
        /// <param name="range"></param>
        /// <param name="neighbors"></param>
        /// <returns></returns>
        public static List<Hex3> GetNeighborsAtDistance(Hex3 center, int range, ref List<Hex3> neighbors)
        {
            int neighborCount = range == 0 ? 1 : 6 * range;
            if(neighbors.Capacity < neighborCount)
                neighbors.Capacity = neighborCount;

            for (int i = 0; i < range; i++)
            {
                neighbors.Add(center - new Hex3(-range, i, range - i));
                neighbors.Add(center - new Hex3(range, -i, -range + i));

                neighbors.Add(center - new Hex3(range - i, -range, i));
                neighbors.Add(center - new Hex3(-range + i, range, -i));

                neighbors.Add(center - new Hex3(i, range - i, -range));
                neighbors.Add(center - new Hex3(-i, -range + i, range));
            }
            return neighbors;
        }

        public int Max()
        {
            return Mathf.Max(Mathf.Abs(this.q), Mathf.Max(Mathf.Abs(this.r), Mathf.Abs(this.s)));
        }
    }
}
