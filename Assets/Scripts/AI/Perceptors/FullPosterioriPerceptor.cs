using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullPosterioriPerceptor : PosterioriPerceptor {

	protected override IEnumerator AnalyzeTurn(int id) {
		Debug.Assert(TurnHistory != null && TurnHistory.Count >= id);
		Debug.Assert(id == NextTurnToAnalyze);
		// Get the actual turn
		MoveData turn = TurnHistory[id];
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, 0.2f));
		// STEP 1: Predict what they may have drawn and filter out impossible outcomes
		FilterHiddenHandWithPlayedCard(turn.Player.SittingOrder, turn.Card.Value);
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, 0.2f));
		// STEP 2: Filter the situation with the discard information
		DiscardFilterStep(turn.Card.Value);
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, 0.2f));
		// STEP 3: Analyze how the turn was resolved
		CardEffectsFilter(turn);
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, 0.2f));
		// STEP 4: Update deck contents again
		DeckContentsFilterStep();
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, 0.2f));
		// Other players' knowledge of your hand becomes irrelevant after your own turn
		if(turn.Player == MyController) {
			UpdateOwnHandDistribution();
			Array.Clear(PlayerKnowsThatMyHandIs, 0, PlayerCount);
		} else {
			// Update other players' stats
			if(turn.Target == MyController) {
				PlayerHasTargetedMe[turn.Player.SittingOrder] += 1;
			} 
			if(turn.KnockedOut != null && turn.KnockedOut != turn.Player) {
				PlayerHasKnockOuts[turn.Player.SittingOrder] += 1;
			}
		}
		// Return
		NextTurnToAnalyze = id + 1;
		//displayCurrentBeliefs();
		AnalysisOngoing = false;
	}

	protected void CardEffectsFilter(MoveData turn) {
		// If the turn was blocked by a Handmaiden, there is no effect
		if(turn.NoEffect) {
			return;
		}
		// Update my own play statistic
		UpdatePlayStatistics(turn);
		// Precompute some utility variables
		bool targetKnockedOut = (turn.Target != null && turn.Target == turn.KnockedOut);
		bool playerKnockedOut = (turn.Player == turn.KnockedOut);
		// Otherwise, the resolution depends on the card played
		switch(turn.Card.Value) {
		case CardController.VALUE_GUARD:
			// If the target is not knocked out by the guess, we know which card they DON'T HAVE
			if(!targetKnockedOut) {
				HiddenHandIsNot(turn.Target.SittingOrder, turn.TargetHandGuess);
			}
			break;
		case CardController.VALUE_PRIEST:
			// A priest only changes something when played by me
			if(turn.Player == MyController) {
				Debug.Assert(lastLearnedHandOf == turn.Target);
				UpdateHandDistributionWithCertainty(turn.Target.SittingOrder, lastLearnedCard.Value);
			}
			break;
		case CardController.VALUE_BARON:
			// If I played this Baron, I only learn anything useful if no one was knocked out
			if(turn.Player == MyController) {
				// Namely that my opponent had the same card as I
				if(turn.KnockedOut == null) {
					UpdateHandDistributionWithCertainty(turn.Target.SittingOrder, myHand.Value);
				}
			} else {
				// Otherwise, filter the probabilities of the hands involved extensively
				if(targetKnockedOut || playerKnockedOut) {
					if(targetKnockedOut) {
						BaronEffectFilterWithKnockout(turn.Player.SittingOrder, turn.Target.SittingOrder, turn.AdditionalDiscard.Value);
					} else {
						BaronEffectFilterWithKnockout(turn.Target.SittingOrder, turn.Player.SittingOrder, turn.AdditionalDiscard.Value);
					}
				} else {
					BaronEffectFilterWithDraw(turn.Player.SittingOrder, turn.Target.SittingOrder);	
				}
			}
			break;
		case CardController.VALUE_PRINCE:
			// For one, we know that the player didn't have a Countess in hand
			if(!playerKnockedOut && HandDistribution[turn.Player.SittingOrder][CardController.VALUE_COUNTESS] > 0) {
				HiddenHandIsNot(turn.Player.SittingOrder, CardController.VALUE_COUNTESS);
			}
			// We then perform a discard filtering step for the card discarded by the target
			DiscardFilterStep(turn.AdditionalDiscard.Value);
			// The target's hand distribution is now equal to the update deck distribution, unless it's my own
			if(turn.Target == MyController) {
				UpdateOwnHandDistribution();
			} else {
				Array.Copy(DeckDistribution, HandDistribution[turn.Target.SittingOrder], CARD_VECTOR_LENGTH);
			}
			break;
		case CardController.VALUE_KING:
			// For one, we know that the player didn't have a Countess in hand
			if(!playerKnockedOut && HandDistribution[turn.Player.SittingOrder][CardController.VALUE_COUNTESS] > 0) {
				HiddenHandIsNot(turn.Player.SittingOrder, CardController.VALUE_COUNTESS);
			}
			// Now we simply swap around the two players' hand distributions
			SwapHandDistributions(turn.Player.SittingOrder, turn.Target.SittingOrder);
			// If I was involved myself, update my distribution just in case
			if(turn.Player == MyController || turn.Target == MyController) {
				UpdateOwnHandDistribution();
			}
			break;
		default:
			// The Handmaid, Countess, and Princess have no particular resolutions
			break;
		}
		// Knock-out effects
		if(targetKnockedOut) {
			Array.Clear(HandDistribution[turn.Target.SittingOrder], 0, CARD_VECTOR_LENGTH);
			PlayerIsKnockedOut[turn.Target.SittingOrder] = true;
			DiscardFilterStep(turn.AdditionalDiscard.Value);
		} else if(playerKnockedOut) {
			Array.Clear(HandDistribution[turn.Player.SittingOrder], 0, CARD_VECTOR_LENGTH);
			PlayerIsKnockedOut[turn.Player.SittingOrder] = true;
			DiscardFilterStep(turn.AdditionalDiscard.Value);
		}
	}

	protected void DiscardFilterStep(int DiscardedCardValue) {
		// Estimate how many cards of this value are still in play
		float cardsInPlay = CountUnaccountedForCards[DiscardedCardValue];
		for(int p1 = 0; p1 < PlayerCount; p1++) {
			cardsInPlay += HandDistribution[p1][DiscardedCardValue];
		}
		if(cardsInPlay > 0) {
			// Add discarded card to the counter
			if(CountUnaccountedForCards[DiscardedCardValue] > 0) {
				AccountForCard(DiscardedCardValue);
			}
			// Calculate the factor by which to reduce all probabilities of having this card
			float scalingFactor = Mathf.Max(0, cardsInPlay - 1) / cardsInPlay;
			// Scale all probabilities accordingly and renormalize
			for(int p2 = 0; p2 < PlayerCount; p2++) {
				HandDistribution[p2][DiscardedCardValue] *= scalingFactor;
				RenormalizeCardDistribution(ref HandDistribution[p2], p2);
			}
			// Update the deck distribution (dirty, but efficient)
			DeckContentsFilterStep();
		}
	}

	protected void DeckContentsFilterStep() {
		// Update the deck distribution
		for(int dc = CardController.VALUE_GUARD; dc <= CardController.VALUE_PRINCESS; dc++) {
			DeckDistribution[dc] = CountUnaccountedForCards[dc];
			// Add all the players
			for(int p = 0; p < PlayerCount; p++) {
				DeckDistribution[dc] += HandDistribution[p][dc];
			}
		}
		// Dirty, but efficient
		RenormalizeCardDistribution(ref DeckDistribution);
	}
}
