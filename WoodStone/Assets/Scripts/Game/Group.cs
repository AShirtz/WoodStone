using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// This class handles the concept of a 'group', a collection of connected units belonging to the same player.
/// Not strictly necessary, but greatly simplifies access to information constant between all tiles in the group.
/// 
/// </summary>
public class Group
{
    public Player associatedPlayer = null;

    /// <summary>
    /// The tiles in this group
    /// </summary>
    public HashSet<Tile> tiles = new HashSet<Tile>();

    public int numTriples = 0;

    /// <summary>
    /// Friendly units in this group that are in contact with enemy units. Only used during combat resolution.
    /// </summary>
    public HashSet<Tile> pointsOfContactFriendly = new HashSet<Tile>();

    /// <summary>
    /// Opponent units this group is in contact with. Only used during combat resolution.
    /// </summary>
    public HashSet<Tile> pointsOfContactEnemy = new HashSet<Tile>();

    /// <summary>
    /// This constructor runs a depth-first search across the gameboard from the given seed.
    /// </summary>
    /// <param name="seed">Tile from which to grow the group</param>
    public Group (Tile seed)
    {
        this.associatedPlayer = seed.associatedPlayer;

        // Grow group from seed
        this.GrowGroup(seed);

        // Count triples in the group
        this.CountTriples();

        // Associate group with all tiles
        foreach (Tile t in tiles)
            t.owningGroup = this;
    }

    private void GrowGroup (Tile seed)
    {
        this.tiles.Add(seed);

        Tile[] neighbors = seed.getNeighbors();

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null && !this.tiles.Contains(neighbors[i]) && neighbors[i].associatedPlayer == this.associatedPlayer && !neighbors[i].shouldTerminate)
            {
                this.GrowGroup(neighbors[i]);
            }
        }
    }

    /// <summary>
    /// Counts the number of triples (3 mutually adjacent units) by "scrubbing over every unit in the group with two kernels."
    /// The two 'kernels' simply search for an up or down triangle with the unit in question being the left most unit.
    /// </summary>
    private void CountTriples()
    {
        this.numTriples = 0;

        foreach (Tile t in this.tiles)
        {
            // Find neighbor to the right
            Tile rNghbr = t.board.getTile(HexUtils.getIndexForDirection(t.gridLocation, HexUtils.hexDir.Right));

            if (rNghbr != null && rNghbr.associatedPlayer == t.associatedPlayer)
            {
                // Find the neigbor up and to the right
                Tile ruNghbr = t.board.getTile(HexUtils.getIndexForDirection(t.gridLocation, HexUtils.hexDir.UpRight));

                if (ruNghbr != null && ruNghbr.associatedPlayer == t.associatedPlayer)
                    this.numTriples++;

                // Find the neigbor down and to the right
                Tile rdNghbr = t.board.getTile(HexUtils.getIndexForDirection(t.gridLocation, HexUtils.hexDir.DownRight));

                if (rdNghbr != null && rdNghbr.associatedPlayer == t.associatedPlayer)
                    this.numTriples++;
            }
        }
    }

    public bool isInCombat ()
    {
        this.pointsOfContactFriendly.Clear();
        this.pointsOfContactEnemy.Clear();

        foreach (Tile t in this.tiles)
        {
            int prevCount = this.pointsOfContactEnemy.Count;

            t.getEnemyContacts(this.pointsOfContactEnemy);

            if (prevCount != this.pointsOfContactEnemy.Count)
                this.pointsOfContactFriendly.Add(t);
        }

        return this.pointsOfContactEnemy.Count > 0;
    }
}