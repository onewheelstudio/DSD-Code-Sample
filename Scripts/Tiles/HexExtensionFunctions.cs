using HexGame.Grid;
using System.Collections.Generic;
using UnityEngine;
public static class HexExtensionFunctions
{
    public static Hex3 Round(this Hex3 hex)
    {
        return Hex3.Hex3Round(hex.q, hex.r, hex.s);
    }

    public static List<Hex3> GetNeighborLocations(this Hex3 tile)
    {
        List<Hex3> tiles = new List<Hex3>();

        for (int i = 0; i < 6; i++)
        {
            Hex3 hex = tile + Hex3.neighborVectors[i];
            tiles.Add(hex);
        }

        return tiles;
    }

    public static Hex3 GetNeighbor(this Hex3 tile, int neighbor)
    {
        return tile + Hex3.neighborVectors[neighbor];
    }

    public static int DistanceTo(this Hex3 a, Hex3 b)
    {
        float[] values = new float[] { Mathf.Abs(a.q - b.q), Mathf.Abs(a.r - b.r), Mathf.Abs(a.s - b.s) };
        return Mathf.RoundToInt(Mathf.Max(values));
    }

    public static Vector3 Hex3ToVector3(this Hex3 tile)
    {
        return tile.q * Hex3.qBasis + tile.r * Hex3.rBasis;
    }

    public static Vector3 Hex3ToVector3Flat(this Hex3 location)
    {
        return location.q * Hex3.qBasisFlat + location.r * Hex3.rBasisFlat;
    }

    public static Vector3 ToVector3(this Hex3 tile)
    {
        return tile.q * Hex3.qBasis + tile.r * Hex3.rBasis;
    }

    public static Vector3 SwapYZ(this Vector3 position)
    {
        return new Vector3(position.x, position.z, position.y);
    }

}
