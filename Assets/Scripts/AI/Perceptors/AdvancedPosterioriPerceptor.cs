using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedPosterioriPerceptor : PosterioriPerceptor {

	public const float UNIFORM_DISTRIBUTION = 1f / CardController.VALUE_PRINCESS;

	protected int[] HiddenHands;

	// Return the index of the next player, according to turn history
	protected int NextPlayerIndex {
		get {
			int result = TurnHistory[NextTurnToAnalyze].Player.SittingOrder;
			do {
				result = (result + 1) % PlayerCount;
			} while(PlayerIsKnockedOut[result]);
			return result;
		}
	}

	// Same memory initialization as for base class, plus hidden hands indices
	protected override void InitializeMemory() {
		base.InitializeMemory();
		// Initialize hidden hand indices
		int counter = 0;
		HiddenHands = new int[PlayerCount - 1];
		for(int i = 0; i < PlayerCount; i++) {
			if(MyController.Game.Players[i] != MyController) {
				HiddenHands[counter++] = i;
			}
		}
	}

	// Same reset as for base class, but also start waiting for the initial draw
	public override void ResetMemory() {
		base.ResetMemory();
		// After resetting memory, wait for the first card or cards to be drawn
		if(GameInitialized) {
			AnalysisOngoing = true;
			StartCoroutine(WaitForTheInitialDraw());
		}
	}

	// Waits until the first card or two cards are drawn to account for them before proper analysis begins
	protected IEnumerator WaitForTheInitialDraw() {
		// First, wait until the intial draw has settled in
		yield return new WaitUntil(() => (myHand != null && justDrawn == null));
		AccountForCard(myHand.Value);
		UpdateOwnHandDistribution();
		// If my turn is the very first one made, wait until the second card is drawn from deck to make a more intelligent move
		if(MyController.Game.CurrentPlayerIndex == MyController.SittingOrder) {
			// Now wait for the second draw
			yield return new WaitUntil(() => (myHand != null && justDrawn != null));
			// And update the distributions accordingly
			AccountForCard(justDrawn.Value);
		}
		RenormalizeDeckAndHandsDistributions();
		yield return new WaitForFixedUpdate();
		AnalysisOngoing = false;
	}

	// Analyzes the most recent turn made in the game
	protected override IEnumerator AnalyzeTurn(int id) {
		Debug.Assert(TurnHistory != null && TurnHistory.Count >= id);
		Debug.Assert(id == NextTurnToAnalyze);
		// Get the actual turn
		MoveData turn = TurnHistory[id];

		// Precompute some helpful booleans
		bool ItWasMyTurn = (turn.Player == MyController);
		bool IWasTheTarget = (turn.Target == MyController);
		bool ThereWasAKnockOut = (turn.KnockedOut != null);
		bool PlayerWasKnockedOut = (ThereWasAKnockOut && turn.KnockedOut == turn.Player);
		bool TargetWasKnockedOut = (ThereWasAKnockOut && turn.KnockedOut == turn.Target); 

		// Wait for some random time, just to ease the computational load per frame
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * PlayerCount));

		// This flag will be set to true in certain cases if no renormalization is requires
		bool RenormalizationCommenced = false;

		// Basic precomputations before getting into the specifics of card effects
		if(ItWasMyTurn) {
			// Update my own hand distribution, just for simplicity
			UpdateOwnHandDistribution();
			// Update my own play statistic
			UpdatePlayStatistics(turn);
			// Other players' knowledge of your hand becomes irrelevant after your own turn
			Array.Clear(PlayerKnowsThatMyHandIs, 0, PlayerCount);
			// We don't need to update unaccounted-for cards with the card just played and the card distribution, 
			// because that was done at the end of the last analysis run (of the player immediately before us)
		} else {
			// When analyzing another player's turn, filter their hand first:
			FilterHiddenHandWithPlayedCard(turn.Player.SittingOrder, turn.Card.Value);
			// The card they just played lands on the discard pile, so update the unaccounted-for list
			AccountForCard(turn.Card.Value);

			// Also update this player's stats
			if(turn.Target == MyController) {
				PlayerHasTargetedMe[turn.Player.SittingOrder] += 1;
			} 
			if(turn.KnockedOut != null && turn.KnockedOut != turn.Player) {
				PlayerHasKnockOuts[turn.Player.SittingOrder] += 1;
			}
		}

		// Wait some more
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * PlayerCount));

		// If there was a knock-out by something other than a Baron, then the filtering is simple
		if(ThereWasAKnockOut && turn.Card.Value != CardController.VALUE_BARON) {
			KnockOutFilter(turn.KnockedOut.SittingOrder, turn.AdditionalDiscard.Value);
		} else if(!turn.NoEffect) {
			// Otherwise, if the card effect wasn't blocked by a Handmaid, we check specific card effects
			switch(turn.Card.Value) {
			case CardController.VALUE_GUARD:
				// If the guard wasn't played against me and the target is still in the game,
				// I at least know what their hand is NOT
				if(!IWasTheTarget) {
					HiddenHandIsNot(turn.Target.SittingOrder, turn.TargetHandGuess);
				}
				break;
			case CardController.VALUE_PRIEST:
				// If it was my turn, I now know the target's hand
				if(ItWasMyTurn) {
					// With a Priest, I now know the target's hand
					Debug.Assert(turn.Target == lastLearnedHandOf);
					UpdateHandDistributionWithCertainty(lastLearnedHandOf.SittingOrder, lastLearnedCard.Value);
				} else if(IWasTheTarget) {
					// But if I was the target, the other player now knows my hand!
					PlayerKnowsThatMyHandIs[turn.Player.SittingOrder] = myHand.Value;
				}
				break;
			case CardController.VALUE_BARON:
				// If either the player or the target were knocked out by Baron, we apply the Baron KO filter
				if(PlayerWasKnockedOut) {
					BaronEffectFilterWithKnockout(turn.Target.SittingOrder, turn.Player.SittingOrder, turn.AdditionalDiscard.Value);
				} else if(TargetWasKnockedOut) {
					BaronEffectFilterWithKnockout(turn.Player.SittingOrder, turn.Target.SittingOrder, turn.AdditionalDiscard.Value);
				} else {
					// On a draw between two other players, their hand distributions are now equal
					BaronEffectFilterWithDraw(turn.Player.SittingOrder, turn.Target.SittingOrder);
				}
				// If there was a draw involving me, the other player now knows my hand!
				if(!ThereWasAKnockOut && (ItWasMyTurn || IWasTheTarget)) {
					PlayerKnowsThatMyHandIs[ItWasMyTurn ? turn.Target.SittingOrder : turn.Player.SittingOrder] = myHand.Value;
				}
				break;
			case CardController.VALUE_PRINCE:
				// If I was the target of the Prince, I just need to update my hand
				if(IWasTheTarget) {
					// Even if it was my turn, I have already accounted for my old hand and the card I just drew (both of which I have discarded),
					// as well as have updated my own hand distribution, therefore I only need to account for the new card in my hand
					Debug.Assert(DrawnBecauseOfThePrince != null);
					UpdateHandDistributionWithCertainty(MyController.SittingOrder, DrawnBecauseOfThePrince.Value);
					AccountForCard(DrawnBecauseOfThePrince.Value);
					DrawnBecauseOfThePrince = null;
				} else {
					// Otherwise, the special Prince filter logic applies (the values are automatically renormalized)
					PrinceEffectFilterWithoutKnockout(turn.Target.SittingOrder, turn.AdditionalDiscard.Value);
					RenormalizationCommenced = true;
				}
				break;
			case CardController.VALUE_KING:
				// If I was involved in the swap, I know quite a lot about the other player's hand now (and vice versa)
				if(ItWasMyTurn) {
					KingEffectFilterInvolvingMe(turn.Target.SittingOrder);
				} else if(IWasTheTarget) {
					KingEffectFilterInvolvingMe(turn.Player.SittingOrder);
				} else {
					// If I wasn't involved, just swap the hand distributions of both players who were
					SwapHandDistributions(turn.Player.SittingOrder, turn.Target.SittingOrder);
				}
				break;
			default:
				// Nothing to do when Handmaid, Countess, or Princess were played
				break;
			}
		}

		// Wait some more
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0, Time.fixedDeltaTime * PlayerCount));

		// If I'm up next, wait until I've drawn my next card before finishing the analysis
		if(turn.Player != MyController && MyController.Game.Deck.CountCardsLeft > 1 && NextPlayerIndex == MyController.SittingOrder) {
			yield return new WaitUntil(() => ((myHand != null && justDrawn != null)) || MyController.Game.RoundOver);
			if(!MyController.Game.RoundOver) {
				// And update the distributions accordingly
				AccountForCard(justDrawn.Value);
				RenormalizeDeckAndHandsDistributions();
			}
		} else if(!RenormalizationCommenced) {
			// If a renormalization is still required, do it now (it is a very expensive call)
			RenormalizeDeckAndHandsDistributions();
		}

		// Finally, finish the update and return
		NextTurnToAnalyze = id + 1;
		if(FullLogging) {
			displayCurrentBeliefs();
		}
		yield return new WaitForFixedUpdate();
		AnalysisOngoing = false;
	}

	// The knocked-out player has no cards anymore, their discarded hand is accounted for
	protected void KnockOutFilter(int KnockedOutPlayerIndex, int DiscardedHandValue) {
		Array.Clear(HandDistribution[KnockedOutPlayerIndex], 0, CARD_VECTOR_LENGTH);
		AccountForCard(DiscardedHandValue);
		PlayerIsKnockedOut[KnockedOutPlayerIndex] = true;
	}

	// If the Baron has knocked a player out, it gives us a lot of information about the winner's hand
	protected override void BaronEffectFilterWithKnockout(int winnerIndex, int loserIndex, int LosersHandValue) {
		if(winnerIndex != MyController.SittingOrder) {
			base.BaronEffectFilterWithKnockout(winnerIndex, loserIndex, LosersHandValue);
		}
		// The knocked-out player has no cards anymore
		KnockOutFilter(loserIndex, LosersHandValue);
	}

	// If a Baron play ended in a draw, it's also a LOT of information
	protected override void BaronEffectFilterWithDraw(int playerIndex, int targetIndex) {
		// If I was involved in the draw in any way, I now know that the other player has the same card as me
		if(playerIndex == MyController.SittingOrder) {
			UpdateHandDistributionWithCertainty(targetIndex, myHand.Value);
		} else if(targetIndex == MyController.SittingOrder) {
			UpdateHandDistributionWithCertainty(playerIndex, myHand.Value);
		} else {
			// Otherwise, we just update the player's hand distribution
			for(int c = CardController.VALUE_GUARD; c <= CardController.VALUE_PRINCESS; c++) {
				HandDistribution[playerIndex][c] *= (CountUnaccountedForCards[c] > 1) ? HandDistribution[targetIndex][c] : 0;
			}
			// Renormalize it
			RenormalizeCardDistribution(ref HandDistribution[playerIndex], playerIndex);
			// And copy it over the target's distribution
			Array.Copy(HandDistribution[playerIndex], HandDistribution[targetIndex], CARD_VECTOR_LENGTH);
		}
	}

	// If a Prince had been played against someone other than me and they weren't knocked out,
	// we need to update the deck distribution in a specific manner and copy it over the target's hand
	protected void PrinceEffectFilterWithoutKnockout(int TargetIndex, int DiscardedHandValue) {
		AccountForCard(DiscardedHandValue);
		// Then we temporarily set the target's hand distribution to all-zeroes, 
		// as well as temporarily setting them to "knocked-out" state, just so RenormalizeDeckAndHandsDistributions() functions as expected
		Array.Clear(HandDistribution[TargetIndex], 0, CARD_VECTOR_LENGTH);
		PlayerIsKnockedOut[TargetIndex] = true;
		// We renormalize the remaining hands and the deck, also set a flag not to do it again at the end
		RenormalizeDeckAndHandsDistributions();
		// Finally, we copy the deck distribution over the target hand's distribution
		Array.Copy(DeckDistribution, HandDistribution[TargetIndex], CARD_VECTOR_LENGTH);
		PlayerIsKnockedOut[TargetIndex] = false;
	}

	// If a King was played by or against me, I need to update my hand, but I also know the other player's hand now (and vice versa)
	protected void KingEffectFilterInvolvingMe(int OtherPlayerIndex) {
		Debug.Assert(MyHandBeforeKing != null);
		// I know that the target has my old card now
		UpdateHandDistributionWithCertainty(OtherPlayerIndex, MyHandBeforeKing.Value);
		// However, since I am no longer directly seeing this card, it must be removed from the list of accounted-for cards
		CountUnaccountedForCards[MyHandBeforeKing.Value] += 1;
		// On the bright side, I now have the target's own hand and account for it, too
		AccountForCard(myHand.Value);
		// Also, the other player knows what my hand is now
		PlayerKnowsThatMyHandIs[OtherPlayerIndex] = myHand.Value;
		// Reset the card, just in case
		MyHandBeforeKing = null;
	}

	protected void RenormalizeDeckAndHandsDistributions() {
		// Initialize distribution of all known hands plus deck
		float[] Hand1Distr = new float[CARD_VECTOR_LENGTH];
		float[] Hand2Distr = new float[CARD_VECTOR_LENGTH];
		float[] Hand3Distr = new float[CARD_VECTOR_LENGTH];
		float[] NewDeckCount = new float[CARD_VECTOR_LENGTH];
		// Loop through the possible values of the first hidden hand
		float Hand1Prob = 0, Hand2Prob = 0, Hand3Prob = 0;
		for(int h1 = CardController.VALUE_GUARD; h1 <= CardController.VALUE_PRINCESS; h1++) {
			if(PlayerIsKnockedOut[HiddenHands[0]]) {
				// This is a dirty hack, but it will be normalized later on
				Hand1Prob = UNIFORM_DISTRIBUTION;
			} else {
				Hand1Prob = HandDistribution[HiddenHands[0]][h1];
				// Check constraints
				if(Hand1Prob <= 0 || CountUnaccountedForCards[h1] <= 0) {
					continue;
				}
			}
			// Loop through the second hidden hand
			for(int h2 = CardController.VALUE_GUARD; h2 <= CardController.VALUE_PRINCESS; h2++) {
				if(PlayerIsKnockedOut[HiddenHands[1]]) {
					Hand2Prob = UNIFORM_DISTRIBUTION;
				} else {
					Hand2Prob = HandDistribution[HiddenHands[1]][h2];
					// Check constraints
					if(Hand2Prob <= 0 || CountUnaccountedForCards[h2] <= 0 || (h1 == h2 && CountUnaccountedForCards[h2] < 2)) {
						continue;
					}
				}
				// Loop through the last hidden hand
				for(int h3 = CardController.VALUE_GUARD; h3 <= CardController.VALUE_PRINCESS; h3++) {
					if(PlayerIsKnockedOut[HiddenHands[2]]) {
						Hand3Prob = UNIFORM_DISTRIBUTION;
					} else {
						Hand3Prob = HandDistribution[HiddenHands[2]][h3];
						// Check constraints
						if(Hand3Prob <= 0 || CountUnaccountedForCards[h3] <= 0 ||
							((h1 == h3 || h2 == h3) && CountUnaccountedForCards[h3] < 2) ||
							(h1 == h2 && h2 == h3 && CountUnaccountedForCards[h3] < 3)) {
							continue;
						}
					}
					// Marginalize probabilities over each hand
					float jointProb = Hand1Prob * Hand2Prob * Hand3Prob;
					Hand1Distr[h1] += jointProb;
					Hand2Distr[h2] += jointProb;
					Hand3Distr[h3] += jointProb;
					// Loop through the deck and update the count probabilities (normalization to sum = 0 will be made later)
					for(int dc = CardController.VALUE_GUARD; dc <= CardController.VALUE_PRINCESS; dc++) {
						// Hypothetically, if each of the hands were h1, h2, and h3, what would be left of this card type in the deck?
						int CardsOfThisValueLeftInDeck = Mathf.Max(0, CountUnaccountedForCards[dc] -
							(!PlayerIsKnockedOut[HiddenHands[0]] && (dc == h1) ? 1 : 0) - 
							(!PlayerIsKnockedOut[HiddenHands[1]] && (dc == h2) ? 1 : 0) - 
							(!PlayerIsKnockedOut[HiddenHands[2]] && (dc == h3) ? 1 : 0));
						// Sum up the probability of this card in the deck
						NewDeckCount[dc] += jointProb * CardsOfThisValueLeftInDeck;
					}
				}
			}
		}
		// Update hand distributions as necessary
		if(!PlayerIsKnockedOut[HiddenHands[0]]) {
			RenormalizeCardDistribution(ref Hand1Distr, HiddenHands[0]);
			Array.Copy(Hand1Distr, HandDistribution[HiddenHands[0]], CARD_VECTOR_LENGTH);
		}
		if(!PlayerIsKnockedOut[HiddenHands[1]]) {
			RenormalizeCardDistribution(ref Hand2Distr, HiddenHands[1]);
			Array.Copy(Hand2Distr, HandDistribution[HiddenHands[1]], CARD_VECTOR_LENGTH);
		}
		if(!PlayerIsKnockedOut[HiddenHands[2]]) {
			RenormalizeCardDistribution(ref Hand3Distr, HiddenHands[2]);
			Array.Copy(Hand3Distr, HandDistribution[HiddenHands[2]], CARD_VECTOR_LENGTH);
		}
		// Finally, update the deck distribution
		Array.Copy(NewDeckCount, DeckDistribution, CARD_VECTOR_LENGTH);
		RenormalizeCardDistribution(ref DeckDistribution);
	}
}
