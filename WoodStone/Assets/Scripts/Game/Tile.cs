using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// This class represents a tile on the game board.
/// All tiles have an associated game object that keeps track of state and handles physics input (raycast targets).
/// 
/// NOTE:
///     The language is a bit mixed:
///         'Tile' is a space on the board, and generally means that the space is not associated with a player
///         'Unit' implies a tile that is associated with a player
///         
/// NOTE:
///     Equivalence between Tiles is entirely based on the gridLocation,
///     which encodes the assumption that there will only ever be one game board active at a time.
/// 
/// TODO:
///     State (management and visual representation)
///     
/// </summary>
public class Tile : MonoBehaviour
{
    public GameBoard board = null;
    public Player associatedPlayer = null;

    public bool isActive = true;            // Used to indicate whether the unit at this location has 'fired' this turn
    public bool isNew = false;              // Used to indicate whether this a new unit has been creatd on this tile this turn
    public bool shouldTerminate = false;    // Used to indicate whether this unit should be terminated
    public bool inCombat = false;

    public bool isGrowthTarget = false;     // Used to indicate that this tile is a growth target, for managing viz (TODO: Move growth target viz behavior into this class)

    public Vector2Int gridLocation = Vector2Int.zero;

    public Tile generatedTile = null;       // Used for the limited distance ruleset, you can't cancel a unit for free unless it's the last one in the chain
    public Tile originatingTile = null;     // Used to reclaim an action if this tile is terminated during the turn it is created.

    // Indicator state precedence is top -> bottom
    public Material terminationMat = null;  // Material for indicator, means that the tile is marked to be terminated
    public Material combatMat = null;       // Material for indicator, means that the is currently in combat
    public Material inactiveMat = null;     // Material for indicator, means that the tile is inactive
    public Material newMat = null;          // Material for indicator, means that the tile is new (only shown when the tile is the last in the chain)

    public Group owningGroup = null;        // Group that this tile is a part of (changes often)
    public GameObject indicator = null;     // Used to show the state of a tile (termination, combat, inactive, and new)
    public Renderer indicatorRndr = null;

    private Renderer rndr = null;

    private void Start()
    {
        if (this.rndr == null)
            this.rndr = this.GetComponent<Renderer>();

        this.transform.localPosition = HexUtils.getHexLocationForIndex(this.gridLocation);

        this.indicatorRndr = this.indicator.GetComponent<Renderer>();
    }

    public void Update()
    {
        // Update indicators to match current state
        if (this.shouldTerminate)
        {
            if (!this.indicator.activeSelf)
                this.indicator.SetActive(true);

            if (this.indicatorRndr.material != this.terminationMat)
                this.indicatorRndr.material = this.terminationMat;
        }
        else if (this.inCombat)
        {
            if (!this.indicator.activeSelf)
                this.indicator.SetActive(true);

            if (this.indicatorRndr.material != this.combatMat)
                this.indicatorRndr.material = this.combatMat;
        }
        else if (!this.isActive)
        {
            if (!this.indicator.activeSelf)
                this.indicator.SetActive(true);

            if (this.indicatorRndr.material != this.inactiveMat)
                this.indicatorRndr.material = this.inactiveMat;
        }
        else if (this.isNew)
        {
            if (!this.indicator.activeSelf)
                this.indicator.SetActive(true);

            if (this.indicatorRndr.material != this.newMat)
                this.indicatorRndr.material = this.newMat;
        }
        else
        {
            if (this.indicator.activeSelf)
                this.indicator.SetActive(false);
        }
    }

    public void TurnReset ()
    {
        this.isActive = true;
        this.isNew = false;
        this.shouldTerminate = false;
        this.inCombat = false;

        this.updateViz();
    }

    public void FullReset()
    {
        this.TurnReset();

        this.associatedPlayer = null;
        this.generatedTile = null;
        this.originatingTile = null;

        this.updateViz();
    }

    public Tile[] getNeighbors ()
    {
        Tile[] result = new Tile[6];

        result[0] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.UpLeft));
        result[1] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.UpRight));

        result[2] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.Left));
        result[3] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.Right));

        result[4] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.DownLeft));
        result[5] = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.DownRight));

        return result;
    }

    /// <summary>
    /// Retrieves all empty tiles within the 6 directions from the current tile.
    /// </summary>
    /// <returns>Set of all valid grow targets.</returns>
    public HashSet<Tile> getGrowTargets ()
    {
        // If the limited distance mode is active, calculate distance based on group population 
        int dist = GameManager.cur.groupPopulationDistanceFactor * this.owningGroup.tiles.Count;

        HashSet<Tile> result = new HashSet<Tile>();

        Tile neighbor = null;

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.UpLeft));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.UpLeft, dist);

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.UpRight));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.UpRight, dist);

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.Left));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.Left, dist);

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.Right));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.Right, dist);

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.DownLeft));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.DownLeft, dist);

        neighbor = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, HexUtils.hexDir.DownRight));
        if (neighbor != null && neighbor.associatedPlayer == null)
            neighbor.getTileLine(this.associatedPlayer, result, HexUtils.hexDir.DownRight, dist);

        return result;
    }

    /// <summary>
    /// Collects all empty tiles in the given direction.
    /// Adds to the workingSet variable.
    /// </summary>
    /// <param name="workingSet">The set into which tiles should be added.</param>
    private void getTileLine (Player owningPlayer, HashSet<Tile> workingSet, HexUtils.hexDir dir, int dist)
    {
        if (this.associatedPlayer == null && dist > 0)
        {
            workingSet.Add(this);

            bool adjacentToEnemy = false;
            Tile[] neighbors = this.getNeighbors();

            for (int i = 0; i < neighbors.Length; i++)
                if (neighbors[i] != null && neighbors[i].associatedPlayer != null && neighbors[i].associatedPlayer != owningPlayer)
                    adjacentToEnemy = true;

            Tile tle = board.getTile(HexUtils.getIndexForDirection(this.gridLocation, dir));

            if (tle != null && !adjacentToEnemy)
            {
                board.getTile(HexUtils.getIndexForDirection(this.gridLocation, dir)).getTileLine(owningPlayer, workingSet, dir, dist - 1);
            }
        }
    }

    public void updateViz ()
    {
        if (this.rndr == null)
            this.rndr = this.GetComponent<Renderer>();

        // Update material
        if (this.associatedPlayer == null)
            if (this.isGrowthTarget)
                this.rndr.material = GameManager.cur.growthTargetMat;
            else
                this.rndr.material = GameManager.cur.neutralTileMat;
        else
            this.rndr.material = this.associatedPlayer.playerMat;

        //// Update State indicator (inactive, new, and terminate)
        //if (!this.isActive || this.shouldTerminate || this.inCombat)
        //{
        //    this.indicator.SetActive(true);

        //    if (this.shouldTerminate || this.inCombat)
        //        this.indicator.GetComponent<Renderer>().material = this.terminationMat;
        //    else
        //        this.indicator.GetComponent<Renderer>().material = this.inactiveMat;
        //}
        //else
        //{
        //    this.indicator.SetActive(false);
        //}
    }

    public void getEnemyContacts (HashSet<Tile> workingSet)
    {
        Tile[] neighbors = this.getNeighbors();

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null && neighbors[i].associatedPlayer != null && neighbors[i].associatedPlayer != this.associatedPlayer)
                workingSet.Add(neighbors[i]);
        }
    }

    public override bool Equals(object other)
    {
        if (other is Tile)
            return this.gridLocation.Equals(((Tile)other).gridLocation);
        else
            return false;
    }

    public override int GetHashCode()
    {
        return this.gridLocation.GetHashCode();
    }
}
