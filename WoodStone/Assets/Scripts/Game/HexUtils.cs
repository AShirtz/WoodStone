using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class simply holds the utility functions for interacting with the hex grid.
/// </summary>
public class HexUtils
{
    public static readonly float InnerRadiusFactor = Mathf.Sqrt(3f) * 0.5f;

    public enum hexDir
    {
        UpLeft,
        UpRight,
        Left,
        Right,
        DownLeft,
        DownRight
    }

    /// <summary>
    /// Converts input location (grid index) to location in space (local to game board).
    /// </summary>
    /// <param name="inVec">Integer vector in X/Z plane.</param>
    /// <returns>Vector3 local to game board corresponding to the given location.</returns>
    public static Vector3 getHexLocationForIndex (Vector2Int inVec)
    {
        Vector3 result = new Vector3
            (
                (inVec.x + (inVec.y * 0.5f) - Mathf.FloorToInt(inVec.y * 0.5f)) * (InnerRadiusFactor * 2f),
                0f,
                inVec.y * 1.5f
            );

        return result;
    }

    public static Vector2Int getIndexForDirection (Vector2Int origin, hexDir dir)
    {
        if (origin.y % 2 == 0)
        {
            switch  (dir)
            {
                case hexDir.UpLeft:
                    return new Vector2Int(origin.x - 1, origin.y + 1);
                case hexDir.UpRight:
                    return new Vector2Int(origin.x, origin.y + 1);
                case hexDir.Left:
                    return new Vector2Int(origin.x - 1, origin.y);
                case hexDir.Right:
                    return new Vector2Int(origin.x + 1, origin.y);
                case hexDir.DownLeft:
                    return new Vector2Int(origin.x - 1, origin.y - 1);
                case hexDir.DownRight:
                    return new Vector2Int(origin.x, origin.y - 1);
            }
        }
        else
        {
            switch (dir)
            {
                case hexDir.UpLeft:
                    return new Vector2Int(origin.x, origin.y + 1);
                case hexDir.UpRight:
                    return new Vector2Int(origin.x + 1, origin.y + 1);
                case hexDir.Left:
                    return new Vector2Int(origin.x - 1, origin.y);
                case hexDir.Right:
                    return new Vector2Int(origin.x + 1, origin.y);
                case hexDir.DownLeft:
                    return new Vector2Int(origin.x, origin.y - 1);
                case hexDir.DownRight:
                    return new Vector2Int(origin.x + 1, origin.y - 1);
            }
        }

        Debug.LogError("Unable to calculate grid index for given direction");
        return Vector2Int.zero;
    }
}
