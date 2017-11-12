using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PosterioriPerceptor : AIGenericPerceptor {

	public const int CARD_VECTOR_LENGTH = CardController.VALUE_PRINCESS + 1;

	public bool FullLogging;

	protected bool GameInitialized = false;
	protected bool HandInitialized = false;
	protected bool AnalysisOngoing = false;

	public override bool READY {
		get { return !AnalysisOngoing; } 
	}

	protected List<MoveData> TurnHistory;
	protected int NextTurnToAnalyze;

	protected float[][] HandDistribution;
	protected float[] DeckDistribution;
	protected int[] CountUnaccountedForCards;

	protected int PlayerCount;
	protected bool[] PlayerIsKnockedOut;
	protected int[] PlayerKnowsThatMyHandIs;
	protected int[] PlayerHasTargetedMe;
	protected int[] PlayerHasKnockOuts;

	protected PlayerController lastLearnedHandOf;
	protected CardController lastLearnedCard;

	public override bool SomeoneKnowsMyHand {
		get {
			for(int i = 0; i < PlayerCount; i++) {
				CardController mh = MyController.GetHand();
				if(i != MyController.SittingOrder && mh != null && PlayerKnowsThatMyHandIs[i] == mh.Value) {
					return true;
				}
			}
			return false;
		}
	}

	protected static bool PrecomputationsComplete = false;
	protected static float[] BaseDeckDistribution;

	// These values were obtained empirically and represent the likelihood of playing 
	// the card with the value equal to index1, while leaving a card with index2 in hand
	protected static float[,] LikelihoodOfPlay = {
		{ 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f }, 
		{ 0.000f, 1.000f, 0.100f, 0.995f, 0.990f, 0.950f, 0.990f, 0.990f, 1.000f }, 
		{ 0.000f, 0.900f, 1.000f, 0.990f, 0.990f, 0.900f, 0.900f, 0.900f, 1.000f }, 
		{ 0.000f, 0.005f, 0.010f, 1.000f, 0.850f, 0.850f, 0.900f, 0.990f, 1.000f }, 
		{ 0.000f, 0.010f, 0.010f, 0.150f, 1.000f, 0.005f, 0.650f, 0.800f, 1.000f }, 
		{ 0.000f, 0.050f, 0.100f, 0.150f, 0.995f, 1.000f, 0.750f, 0.000f, 1.000f }, 
		{ 0.000f, 0.010f, 0.100f, 0.100f, 0.350f, 0.250f, 0.000f, 0.000f, 1.000f }, 
		{ 0.000f, 0.010f, 0.100f, 0.010f, 0.200f, 1.000f, 1.000f, 0.000f, 1.000f }, 
		{ 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f }
	};

	// These can be used for re-estimating the LikelihoodOfPlay parameters
	protected static bool CollectStatisticsOfPlay = false;
	public static int[,] StatisticOfPlay;

	protected void Start() {
		if(!PrecomputationsComplete) {
			PrecomputationsComplete = true;
			// Precompute base deck distribution for later use
			BaseDeckDistribution = new float[CARD_VECTOR_LENGTH];
			for(int CardValue = CardController.VALUE_GUARD; CardValue <= CardController.VALUE_PRINCESS; CardValue++) {
				// Initialized base deck distribution
				BaseDeckDistribution[CardValue] = ((float)GameController.CARD_COUNT[CardValue]) / GameController.TOTAL_CARD_COUNT;
			}
			if(CollectStatisticsOfPlay) {
				StatisticOfPlay = new int[CARD_VECTOR_LENGTH, CARD_VECTOR_LENGTH];
			}
		}
	}

	protected void Update() {
		// Get the game parameters as soon as available
		if(!GameInitialized && MyController.Game != null) {
			InitializeMemory();
			ResetMemory();
		}

		// Check the turn history for new data, one at a time
		if(!AnalysisOngoing && TurnHistory != null && !MyController.KnockedOut && TurnHistory.Count > NextTurnToAnalyze) {
			AnalysisOngoing = true;
			StartCoroutine(AnalyzeTurn(NextTurnToAnalyze));
		}
	}

	// Initializes memory structures of the perceptor for later use
	// Only called once at game start, never for consecutive rounds
	protected virtual void InitializeMemory() {
		Debug.Assert(!GameInitialized);
		Debug.Assert(MyController.Game != null);
		// Set the turn history reference
		TurnHistory = MyController.Game.TurnHistory;
		// Initialized hand distributions
		PlayerCount = MyController.Game.Players.Length;
		HandDistribution = new float[PlayerCount][];
		for(int i = 0; i < PlayerCount; i++) {
			HandDistribution[i] = new float[CARD_VECTOR_LENGTH];
		}
		DeckDistribution = new float[CARD_VECTOR_LENGTH];
		CountUnaccountedForCards = new int[CARD_VECTOR_LENGTH];
		// Initialize other player's stats
		PlayerHasTargetedMe = new int[PlayerCount];
		PlayerHasKnockOuts = new int[PlayerCount];
		// Initialize other players' knowledge
		PlayerKnowsThatMyHandIs = new int[PlayerCount];
		PlayerIsKnockedOut = new bool[PlayerCount];
		GameInitialized = true;
	}

	// Resets the memory structures at the end of the round
	public override void ResetMemory() {
		if(!GameInitialized) {
			return;
		}
		Debug.Assert(!AnalysisOngoing);
		// Reset the next-to-analyze turn back to 0
		NextTurnToAnalyze = 0;
		// Set the initial card draw probabilities
		Array.Copy(BaseDeckDistribution, DeckDistribution, CARD_VECTOR_LENGTH);
		for(int p = 0; p < PlayerCount; p++) {
			Array.Copy(BaseDeckDistribution, HandDistribution[p], CARD_VECTOR_LENGTH);
		}
		Array.Copy(GameController.CARD_COUNT, CountUnaccountedForCards, CARD_VECTOR_LENGTH);
		HandInitialized = false;
		// Reset object pointers
		myHand = null;
		justDrawn = null;
		lastLearnedHandOf = null;
		lastLearnedCard = null;
		// Clear other players' knowledge states
		Array.Clear(PlayerKnowsThatMyHandIs, 0, PlayerCount);
		Array.Clear(PlayerIsKnockedOut, 0, PlayerCount);
	}

	// Evaluations show that, for some reason, the base deck distribution
	// is on average a more accurate estimation than using the filtered deck distribution
	// Nonetheless, the filtered deck distribution brings better results for hand filtering
	public override float GetCardProbabilityInDeck(int CardValue) {
		Debug.Assert(!AnalysisOngoing);
		Debug.Assert(DeckDistribution != null);
		Debug.Assert(CardValue >= 0 && CardValue <= CardController.VALUE_PRINCESS);
		return BaseDeckDistribution[CardValue];
	}

	public override float GetCardProbabilityInHand(PlayerController Player, int CardValue) {
		Debug.Assert(!AnalysisOngoing);
		Debug.Assert(HandDistribution != null);
		Debug.Assert(MyController.Game != null);
		Debug.Assert(Player != null);
		Debug.Assert(MyController.Game != null && Player.SittingOrder < PlayerCount);
		Debug.Assert(CardValue >= 0 && CardValue <= CardController.VALUE_PRINCESS);
		return HandDistribution[Player.SittingOrder][CardValue];
	}

	public override void RevealHand(PlayerController toPlayer) {
		Debug.Assert(!AnalysisOngoing);
		Debug.Assert(toPlayer != null && toPlayer.SittingOrder < PlayerCount);
		PlayerKnowsThatMyHandIs[toPlayer.SittingOrder] = MyController.GetHand().Value;
	}

	public override void LearnHand(PlayerController ofPlayer, CardController card) {
		Debug.Assert(!AnalysisOngoing);
		Debug.Assert(ofPlayer != null && ofPlayer.SittingOrder < PlayerCount);
		// Save data for filtering later
		lastLearnedHandOf = ofPlayer;
		lastLearnedCard = card;
	}

	public override int PlayerThinksMyHandIs(PlayerController p) {
		return PlayerKnowsThatMyHandIs[p.SittingOrder];
	}

	public override int HowOftenHasPlayerTargetedMe(PlayerController p) {
		return PlayerHasTargetedMe[p.SittingOrder];
	}

	public override int HowManyOthersHasPlayerKnockedOut(PlayerController p) {
		return PlayerHasKnockOuts[p.SittingOrder];
	}

	// This is the primary interface for subclasses
	protected abstract IEnumerator AnalyzeTurn(int id);

	// Attempts to strike the specified card from the unaccounted-for list
	// Throws exception if all cards of this type already lready accounted for
	protected void AccountForCard(int Value) {
		Debug.Assert(Value >= CardController.VALUE_GUARD && Value <= CardController.VALUE_PRINCESS);
		if(CountUnaccountedForCards[Value] > 0) {
			if(FullLogging) {
				Debug.LogFormat("{0} accounts for a {1}.", MyController, CardController.NAMES[Value]);
			}
			CountUnaccountedForCards[Value] -= 1;
		} else {
			throw new ArgumentOutOfRangeException(string.Format("{0} cannot account for {1}, because all cards of this value are already accounted for!", MyController, CardController.NAMES[Value]));
		}
	}

	// Filters the hand distribution of another player using the knowlege of the card they've just played
	protected void FilterHiddenHandWithPlayedCard(int PlayerIndex, int PlayedCardValue) {
		// Calculate the likelihoods that the player immediately played the card they just drew,
		// or that they played the current hand from their hand
		float probPlayFromDECK = 0, probPlayFromHAND = 0;
		for(int otherCardValue = CardController.VALUE_GUARD; otherCardValue <= CardController.VALUE_PRINCESS; otherCardValue++) {
			probPlayFromDECK += LikelihoodOfPlay[PlayedCardValue, otherCardValue] * HandDistribution[PlayerIndex][otherCardValue];
			probPlayFromHAND += LikelihoodOfPlay[PlayedCardValue, otherCardValue] * DeckDistribution[otherCardValue];
		}
		// Multiply each likelihood by the probability of each the played card being, respectively, in the deck, or in the hand
		// It's OK if they don't sum up to 1: we will renormalize everything together at the end
		probPlayFromDECK *= DeckDistribution[PlayedCardValue];
		probPlayFromHAND *= HandDistribution[PlayerIndex][PlayedCardValue];
		// Perform a linear combination of every possible value 
		for(int c2 = CardController.VALUE_GUARD; c2 <= CardController.VALUE_PRINCESS; c2++) {
			HandDistribution[PlayerIndex][c2] = probPlayFromDECK * HandDistribution[PlayerIndex][c2] + probPlayFromHAND * DeckDistribution[c2];
		}
		// Renormalize the array if necessary
		RenormalizeCardDistribution(ref HandDistribution[PlayerIndex], PlayerIndex);
	}

	// If we know for sure that a hidden hand does not contain a certain card value,
	// we set that value's probability to 0 and renormalize the hand distribution
	protected void HiddenHandIsNot(int PlayerIndex, int CardValue) {
		HandDistribution[PlayerIndex][CardValue] = 0;
		RenormalizeCardDistribution(ref HandDistribution[PlayerIndex], PlayerIndex);
	}

	// If the Baron has knocked a player out, it gives us a lot of information about the winner's hand
	protected virtual void BaronEffectFilterWithKnockout(int winnerIndex, int loserIndex, int LosersHandValue) {
		// We now know that the winning player cannot have a card in hand lower than what the loser has discarded
		for(int winningHand = CardController.VALUE_GUARD; winningHand <= LosersHandValue; winningHand++) {
			// So we set the probabilities of these lower cards in their hand to zero
			HandDistribution[winnerIndex][winningHand] = 0;
		}
		RenormalizeCardDistribution(ref HandDistribution[winnerIndex], winnerIndex);
		// We don't care about the loser here because they will be knocked out, anyway
	}

	protected virtual void BaronEffectFilterWithDraw(int playerIndex, int targetIndex) {
		// Update the player's hand distribution
		for(int c = CardController.VALUE_GUARD; c <= CardController.VALUE_PRINCESS; c++) {
			HandDistribution[playerIndex][c] *= HandDistribution[targetIndex][c];
		}
		// Renormalize it
		RenormalizeCardDistribution(ref HandDistribution[playerIndex], playerIndex);
		// Copy it over the target's distribution
		Array.Copy(HandDistribution[playerIndex], HandDistribution[targetIndex], CARD_VECTOR_LENGTH);
	}

	// Swaps the hand distributions of two players
	protected void SwapHandDistributions(int FirstPlayerIndex, int SecondPlayerIndex) {
		float[] temp = new float[CARD_VECTOR_LENGTH];
		Array.Copy(HandDistribution[FirstPlayerIndex], temp, CARD_VECTOR_LENGTH);
		Array.Copy(HandDistribution[SecondPlayerIndex], HandDistribution[FirstPlayerIndex], CARD_VECTOR_LENGTH);
		Array.Copy(temp, HandDistribution[SecondPlayerIndex], CARD_VECTOR_LENGTH);
	}

	protected void UpdateHandDistributionWithCertainty(int PlayerIndex, int HandValue) {
		Array.Clear(HandDistribution[PlayerIndex], 0, CARD_VECTOR_LENGTH);
		HandDistribution[PlayerIndex][HandValue] = 1f;
	}

	protected void UpdateOwnHandDistribution() {
		if(myHand != null) {
			UpdateHandDistributionWithCertainty(MyController.SittingOrder, myHand.Value);
		} else if(justDrawn != null) {			
			UpdateHandDistributionWithCertainty(MyController.SittingOrder, justDrawn.Value);
		} else {
			Debug.LogErrorFormat("{0}'s perceptor wants to update his own hand distribution, but his hand is empty!", MyController);
		}
	}

	// Normalize the new winner hand distribution, resetting it to the default distribution if it sums up to zero
	protected void RenormalizeCardDistribution(ref float[] Distr, int HandOwnerIndex = -1) {
		if(AIUtil.NormalizeProbabilitiesArray(ref Distr) <= 0) {
			if(HandOwnerIndex >= 0) {
				if(PlayerIsKnockedOut[HandOwnerIndex]) {
					return;
				}
				Debug.LogWarningFormat("{0}'s beliefs about {1}'s hand sum up to 0! Resetting to default...", MyController, MyController.Game.Players[HandOwnerIndex]);
			} else {
				if(MyController.Game.Deck.CountCardsLeft == 0) {
					return;
				}
				Debug.LogWarningFormat("{0}'s beliefs about deck contents sum up to 0! Resetting to default...", MyController);
			}
			Array.Copy(BaseDeckDistribution, Distr, CARD_VECTOR_LENGTH);
		}
	}

	protected void displayCurrentBeliefs() {
		string result = MyController.ToString() + "'s beliefs:\n", formatStr = "{0}: [Grd: {1:P}; Prst: {2:P}; Brn: {3:P}; Hmd: {4:P}; Prc: {5:P}; Kng: {6:P}; Cnts: {7:P}; Prcs: {8:P}]\n";
		for(int i = 0; i < PlayerCount; i++) {
			result += string.Format(formatStr, MyController.Game.Players[i],
				HandDistribution[i][1], HandDistribution[i][2], HandDistribution[i][3], HandDistribution[i][4],
				HandDistribution[i][5], HandDistribution[i][6], HandDistribution[i][7], HandDistribution[i][8]);
		}
		result += string.Format(formatStr, "DECK",
			DeckDistribution[1], DeckDistribution[2], DeckDistribution[3], DeckDistribution[4],
			DeckDistribution[5], DeckDistribution[6], DeckDistribution[7], DeckDistribution[8]);
		Debug.Log(result);
	}

	protected void UpdatePlayStatistics(MoveData Turn) {
		if(CollectStatisticsOfPlay) {
			if(Turn.Player == MyController) {
				if(Turn.Card.Value == CardController.VALUE_KING && !Turn.NoEffect) {
					StatisticOfPlay[Turn.Card.Value, MyHandBeforeKing.Value]++;
				} else if(Turn.Target == MyController && Turn.Card.Value == CardController.VALUE_PRINCE) {
					StatisticOfPlay[Turn.Card.Value, Turn.AdditionalDiscard.Value]++;
				} else {
					StatisticOfPlay[Turn.Card.Value, myHand.Value]++;
				}
			}
		}
	}
}
