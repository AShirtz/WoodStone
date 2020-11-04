using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class just holds the player's owned units/groups, turn timer, and action count.
/// </summary>
public class Player : MonoBehaviour
{
    public bool isInitialized = false;

    public int playerIndex = -1;

    public int actionsPerTurn = 0;
    public float turnTimerMax = 0f;

    public int actionsThisTurn = 0;
    public float curTurnTimerVal = 0f;

    public int totalPopulation = 0;
    public int groupWithHighestTriplesCount = 0;

    public float curScore = 0f;

    public Material playerMat = null;

    public List<Group> ownedGroups = null;

    public GameBoard brd = null;

    public Text TurnTimerUI = null;
    public Slider TurnTimerUISlider = null;
    public GameObject TurnTimerUISliderFill = null;

    public Text ActionsPerTurnUI = null;
    public Text WaitGoUIIndicator = null;

    public Text scoreIndicator = null;

    public bool shouldBlink = false;
    private bool isBlinking = false;
    private WaitForSeconds blinkWait = new WaitForSeconds(0.25f);

    public void Initialize (int index, GameBoard gmBrd)
    {
        this.isInitialized = true;

        this.playerIndex = index;
        this.brd = gmBrd;

        this.ownedGroups = new List<Group>();
    }

    private void Update()
    {
        if (this.shouldBlink && !this.isBlinking)
            this.StartCoroutine(this.blinkIndicator());

        if (this.turnTimerMax >= 10f)
            this.TurnTimerUI.text = string.Format("{0:00.0}/{1:00.0}", this.curTurnTimerVal, this.turnTimerMax);
        else
            this.TurnTimerUI.text = string.Format("{0:0.0}/{1:0.0}", this.curTurnTimerVal, this.turnTimerMax);

    }

    public void UpdatePlayer()
    {
        if (!this.isInitialized)
            return;

        // Update timer (if running)
        if (GameManager.cur != null && GameManager.cur.timersRunning)
        {
            this.curTurnTimerVal -= Time.deltaTime;

            // Kick off turn
            if (this.curTurnTimerVal <= 0f)
            {
                GameManager.cur.startTurn(this.playerIndex);

                this.actionsThisTurn = this.actionsPerTurn;

                // Update relevant UI elements
                this.WaitGoUIIndicator.text = "G O !!!";

                this.shouldBlink = true;

                this.TurnTimerUISlider.value = 1f;

                //if (this.turnTimerMax >= 10f)
                //    this.TurnTimerUI.text = string.Format("{0:00.0}/{1:00.0}", this.curTurnTimerVal, this.turnTimerMax);
                //else
                //    this.TurnTimerUI.text = string.Format("{0:0.0}/{1:0.0}", this.curTurnTimerVal, this.turnTimerMax);

                this.ActionsPerTurnUI.gameObject.SetActive(true);
                this.ActionsPerTurnUI.text = "Actions: " + this.actionsThisTurn.ToString();
            }
            else
            {
                // Update score and viz
                this.curScore += ((float)this.totalPopulation) * Time.deltaTime;
                this.scoreIndicator.text = string.Format("Score:{0:000}", Mathf.RoundToInt(this.curScore));

                // Update relevant UI elements
                this.TurnTimerUISlider.value = this.curTurnTimerVal / this.turnTimerMax;

                //if (this.turnTimerMax >= 10f)
                //    this.TurnTimerUI.text = string.Format("{0:00.0}/{1:00.0}", this.curTurnTimerVal, this.turnTimerMax);
                //else
                //    this.TurnTimerUI.text = string.Format("{0:0.0}/{1:0.0}", this.curTurnTimerVal, this.turnTimerMax);
            }
        }
        else if (GameManager.cur.curPlayerTurn == this.playerIndex)
        {
            this.ActionsPerTurnUI.text = "Actions:" + this.actionsThisTurn.ToString();
        }
    }

    /// <summary>
    /// Actions necessary to return current player to 'waiting' state.
    /// Must be called after the current state of the game is calculated.
    /// </summary>
    public void endTurn()
    {
        // Update relevant UI elements
        this.WaitGoUIIndicator.text = "W A I T";
        this.shouldBlink = false;

        if (this.turnTimerMax >= 10f)
            this.TurnTimerUI.text = string.Format("{0:00.0}/{1:00.0}", this.curTurnTimerVal, this.turnTimerMax);
        else
            this.TurnTimerUI.text = string.Format("{0:0.0}/{1:0.0}", this.curTurnTimerVal, this.turnTimerMax);

        this.ActionsPerTurnUI.gameObject.SetActive(false);

        // Reset timer value
        this.curTurnTimerVal = this.turnTimerMax;
    }

    public void Win(bool isWinner)
    {
        if (isWinner)
        {
            this.WaitGoUIIndicator.text = "W I N !!!";

            this.shouldBlink = true;
        }
        else
        {
            this.WaitGoUIIndicator.text = "L O S S";

            this.shouldBlink = false;
        }
    }

    private IEnumerator blinkIndicator ()
    {
        if (!this.isBlinking)
        {
            this.isBlinking = true;

            while (this.shouldBlink)
            {
                yield return blinkWait;

                this.WaitGoUIIndicator.gameObject.SetActive(!this.WaitGoUIIndicator.gameObject.activeSelf);
                this.TurnTimerUISliderFill.SetActive(!this.TurnTimerUISliderFill.activeSelf);
            }

            this.WaitGoUIIndicator.gameObject.SetActive(true);
            this.TurnTimerUISliderFill.SetActive(true);

            this.isBlinking = false;
        }
    }

    /// <summary>
    /// Calculates 'turn timer' and 'actions per turn' from ownedGroups
    /// </summary>
    public void CalculatePopulation()
    {
        // Calculate group and total population
        this.totalPopulation = 0;
        this.groupWithHighestTriplesCount = 0;

        foreach (Group g in this.ownedGroups)
        {
            //this.totalPopulation += g.tiles.Count;

            foreach (Tile t in g.tiles)
            {
                if (!t.shouldTerminate && !t.inCombat)
                    totalPopulation++;
            }

            if (g.numTriples > this.groupWithHighestTriplesCount)
                this.groupWithHighestTriplesCount = g.numTriples;
        }

        // Calculate turn timer and actions per turn
        this.turnTimerMax = GameManager.cur.turnTimerUnitCost * this.totalPopulation;



        //  Fibonacci by triple (Adjusted)
        int fibFctr = this.getFibonacci(this.groupWithHighestTriplesCount);

        if (fibFctr == 0 && this.groupWithHighestTriplesCount == 1)
            fibFctr = 1;

        this.actionsPerTurn = fibFctr + GameManager.cur.minActionsPerTurn;


        //  Fibonacci by triple
        //this.actionsPerTurn = this.getFibonacci(this.groupWithHighestTriplesCount) + GameManager.cur.minActionsPerTurn;


        //  Linear by triple
        //this.actionsPerTurn = Mathf.FloorToInt(this.groupWithHighestTriplesCount / GameManager.cur.actionsPerTurnPopulationCost) + GameManager.cur.minActionsPerTurn;


        //  Linear by unit (also change count above)
        //this.actionsPerTurn = Mathf.FloorToInt(this.largestGroupPopulation / GameManager.cur.actionsPerTurnPopulationCost) + GameManager.cur.minActionsPerTurn;
    }

    // Returns the index of the Fibonacci number lower than the given input
    private int getFibonacci (int i)
    {
        int counter = 0;

        // 'current' is [(toggle) ? (0) : (1)], 'previous' is [(toggle) ? (1) : (0)]
        bool toggle = true;
        int[] workspace = new int[2] { 1, 1 };

        while (workspace[(toggle) ? (0) : (1)] < i)
        {
            counter++;

            // Find next Fibonacci number 
            workspace[(toggle) ? (0) : (1)] += workspace[(toggle) ? (1) : (0)];

            toggle = !toggle;
        }

        return counter;
    }
}
