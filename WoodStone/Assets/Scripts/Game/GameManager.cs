using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;
    public static GameManager cur { get { return instance; } }

    //public enum Rules
    //{
    //    Classic,
    //    Movement,
    //    LimitedDistance
    //}

    /*
     *      PARAMS
     */

    [Header("Parameters")]
    public int numPlayers = 2;

    [Tooltip("How much each unit 'costs' on the turn timer. (More units => slower to act)")]
    public float turnTimerUnitCost = 0.1f;

    [Tooltip("Minimum number of actions per turn, before exponential requirement.")]
    public int minActionsPerTurn = 2;

    [Tooltip("Actions per turn gained when largest group population hits another multiple of this number.")]
    public int actionsPerTurnPopulationCost = 1;

    [Tooltip("Distance a unit can grow times group population. Only used for the 'Limited Distance' ruleset.")]
    public int groupPopulationDistanceFactor = 2;

    [Tooltip("Time limit for the entire game. Only counts time not during a player's turn.")]
    public float totalGameTime = 120f;

    [Tooltip("The time used to count up the attack power for a group in conflict (divided by number of units to count)")]
    public float attackPowerCountUpTime = 2f;

    [Tooltip("The time used to ripple into an enemy group (divided by the power the attack)")]
    public float attackRippleTime = 3f;

    //public Rules curRules = Rules.Classic;

    public Material neutralTileMat = null;
    public Material growthTargetMat = null;
    public Material[] playerMats = null;

    public AudioSource turnChime = null;

    public WoodblockAudioController woodblockSFX = null;

    /*
     *      UI ELEMENTS
     */

    [Header("UI Wiring")]
    public GameObject playIndicator = null;
    public GameObject pauseIndicator = null;

    public Text gameTimer = null;

    public Text[] PlayerAttackCounters = null;

    /*
     *      INSTANCE MEMBERS
     */

    [Header("Instance Members")]
    public bool timersRunning = false;

    public float curGameTime = 0f;

    public GameBoard curBrd = null;
    public Player[] players = null;

    public int curPlayerTurn = -1;                                  // -1 => no player's turn, also used for initial setup

    public bool growEventStarted = false;                           // Used to indicate that a 'click-drag' event has started
    public Vector2Int growEventOrigination = Vector2Int.zero;       // This is the grid index where the 'click-drag' even started
    public HashSet<Tile> growEventValidTiles = null;                // Contains the valid tiles that can be 'grown onto' by the originating tile

    public bool gameBoardReady = false;
    private WaitForEndOfFrame eofWait = null;

    void Start()
    {
        instance = this;
        this.eofWait = new WaitForEndOfFrame();

        // TODO: Menu? Start Game button? Single vs. Multiplayer?

        this.StartCoroutine(this.Initialize());
    }

    private void Update()
    {
        // TODO: Escape menu?
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    Application.Quit();
        //}

        // Players were diverging in score (sometimes a player would act before the GameManager and would get an Update)
        for (int i = 0; i < this.players.Length; i++)
            this.players[i].UpdatePlayer();

        // Handle Input
        if (this.gameBoardReady)
        {
            this.StartCoroutine(this.HandleInput());
        }

        // Update game timer and UI
        if (this.timersRunning)
        {
            this.curGameTime += Time.deltaTime;
            this.gameTimer.text = string.Format("{0:000.0}/{1:000.0}", this.curGameTime, this.totalGameTime);
        }

        // Update UI to indicate which player won
        if (this.curGameTime >= this.totalGameTime)
        {
            // Figure out which player has a higher score
            float maxScore = 0f;
            int winningPlayerIndex = -1;

            for (int i = 0; i < this.players.Length; i++)
            {
                if (this.players[i].curScore > maxScore)
                {
                    maxScore = this.players[i].curScore;
                    winningPlayerIndex = i;
                }
            }

            // Update UI to indicate which player won
            for (int i = 0; i < this.players.Length; i++)
            {
                if (i == winningPlayerIndex)
                    this.players[i].Win(true);
                else
                    this.players[i].Win(false);
            }

            this.timersRunning = false;
        }

        // Check to see if the player has clicked the turn button
        if (Input.GetMouseButtonDown(0) && this.gameBoardReady)
        {
            RaycastHit ht;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out ht))
            {
                if (ht.collider.tag == "TurnButton")
                {
                    this.timersRunning = !this.timersRunning;
                }
            }
        }

        // Update Play/Pause Indicators
        if (this.timersRunning)
        {
            this.playIndicator.SetActive(false);
            this.pauseIndicator.SetActive(true);
        }
        else
        {
            this.playIndicator.SetActive(true);
            this.pauseIndicator.SetActive(false);
        }
    }

    private IEnumerator HandleInput()
    {
        // Handle Mouse Input
        if (this.curPlayerTurn != -1)
        {
            if (!this.growEventStarted)
            {
                // Listen for either a right click (termination) or left click (growth)
                if (Input.GetMouseButtonDown(1))
                {
                    // Check the clicked unit and toggle termination marker
                    RaycastHit ht;

                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out ht))
                    {
                        // Toggle termination state of clicked unit
                        if (ht.collider != null && ht.collider.GetComponent<Tile>() != null && ht.collider.GetComponent<Tile>().associatedPlayer != null && ht.collider.GetComponent<Tile>().associatedPlayer.playerIndex == this.curPlayerTurn)
                        {
                            Tile tle = ht.collider.GetComponent<Tile>();

                            if (tle.isNew && tle.generatedTile == null)
                            {
                                // Give back an action to the player
                                this.players[this.curPlayerTurn].actionsThisTurn++;

                                // If the tile was marked for termination, give the player another action (possible in limited distance ruleset)
                                if (tle.shouldTerminate)
                                    this.players[this.curPlayerTurn].actionsThisTurn++;

                                // Remove tile from collection of live tiles
                                this.curBrd.liveTiles.Remove(tle);

                                // Reactivate the originating tile
                                if (tle.originatingTile != null)
                                {
                                    tle.originatingTile.isActive = true;
                                    tle.originatingTile.generatedTile = null;

                                    tle.originatingTile = null;
                                }

                                // Reset the new tile
                                tle.FullReset();
                            }
                            else
                            {
                                if (!tle.shouldTerminate)
                                {
                                    if (this.players[this.curPlayerTurn].actionsThisTurn > 0)
                                    {
                                        // Decrement player actions
                                        --this.players[this.curPlayerTurn].actionsThisTurn;

                                        // Mark tile for termination
                                        tle.shouldTerminate = true;
                                    }
                                }
                                else
                                {
                                    // Increment player actions
                                    ++this.players[this.curPlayerTurn].actionsThisTurn;

                                    // Mark tile for termination
                                    tle.shouldTerminate = false;
                                }
                            }

                            yield return StartCoroutine(CalculateState());
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    // Check to see if the clicked unit is a valid starting point
                    RaycastHit ht;

                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out ht))
                    {
                        if (ht.collider.tag == "TurnButton")
                        {
                            // Player has clicked the turn button
                            if (this.curPlayerTurn != -1)
                            {
                                // End turn
                                this.endTurn();
                            }
                        }
                        else if (ht.collider != null && ht.collider.gameObject != null)
                        {
                            // Player has clicked a tile

                            Tile tle = ht.collider.GetComponent<Tile>();

                            if (tle != null && tle.associatedPlayer.playerIndex == this.curPlayerTurn && tle.isActive && !tle.inCombat && this.players[this.curPlayerTurn].actionsThisTurn > 0)
                            {
                                this.growEventValidTiles = tle.getGrowTargets();

                                if (this.growEventValidTiles.Count > 0)
                                {
                                    this.growEventStarted = true;
                                    this.growEventOrigination = tle.gridLocation;

                                    // Highlight valid growth targets
                                    foreach (Tile t in this.growEventValidTiles)
                                    {
                                        t.isGrowthTarget = true;
                                        t.updateViz();
                                    }
                                }
                                else
                                {
                                    this.growEventValidTiles = null;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Listen for left mouse button release
                if (Input.GetMouseButtonUp(0))
                {
                    // Check if the selected tile is a valid growth target
                    RaycastHit ht;

                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out ht))
                    {
                        Tile tle = ht.collider.GetComponent<Tile>();

                        if (tle != null && this.growEventValidTiles != null && this.growEventValidTiles.Contains(tle))
                        {
                            // Selected tile is valid, complete the growth event
                            tle.associatedPlayer = this.players[this.curPlayerTurn];
                            tle.originatingTile = this.curBrd.getTile(this.growEventOrigination);
                            tle.originatingTile.generatedTile = tle;

                            tle.isNew = true;

                            // Decrement player actions
                            this.players[this.curPlayerTurn].actionsThisTurn--;

                            // Record new tile as live
                            this.curBrd.liveTiles.Add(tle);

                            // Set originating tile as inactive
                            tle.originatingTile.isActive = false;

                            yield return StartCoroutine(CalculateState());
                        }
                    }

                    // Reset growth event variables
                    this.growEventStarted = false;
                    this.growEventOrigination = Vector2Int.zero;

                    foreach (Tile t in this.growEventValidTiles)
                    {
                        t.isGrowthTarget = false;
                        t.updateViz();
                    }

                    this.growEventValidTiles = null;
                }
            }
        }
    }

    private IEnumerator Initialize ()
    {
        // Check parameters for correctness
        if (curBrd == null)
            Debug.LogError("No Gameboard Assigned");

        if (numPlayers < 2)
            Debug.LogError("At least two players required");

        if (playerMats.Length != numPlayers)
            Debug.LogError("Player Materials length doesn't match number of players");

        // Initialize players
        for (int i = 0; i < this.players.Length; i++)
        {
            this.players[i].Initialize(i, this.curBrd);
        }

        // TODO: Which map should be used?
        // Setup Gameboard
        this.curBrd.Initialize(Maps.mapAWidth, Maps.mapAHeight, Maps.mapA);

        // Calculate initial state of the board
        yield return this.StartCoroutine(this.CalculateState());
    }

    private IEnumerator CalculateState()
    {
        this.gameBoardReady = false;

        // Find Groups
        HashSet<Group> groups = this.curBrd.FindGroups();

        yield return eofWait;

        // If this isn't initial setup, handle conflict
        if (this.curPlayerTurn != -1)
        {
            // Mark all tiles as not in combat, those in combat will be marked later
            foreach (Group g in groups)
                foreach (Tile t in g.tiles)
                    t.inCombat = false;

            // Find groups that are in conflict
            foreach (Group g in groups)
            {
                if (g.associatedPlayer.playerIndex == this.curPlayerTurn && g.isInCombat())
                {
                    // Calculate Attack Power
                    int attackPower = 0;

                    // Mark player's units as in combat
                    foreach (Tile t in g.tiles)
                    {
                        if (!t.shouldTerminate)
                        {
                            attackPower++;
                            t.inCombat = true;
                        }
                    }

                    yield return eofWait;

                    // Collect all enemy units that will need to be terminated (Ordered by distance from point of contact)
                    HashSet<Tile>[] tilesToDestroy = new HashSet<Tile>[attackPower];
                    HashSet<Tile> tilesAlreadyConsidered = new HashSet<Tile>();

                    // Seed 'ripple' with the points of contact
                    tilesToDestroy[0] = new HashSet<Tile>(g.pointsOfContactEnemy);
                    tilesAlreadyConsidered.UnionWith(tilesToDestroy[0]);

                    // 'Ripple' into enemy units
                    for (int i = 1; i < attackPower; i++)
                    {
                        if (tilesToDestroy[i - 1] != null)
                        {
                            foreach (Tile t in tilesToDestroy[i - 1])
                            {
                                Tile[] neighbors = t.getNeighbors();

                                for (int j = 0; j < neighbors.Length; j++)
                                {
                                    if (neighbors[j] != null && neighbors[j].associatedPlayer == t.associatedPlayer && !tilesAlreadyConsidered.Contains(neighbors[j]))
                                    {
                                        if (tilesToDestroy[i] == null)
                                            tilesToDestroy[i] = new HashSet<Tile>();

                                        tilesToDestroy[i].Add(neighbors[j]);
                                        tilesAlreadyConsidered.Add(neighbors[j]);
                                    }
                                }
                            }
                        }
                    }

                    yield return eofWait;

                    // Mark enemy units as in combat
                    for (int i = 0; i < attackPower; i++)
                    {
                        if (tilesToDestroy[i] != null)
                        {
                            foreach (Tile t in tilesToDestroy[i])
                            {
                                t.inCombat = true;
                            }
                        }
                    }
                }
            }

            yield return eofWait;

            // Find new Groups
            groups = this.curBrd.FindGroups();
        }

        yield return eofWait;

        // Clear player data
        for (int i = 0; i < this.players.Length; i++)
            this.players[i].ownedGroups.Clear();

        // Associate groups/units with players
        foreach (Group g in groups)
            g.associatedPlayer.ownedGroups.Add(g);

        // Calculate population based variables
        for (int i = 0; i < this.players.Length; i++)
            this.players[i].CalculatePopulation();

        yield return eofWait;

        // Update viz of all tiles
        for (int x = 0; x < this.curBrd.Width; x++)
        {
            for (int y = 0; y < this.curBrd.Height; y++)
            {
                Tile t = this.curBrd.tiles[x, y];

                if (t != null)
                {
                    t.updateViz();
                }
            }
        }

        this.gameBoardReady = true;
    }

    public void startTurn (int playerIndex)
    {
        this.timersRunning = false;
        this.curPlayerTurn = playerIndex;

        // Play Turn Chime SFX
        this.turnChime.Play();
    }

    public void endTurn ()
    {
        if (this.curPlayerTurn != -1 && this.gameBoardReady)
            this.StartCoroutine(this.endTurnRoutine());
    }

    /// <summary>
    /// This function is to be called at the end of each turn to handle combat and calculate population based 
    /// </summary>
    private IEnumerator endTurnRoutine()
    {
        this.gameBoardReady = false;

        WaitForSeconds delayWait = new WaitForSeconds(this.attackRippleTime);

        // Terminate tiles marked for termination
        HashSet<Tile> deadTiles = new HashSet<Tile>();

        foreach (Tile tle in this.curBrd.liveTiles)
        {
            if (tle.shouldTerminate)
            {
                tle.FullReset();

                deadTiles.Add(tle);
            }
        }

        yield return eofWait;

        // Remove dead tiles from liveTiles list
        this.curBrd.liveTiles.RemoveWhere(tle => deadTiles.Contains(tle));

        if (deadTiles.Count > 0)
        {
            // Just resets the audio script so it always goes 'tick-tock'
            this.woodblockSFX.reset();

            // Play woodblock SFX
            this.woodblockSFX.playSFX();

            yield return delayWait;
        }

        yield return eofWait;

        // Find Groups
        HashSet<Group> groups = this.curBrd.FindGroups();

        yield return eofWait;

        // If this isn't initial setup, handle conflict
        if (this.curPlayerTurn != -1)
        {
            // Find those attacking player's groups that are in conflict (and associated points of contact)
            foreach (Group g in groups)
            {
                if (g.associatedPlayer.playerIndex == this.curPlayerTurn && g.isInCombat())
                {
                    // Calculate Attack Power
                    int attackPower = g.tiles.Count;
                    int attackPowerUI = 0;

                    /*
                     *              FRIENDLY UNITS
                     */

                    // Show units being counted up
                    delayWait = new WaitForSeconds(this.attackPowerCountUpTime);

                    // Enable Attack Counter UI element if it is disabled
                    this.PlayerAttackCounters[this.curPlayerTurn].gameObject.SetActive(true);

                    // Collect all friendly units that will need to be terminated (Ordered by distance from point of contact)
                    HashSet<Tile>[] tilesToDestroy = new HashSet<Tile>[attackPower];
                    HashSet<Tile> tilesAlreadyConsidered = new HashSet<Tile>();

                    // Seed 'ripple' with the points of contact
                    tilesToDestroy[0] = new HashSet<Tile>(g.pointsOfContactFriendly);
                    tilesAlreadyConsidered.UnionWith(tilesToDestroy[0]);

                    // 'Ripple' into friendly units
                    for (int i = 1; i < attackPower; i++)
                    {
                        if (tilesToDestroy[i - 1] != null)
                        {
                            foreach (Tile t in tilesToDestroy[i - 1])
                            {
                                Tile[] neighbors = t.getNeighbors();

                                for (int j = 0; j < neighbors.Length; j++)
                                {
                                    if (neighbors[j] != null && neighbors[j].associatedPlayer == t.associatedPlayer && !tilesAlreadyConsidered.Contains(neighbors[j]))
                                    {
                                        if (tilesToDestroy[i] == null)
                                            tilesToDestroy[i] = new HashSet<Tile>();

                                        tilesToDestroy[i].Add(neighbors[j]);
                                        tilesAlreadyConsidered.Add(neighbors[j]);
                                    }
                                }
                            }
                        }
                    }

                    // Just resets the audio script so it always goes 'tick-tock'
                    this.woodblockSFX.reset();

                    // Show friendly units being 'used up' as a ripple outward from the points of contact
                    for (int i = attackPower - 1; i >= 0; i--)
                    {
                        if (tilesToDestroy[i] != null && tilesToDestroy[i].Count > 0)
                        {
                            foreach (Tile t in tilesToDestroy[i])
                            {
                                // Increment Attack Counter UI Element
                                this.PlayerAttackCounters[this.curPlayerTurn].text = "AtkPow" + Environment.NewLine + (++attackPowerUI).ToString();

                                t.FullReset();

                                this.curBrd.liveTiles.Remove(t);

                                // Play woodblock SFX
                                this.woodblockSFX.playSFX();

                                yield return delayWait;
                            }
                        }
                    }

                    /*
                     *              ENEMY UNITS
                     */

                    // Collect all enemy units that will need to be terminated (Ordered by distance from point of contact)
                    tilesToDestroy = new HashSet<Tile>[attackPower];
                    tilesAlreadyConsidered.Clear();

                    // Seed 'ripple' with the points of contact
                    tilesToDestroy[0] = new HashSet<Tile>(g.pointsOfContactEnemy);
                    tilesAlreadyConsidered.UnionWith(tilesToDestroy[0]);

                    // 'Ripple' into enemy units
                    for (int i = 1; i < attackPower; i++)
                    {
                        if (tilesToDestroy[i - 1] != null)
                        {
                            foreach (Tile t in tilesToDestroy[i - 1])
                            {
                                if (t != null)
                                {
                                    Tile[] neighbors = t.getNeighbors();

                                    for (int j = 0; j < neighbors.Length; j++)
                                    {
                                        if (neighbors[j] != null && neighbors[j].associatedPlayer == t.associatedPlayer && !tilesAlreadyConsidered.Contains(neighbors[j]))
                                        {
                                            if (tilesToDestroy[i] == null)
                                                tilesToDestroy[i] = new HashSet<Tile>();

                                            tilesToDestroy[i].Add(neighbors[j]);
                                            tilesAlreadyConsidered.Add(neighbors[j]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    yield return eofWait;

                    delayWait = new WaitForSeconds(this.attackRippleTime);

                    // Show termination of enemy units as a ripple outward from the points of contact
                    for (int i = 0; i < attackPower; i++)
                    {
                        // Decrement Attack Counter UI Element
                        this.PlayerAttackCounters[this.curPlayerTurn].text = "AtkPow" + Environment.NewLine + (--attackPowerUI).ToString();

                        if (tilesToDestroy[i] != null)
                        {
                            foreach (Tile t in tilesToDestroy[i])
                            {
                                t.FullReset();

                                this.curBrd.liveTiles.Remove(t);
                            }
                        }

                        // Play woodblock SFX
                        this.woodblockSFX.playSFX();

                        yield return delayWait;
                    }
                }
            }

            // Disable the Attack Counter UI element
            this.PlayerAttackCounters[this.curPlayerTurn].gameObject.SetActive(false);

            // Find new Groups
            groups = this.curBrd.FindGroups();
        }

        yield return eofWait;

        // Clear player data
        for (int i = 0; i < this.players.Length; i++)
            this.players[i].ownedGroups.Clear();

        // Associate groups/units with players
        foreach (Group g in groups)
            g.associatedPlayer.ownedGroups.Add(g);

        // Calculate population based variables
        for (int i = 0; i < this.players.Length; i++)
            this.players[i].CalculatePopulation();

        yield return eofWait;

        // Reset tile state (active and new)
        for (int x = 0; x < this.curBrd.Width; x++)
        {
            for (int y = 0; y < this.curBrd.Height; y++)
            {
                Tile t = this.curBrd.tiles[x, y];

                if (t != null)
                {
                    t.isActive = true;
                    t.isNew = false;

                    t.generatedTile = null;
                    t.originatingTile = null;

                    t.updateViz();
                }
            }
        }

        // Check to see if either player has no units left (winner)
        Player winner = this.curBrd.getWinner();

        if (winner != null)
        {
            for (int i = 0; i < this.players.Length; i++)
            {
                if (i == winner.playerIndex)
                    this.players[i].Win(true);
                else
                    this.players[i].Win(false);
            }

            this.timersRunning = false;
        }
        else
        {
            this.players[this.curPlayerTurn].endTurn();

            this.timersRunning = true;
            this.curPlayerTurn = -1;

            this.gameBoardReady = true;
        }
    }
}
