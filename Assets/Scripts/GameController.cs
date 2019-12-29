using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	public const int STAGE_INITIAL = 0;
	public const int STAGE_SETUP = 1;
	public const int STAGE_SETUP_OVER = 2;
	public const int STAGE_PLAY = 3;
	//public const int STAGE_PLAY_OVER = 4;
	public const int STAGE_ONE_LEFT = 5;
	public const int STAGE_FINAL_SCORE = 6;
	public const int STAGE_CONGRATULATIONS = 7;
	public const int STAGE_END = 8;
	public const int STAGE_RESET = 9;

	public const int TOTAL_CARD_COUNT = 16;
	public static int[] CARD_COUNT = { 0, 5, 2, 2, 2, 2, 1, 1, 1 };
	private CardController[] Cards;

	private int CurrentStage;
	private int CurrentPlayer;

	public int CurrentPlayerIndex {
		get { return CurrentPlayer; }
	}

	public bool RoundOver {
		get { return CurrentStage > STAGE_PLAY; }
	}

	public DeckController Deck;
	public DiscardController DiscardPile;

	public List<MoveData> TurnHistory;
	public PlayerController[] Players;
	public UIPlayerController HumanPlayer;
	public AIEvaluator PerceptorEvaluator;

	public CardController PrincessCard;
	public GameObject HeartPrefab;
	private int TotalHeartCount;

	// This checks whether everyone is ready for the next game move
	public bool EveryoneReady {
		get {
			// First check if the current player is busy
			if(Players[CurrentPlayer].IsBusy) {
				return false;
			}
			// If not, check the other players in reverse sequence
			for(int i = Players.Length - 1; i >= 0; i--) {
				if(Players[i].IsBusy) {
					return false;
				}
			}
			// If no one is busy, then everyone is ready
			return true;
		}
	}

	// Returns true if no card is currently moving
	public bool AllCardsDown {
		get { 
			foreach(CardController c in Cards) {
				if(c.isInMotion) {
					return false;
				}
			}
			return true;
		}
	}

	// Returns how many players are still active in the round
	private int ActivePlayers {
		get { 
			int result = 0;
			foreach(PlayerController p in Players) {
				result += (p.KnockedOut ? 0 : 1);
			}
			return result;
		}
	}

	// Returns true if the King and both Princes are already discarded
	public bool TheKingAndBothPrincesAreDiscarded {
		get {
			return (DiscardPile.CardCount[CardController.VALUE_KING] == CARD_COUNT[CardController.VALUE_KING] &&
			DiscardPile.CardCount[CardController.VALUE_PRINCE] == CARD_COUNT[CardController.VALUE_PRINCE]);
		}
	}

	// Use this for initialization
	void Start() {
		CurrentStage = STAGE_INITIAL;
		CurrentPlayer = (Debug.isDebugBuild ? 0 : UnityEngine.Random.Range(0, Players.Length));
		TotalHeartCount = 0;
		// This assumes that all of the cards are in the deck when the game loads
		Cards = Deck.GetComponentsInChildren<CardController>();
		Debug.Assert(Cards.Length == TOTAL_CARD_COUNT);
		TurnHistory = new List<MoveData>();
	}
	
	// Update is called once per frame
	void Update() {
		switch(CurrentStage) {
		case STAGE_INITIAL:
			StartCoroutine(roundSetup());
			CurrentStage = STAGE_SETUP;
			break;
		case STAGE_SETUP_OVER:
			StartCoroutine(playRound());
			CurrentStage = STAGE_PLAY;
			break;
		case STAGE_ONE_LEFT:
			StartCoroutine(congratulateWinnerByElimination());
			CurrentStage = STAGE_CONGRATULATIONS;
			break;
		case STAGE_FINAL_SCORE:
			StartCoroutine(tallyUpTheFinalScore());
			CurrentStage = STAGE_CONGRATULATIONS;
			break;
		case STAGE_END:
			StartCoroutine(resetRound());
			CurrentStage = STAGE_RESET;
			break;
		default:
			return;
		}
	}

	private IEnumerator roundSetup() {
		float waitBetweenDraws = 0.5f;
		if(PerceptorEvaluator != null && PerceptorEvaluator.isActiveAndEnabled) {
			PerceptorEvaluator.NextGameSeed();
		}
		// Shuffle the deck and wait until the deck is fully shuffled
		Deck.Shuffle();
		yield return new WaitUntil(() => (AllCardsDown));
		// Draw a card for each player, waiting after each one
		float waitUntil = Time.timeSinceLevelLoad + waitBetweenDraws;
		for(int p = 0; p < Players.Length; p++) {
			// Initialize player
			Players[p].Game = this;
			Players[p].SittingOrder = p;
			// Draw a card
			Players[p].DrawNewCard();
			yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil)); 
			waitUntil += waitBetweenDraws;
		}
		// Wait until every player has his card
		yield return new WaitUntil(() => (EveryoneReady));
		Debug.Log("Let the game begin!");
		CurrentStage = STAGE_SETUP_OVER;
	}

	private IEnumerator playRound() {
		// Wait until every player is ready, just in case
		yield return new WaitUntil(() => (EveryoneReady));
		// Loop through the players until either the deck runs out of cards (except the last one)
		// or there is only one player standing
		do {
			// Check if the next player is knocked out, if so, skip their turn
			PlayerController curPlayer = Players[CurrentPlayer];
			if(curPlayer.KnockedOut) {
				Debug.LogFormat("{0} is knocked out and skips this turn!", curPlayer);
			} else {
				// Let the player take their turn and wait for them to finish it
				Debug.LogFormat("{0} takes a turn...", curPlayer);
				curPlayer.NextTurn();
				yield return new WaitUntil(() => (EveryoneReady));
				if(PerceptorEvaluator != null && PerceptorEvaluator.isActiveAndEnabled) {
					PerceptorEvaluator.UpdateStatistics(Deck.GetCardDistribution(), Deck.CountCardsLeft);
				}
			}
			// Select the next player
			CurrentPlayer = (CurrentPlayer + 1) % Players.Length;
		} while(Deck.CountCardsLeft > 1 && ActivePlayers > 1);
		// Check the endgame condition
		if(ActivePlayers == 1) {
			CurrentStage = STAGE_ONE_LEFT;  
		} else if(Deck.CountCardsLeft <= 1) {
			CurrentStage = STAGE_FINAL_SCORE;
		} else {
			throw new ApplicationException("The gameplay loop has exited without a clear win condition!");
		}
	}

	private IEnumerator congratulateWinnerByElimination() {
		Debug.Assert(ActivePlayers == 1);
		// Wait until every player is ready, just in case
		yield return new WaitUntil(() => (EveryoneReady && AllCardsDown));
		// Find the winning player
		PlayerController winner = Players[0];
		foreach(PlayerController p in Players) {
			if(!p.KnockedOut) {
				winner = p;
				break;
			}
		}
		// Reveal the winner's hand
		CardController cc = winner.GetHand();
		cc.FlipUp();
		Debug.LogFormat("{0} had the {1}!", winner, cc);
		// Congratulate the winner
		Debug.LogFormat("{0} wins this round by elimination!", winner);
		StartCoroutine(bestowHeart(winner));
	}

	private IEnumerator tallyUpTheFinalScore() {
		Debug.Assert(Deck.CountCardsLeft <= 1);
		float waitBetweenCardFlips = 0.5f;
		// Wait until every player is ready, just in case
		yield return new WaitUntil(() => (EveryoneReady && AllCardsDown));
		// Flip all the cards that are still not flipped
		float waitUntil = Time.timeSinceLevelLoad;
		// First, check if there is still a card in the deck
		// (there should be, unless a Prince has been played in the ver last turn)
		CardController cc;
		if(Deck.CountCardsLeft > 0) {
			waitUntil += waitBetweenCardFlips;
			yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil));
			// Reveal the card
			cc = Deck.Draw();
			cc.FlipUp();
			Debug.LogFormat("The final card in the deck was the {0}!", cc);
		}
		// Then go through still-active players and find the winner
		int highestValue = 0, highestTotalDiscardedValue = 0;
		PlayerController winner = Players[0];
		foreach(PlayerController p in Players) {
			if(!p.KnockedOut) {
				waitUntil += waitBetweenCardFlips;
				yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil));
				// Reveal the hand
				cc = p.GetHand();
				cc.FlipUp();
				Debug.LogFormat("{0} had the {1}!", p, cc);
				// Update the winner
				if(cc.Value > highestValue || (cc.Value == highestValue && p.TotalDiscardedValue > highestTotalDiscardedValue)) {
					winner = p;
					highestValue = cc.Value;
					highestTotalDiscardedValue = p.TotalDiscardedValue;
				}
			}
		}
		// Congratulate the winner
		Debug.LogFormat("{0} wins this round with the {1}!", winner, winner.GetHand());
		StartCoroutine(bestowHeart(winner));
	}

	private IEnumerator bestowHeart(PlayerController winner) {
		Debug.Assert(PrincessCard != null);
		Debug.Assert(HeartPrefab != null);
		// Flip the Princess' card, if not yet flipped
		PrincessCard.FlipUp();
		// Spawn a new heart under the Princess' heart
		GameObject heart = Instantiate(Resources.Load("Prefabs/Heart", typeof(GameObject))) as GameObject;
		heart.name = "Heart";
		heart.transform.position = PrincessCard.transform.position;
		heart.GetComponent<SpriteRenderer>().sortingOrder = TotalHeartCount++;
		// Set the heart to move towards a random spot in the winner's heart container
		HeartController hc = heart.GetComponent<HeartController>();
		hc.transform.SetParent(winner.HeartContainer.transform);
		hc.TargetPosition = UnityEngine.Random.insideUnitCircle * 1.5f;
		yield return new WaitUntil(() => (hc.hasStopped));
		Debug.LogFormat("{0} received a heart from the Princess, congratulations!", winner);
		winner.HeartCount += 1;
		// The winner of the current round goes first in the next
		for(int i = 0; i < Players.Length; i++) {
			if(Players[i] == winner) {
				CurrentPlayer = i;
				break;
			}
		}
		CurrentStage = STAGE_END;
	}

	private IEnumerator resetRound() {
		if(PerceptorEvaluator != null && PerceptorEvaluator.isActiveAndEnabled) {
			yield return new WaitUntil(() => PerceptorEvaluator.READY);
		}
		// Reset all cards from the discard back into the deck
		foreach(CardController cc in DiscardPile.GetComponentsInChildren<CardController>()) {
			cc.MoveTo(Deck.transform);
		}
		// Reset all players
		foreach(PlayerController p in Players) {
			p.Reset();
		}
		// Reset the discard pile counters (the deck is reset when Shuffle() is called during STAGE_INITIAL)
		DiscardPile.Reset();
		TurnHistory.Clear();
		// Display play statistics, if any
		if(PosterioriPerceptor.StatisticOfPlay != null) {
			AIUtil.DisplayMatrix("Statistics of play", PosterioriPerceptor.StatisticOfPlay);
		}
		// Wait untill all cards are back in the deck before reseting the overall game state
		yield return new WaitUntil(() => (AllCardsDown));
		CurrentStage = STAGE_INITIAL;
	}
}
