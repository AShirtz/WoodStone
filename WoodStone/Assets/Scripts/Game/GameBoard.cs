using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class holds and manages references to tiles and units.
/// This class has no idea of time or game flow.
/// </summary>
public class GameBoard : MonoBehaviour
{
    public int Width = 20;
    public int Height = 20;

    public GameObject tilePrefab = null;

    /// <summary>
    /// This is used to reference units by location.
    /// </summary>
    public Tile[,] tiles = null;

    /// <summary>
    /// This is used as access to the tiles of the board that have a unit on them
    /// </summary>
    public HashSet<Tile> liveTiles = null;

    public void Initialize (int w, int h, int[,] map)
    {
        if (this.liveTiles == null)
            this.liveTiles = new HashSet<Tile>();
        else
            this.liveTiles.Clear();

        this.Width = w;
        this.Height = h;

        // Create Gameboard
        this.tiles = new Tile[this.Width, this.Height];

        // Populate Gameboard
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (map[y, x] >= 0)
                {
                    GameObject obj = GameObject.Instantiate(this.tilePrefab);
                    obj.transform.parent = this.transform;

                    Tile tle = obj.GetComponent<Tile>();
                    tle.board = this;
                    tle.gridLocation = new Vector2Int(x, y);

                    this.tiles[x, y] = tle;

                    if (map[y, x] > 0)
                    {
                        tle.associatedPlayer = GameManager.cur.players[map[y, x] - 1];
                        tle.updateViz();

                        this.liveTiles.Add(this.getTile(x, y));
                    }
                }
            }
        }
    }

    public HashSet<Group> FindGroups ()
    {
        HashSet<Group> result = new HashSet<Group>();

        // Duplicate set of live tiles
        List<Tile> ungroupedTiles = new List<Tile>(this.liveTiles);

        // Remove all tiles that will terminate
        ungroupedTiles.RemoveAll(tle => tle.shouldTerminate);

        while (ungroupedTiles.Count > 0)
        {
            // Create a new Group, picking a live tile from the ungroupedTiles list
            Group g = new Group(ungroupedTiles[0]);

            // Add g to the result set
            result.Add(g);

            // Remove all newly grouped tiles from the ungroupedTiles list
            ungroupedTiles.RemoveAll(tle => g.tiles.Contains(tle));
        }

        return result;
    }

    public Player getWinner()
    {
        List<Player> remainingPlayers = new List<Player>();

        foreach(Tile t in this.liveTiles)
        {
            if (!remainingPlayers.Contains(t.associatedPlayer))
                remainingPlayers.Add(t.associatedPlayer);
        }

        if (remainingPlayers.Count == 1)
        {
            return remainingPlayers[0];
        }
        else
        {
            return null;
        }
    }

    public Tile getTile (int x, int y)
    {
        if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
            return null;

        return this.tiles[x, y];
    }
    public Tile getTile (Vector2Int loc)
    {
        return this.getTile(loc.x, loc.y);
    }
}
