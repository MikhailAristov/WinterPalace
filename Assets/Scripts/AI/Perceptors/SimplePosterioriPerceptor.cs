using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePosterioriPerceptor : PrioriPerceptor {

	protected bool GameInitialized = false;

	protected List<MoveData> TurnHistory;
	protected int NextTurnToAnalyze;

	protected int[] KnownHands;

	protected PlayerController lastLearnedHandOf;
	protected CardController lastLearnedCard;

	protected int SomeoneKnowsThatMyHandIs;
	public override bool SomeoneKnowsMyHand {
		get {
			return (SomeoneKnowsThatMyHandIs == myHand.Value);
		}
	}

	// Update is called once per frame
	void Update () {
		// Get the game parameters as soon as available
		if(!GameInitialized && MyController.Game != null) {
			// Set the turn history reference
			TurnHistory = MyController.Game.TurnHistory;
			// Initialized hand distributions
			KnownHands = new int[CardController.VALUE_PRINCESS + 1];
			GameInitialized = true;
			// Reset memory
			ResetMemory();
		}

		if(GameInitialized && myHand != MyController.GetHand()) {
			myHand = MyController.GetHand();
		}

		if(myHand != null && KnownHands[MyController.SittingOrder] == 0) {
			KnownHands[MyController.SittingOrder] = myHand.Value;
		}

		// Check the turn history for new data, one at a time
		if(TurnHistory != null && !MyController.KnockedOut && TurnHistory.Count > NextTurnToAnalyze) {
			UpdateHandKnowledgeFromTurn(NextTurnToAnalyze);
		}
	}

	public override void ResetMemory() {
		if(!GameInitialized) {
			return;
		}
		// Reset the next-to-analyze turn back to 0
		NextTurnToAnalyze = 0;
		// Clear hand knowledge
		SomeoneKnowsThatMyHandIs = 0;
		Array.Clear(KnownHands, 0, KnownHands.Length);
	}

	protected void UpdateHandKnowledgeFromTurn(int id) {
		MoveData turn = TurnHistory[id];
		// Precompute some boolean variables
		bool PlayedByMe = (turn.Player == MyController), PlayedAgainstMe = (turn.Target == MyController);
		// First, check if another player's turn invalidates your prior knowledge of their hand,
		// i.e. whether they have played the card you know they had
		if(turn.Card.Value == KnownHands[turn.Player.SittingOrder]) {
			KnownHands[turn.Player.SittingOrder] = 0;
		}
		// The check if your turn invalidated others' knowledge of your hand
		if(PlayedByMe && turn.Card.Value == SomeoneKnowsThatMyHandIs) {
			SomeoneKnowsThatMyHandIs = 0;
		}
		// Then analyze the turn in detail, unless it had no effect
		if(!turn.NoEffect) {
			switch(turn.Card.Value) {
			case CardController.VALUE_PRIEST:
				// This is only interesting if played by or against myself
				if(PlayedByMe) {
					KnownHands[lastLearnedHandOf.SittingOrder] = lastLearnedCard.Value;
				} else if(PlayedAgainstMe) {
					SomeoneKnowsThatMyHandIs = myHand.Value;
				}
				break;
			case CardController.VALUE_BARON:
				// This is only interesting if played by or against myself
				if(PlayedByMe || PlayedAgainstMe) {
					KnownHands[lastLearnedHandOf.SittingOrder] = lastLearnedCard.Value;
					// If no one was knocked out, then the other player now knows my card
					if(turn.KnockedOut == null) {
						SomeoneKnowsThatMyHandIs = myHand.Value;
					}
				}
				break;
			case CardController.VALUE_PRINCE:
				// The Prince automatically invalidates any knowledge of the target's hand
				KnownHands[turn.Target.SittingOrder] = 0;
				break;
			case CardController.VALUE_KING:
				// Swap the knowledge around
				int temp = KnownHands[turn.Player.SittingOrder];
				KnownHands[turn.Player.SittingOrder] = KnownHands[turn.Target.SittingOrder];
				KnownHands[turn.Target.SittingOrder] = temp;
				// If played by or against myself, there is also the issue of other player's knowledge...
				if(PlayedByMe || PlayedAgainstMe) {
					SomeoneKnowsThatMyHandIs = myHand.Value;
				}
				break;
			default:
				// All other cards required probabilistic analysis too complex for this knowledge model
				break;
			}
			// Check for knock-outs
			if(turn.KnockedOut != null) {
				KnownHands[turn.KnockedOut.SittingOrder] = 0;
			}
		}
		// Show what you know
		//DisplayKnowledge();
		// Return
		NextTurnToAnalyze = id + 1;
	}

	public override int GetCertainHandValue(PlayerController p) {
		return KnownHands[p.SittingOrder];
	}

	public override void LearnHand(PlayerController ofPlayer, CardController card) {
		lastLearnedHandOf = ofPlayer;
		lastLearnedCard = card;
	}

	protected void DisplayKnowledge() {
		string output = "";
		foreach(PlayerController p in MyController.Game.Players) {
			if(!p.KnockedOut && p != MyController && KnownHands[p.SittingOrder] > 0) {
				output += string.Format("{0} - {1}; ", p, CardController.NAMES[KnownHands[p.SittingOrder]]);
			}
		}
		Debug.Log(string.Format("{0}'s knowledge: ", MyController) + (output.Length > 0 ? output : "empty"));
	}
}
