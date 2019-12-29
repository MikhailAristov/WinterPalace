using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIGenericPerceptor : MonoBehaviour {

	public const float CERTAINTY_THRESHOLD = 0.7f;

	public abstract bool READY { get; }

	public AIPlayerController MyController;
	public bool HumanPlayerHasActed {
		get { return (MyController.Game != null && MyController.Game.HumanPlayer != null && MyController.Game.HumanPlayer.TotalDiscardedValue > 0); }
	}

	public int RemainingDeckSize {
		get {
			if(MyController != null) {
				return MyController.Game.Deck.CountCardsLeft;
			} else {
				return GameController.TOTAL_CARD_COUNT;
			} 
		}
	}

	protected CardController myHand;
	protected CardController justDrawn;

	// The reference to the last card I had in hand before it was swapped out 
	// with another player's by playing a King must be preserved for posteriori 
	// perceptors, or else this information would be completely lost by the time they analyzed it!
	protected CardController MyHandBeforeKing;
	// Ditto for the Prince
	protected CardController DrawnBecauseOfThePrince;

	public abstract bool SomeoneKnowsMyHand { get; }

	public abstract float GetCardProbabilityInDeck(int CardValue);

	public float[] GetCardProbabilitiesInDeck() {
		float[] result = new float[CardController.VALUE_PRINCESS + 1];
		float sum = 0;
		for(int i = CardController.VALUE_GUARD; i <= CardController.VALUE_PRINCESS; i++) {
			result[i] = GetCardProbabilityInDeck(i);
			sum += result[i];
		}
		Debug.AssertFormat(sum <= 0 || AIUtil.Approx(sum, 1f) || MyController.Game.Deck.CountCardsLeft < 1, "{0}: Deck card probabilities don't sum up to 1! ({1})", MyController, sum);
		return result;
	}

	public abstract float GetCardProbabilityInHand(PlayerController Player, int CardValue);

	public float[] GetCardProbabilitiesInHand(PlayerController Player) {
		float[] result = new float[CardController.VALUE_PRINCESS + 1];
		// If no player is specified (when called for a non-targeted card), just return an empty array
		if(Player != null) {
			float sum = 0;
			for(int i = CardController.VALUE_GUARD; i <= CardController.VALUE_PRINCESS; i++) {
				result[i] = GetCardProbabilityInHand(Player, i);
				sum += result[i];
			}
			Debug.AssertFormat(sum <= 0 || AIUtil.Approx(sum, 1f), "{0}: Hand card probabilities of {2} don't sum up to 1! ({1})", MyController, sum, Player);
		}
		return result;
	}

	public virtual void UpdateOwnHand(CardController NewHand, CardController NewDraw) {
		// A Prince replaces the old hand with a new draw
		if(myHand != null && justDrawn == null && NewHand == null && NewDraw != null) {
			DrawnBecauseOfThePrince = NewDraw;
		}
		// A Kind directly replaces the hand with a different card, bypassing the just-drawn slots
		if(myHand != NewHand && justDrawn == null && NewDraw == null) {
			MyHandBeforeKing = myHand;
		}
		// Update the hand
		myHand = NewHand;
		justDrawn = NewDraw;
	}

	public abstract void ResetMemory();

	public abstract void RevealHand(PlayerController toPlayer);

	public abstract void LearnHand(PlayerController ofPlayer, CardController card);

	public virtual int PlayerThinksMyHandIs(PlayerController ofPlayer) {
		return 0;
	}

	public virtual int HowOftenHasPlayerTargetedMe(PlayerController p) {
		return 0;
	}

	public virtual int HowManyOthersHasPlayerKnockedOut(PlayerController p) {
		return 0;
	}

	public float GetExpectedDeckValue() {
		float result = 0;
		for(int c = CardController.VALUE_GUARD; c <= CardController.VALUE_PRINCESS; c++) {
			result += GetCardProbabilityInDeck(c) * c;
		}
		return result;
	}

	public float GetExpectedHandValue(PlayerController p) {
		float result = 0;
		for(int c = CardController.VALUE_GUARD; c <= CardController.VALUE_PRINCESS; c++) {
			result += GetCardProbabilityInHand(p, c) * c;
		}
		return result;
	}

	public int GetMostLikelyHandValue(PlayerController p) {
		int result = 0;
		float highestLikelihood = float.MinValue, curLikelihood = 0;
		for(int c = CardController.VALUE_GUARD; c <= CardController.VALUE_PRINCESS; c++) {
			curLikelihood = GetCardProbabilityInHand(p, c);
			if(curLikelihood > highestLikelihood) {
				result = c;
				highestLikelihood = curLikelihood;
			}
		}
		return result;
	}

	// Returns GetMostLikelyHandValue(p) if certainty is above a threshold, and 0 otherwise
	public virtual int GetCertainHandValue(PlayerController p) {
		int result = GetMostLikelyHandValue(p);
		return (GetCardProbabilityInHand(p, result) > CERTAINTY_THRESHOLD ? result : 0);
	}

	public int GetRevealedCardCount(int OfValue) {
		Debug.Assert(MyController != null);
		int result = MyController.Game.DiscardPile.CardCount[OfValue] + (OfValue == myHand.Value ? 1 : 0);
		// If you've just drawn a card, account for it, too
		if(justDrawn != null && justDrawn.Value == OfValue) {
			result += 1;
		}
		// Handmaids do not immediately come onto the discard pile
		if(OfValue == CardController.VALUE_HANDMAID) {
			foreach(PlayerController p in MyController.Game.Players) {
				if(!p.KnockedOut && p.Protected) {
					result += 1;
				}
			}
		}
		return result;
	}

	public int GetCompleteRevealedCardCount() {
		Debug.Assert(MyController != null);
		// Basic result := all cards on discard pile plus my own hands
		int result = MyController.Game.DiscardPile.TotalCount + (myHand != null ? 1 : 0) + (justDrawn != null ? 1 : 0);
		// Handmaids do not immediately come onto the discard pile
		foreach(PlayerController p in MyController.Game.Players) {
			if(!p.KnockedOut && p.Protected) {
				result += 1;
			}
		}
		return result;
	}

	public bool WillThisPlayerHaveAnotherTurn(PlayerController targetPlayer) {
		int remainingCards = RemainingDeckSize + 1, curPlayer = MyController.SittingOrder;
		while(curPlayer != targetPlayer.SittingOrder) {
			if(!MyController.Game.Players[curPlayer].KnockedOut) {
				remainingCards -= 1;
				if(remainingCards <= 1) {
					return false;
				}
			}
			curPlayer = (curPlayer + 1) % MyController.Game.Players.Length;
		}
		return true;
	}
}
