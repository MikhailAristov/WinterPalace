using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotStupidPlayerController : AIPlayerController {

	protected bool KnowAtLeastOneOtherHand {
		get { 
			for(int i = 0; i < Game.Players.Length; i++) {
				// Guards are of no interest in this case
				if(i != SittingOrder && MyPerceptor.GetCertainHandValue(Game.Players[i]) > CardController.VALUE_GUARD) {
					return true;
				}
			}
			return false;
		}
	}

	protected int WhoHasThePrincess {
		get {
			for(int i = 0; i < Game.Players.Length; i++) {
				if(MyPerceptor.GetCertainHandValue(Game.Players[i]) == CardController.VALUE_PRINCESS) {
					return (i != SittingOrder ? i : -1);
				}
			}
			return -1;
		}
	}

	protected override IEnumerator PickAMove() {
		// Sort available moves into stupid and not-stupid lists
		List<MoveData> stupidMoves = new List<MoveData>(), notStupidMoves = new List<MoveData>();
		foreach(MoveData m in myHand.GetLegalMoves(Game, this)) {
			if(IsMoveSuicidal(m, justDrawn)) {
				continue;
			} else if(IsMoveStupid(m, justDrawn)) {
				stupidMoves.Add(m);
			} else {
				notStupidMoves.Add(m);
			}
		}
		foreach(MoveData m in justDrawn.GetLegalMoves(Game, this)) {
			if(IsMoveSuicidal(m, myHand)) {
				continue;
			} else if(IsMoveStupid(m, myHand)) {
				stupidMoves.Add(m);
			} else {
				notStupidMoves.Add(m);
			}
		}
		Debug.Assert(stupidMoves.Count + notStupidMoves.Count > 0);
		// If there are not-stupid moves, randomly pick one and return; otherwise, pick a random stupid move
		if(notStupidMoves.Count == 0) {
			Debug.LogWarning("only stupid moves available...");
		}
		myNextMove = (notStupidMoves.Count > 0) ? 
					  notStupidMoves[UnityEngine.Random.Range(0, notStupidMoves.Count)] : 
					  stupidMoves[UnityEngine.Random.Range(0, stupidMoves.Count)];
		yield return null;
	}

	protected override void postDrawHook() {
		MyPerceptor.UpdateOwnHand(myHand, justDrawn);
	}

	protected override void postMoveSelectHook() {
		MyPerceptor.UpdateOwnHand(myHand, justDrawn);
	}

	public override void LearnHand(PlayerController owner) {
		CardController cc = owner.RevealHand(this);
		Debug.LogFormat("{0} now knows that {1} has the {2} in hand!", this, owner, cc);
		MyPerceptor.LearnHand(owner, cc);
	}

	public override CardController RevealHand(PlayerController toPlayer) {
		return myHand;
	}

	// Playing a Princess or not playing a Countess when you have a Prince/King will result in instantly losing the round
	// Such moves should not even be considered
	protected bool IsMoveSuicidal(MoveData move, CardController otherCard) {
		return (CardController.IsKnockOutByPrincess(move.Card.Value, otherCard.Value, move.Player == move.Target) 
			|| CardController.IsKnockOutByCountess(move.Card.Value, otherCard.Value));
	}

	// Returns whether the move under consideration looks stupid or not
	protected bool IsMoveStupid(MoveData move, CardController otherCard) {
		// Penalize playing against the human playing before they had a first turn after launching a game
		if(move.Target == Game.HumanPlayer && !MyPerceptor.HumanPlayerHasActed) {
			return true;
		}
		// Playing anything against a player protected by a Handmaid looks stupid
		if(move.Target != null && move.Target.Protected) {
			return true;
		}
		// NOT playing your hand when someone else knows your hand and there are still Guards in play
		if(MyPerceptor.SomeoneKnowsMyHand && move.Card != myHand && move.Card.Value != otherCard.Value && 
			move.Card.Value != CardController.VALUE_HANDMAID &&
			MyPerceptor.GetRevealedCardCount(CardController.VALUE_GUARD) < GameController.CARD_COUNT[CardController.VALUE_GUARD]) {
			return true;
		}
		// Playing the higher card in the very last round
		if(MyPerceptor.RemainingDeckSize <= 1 && move.Card.Value > otherCard.Value) {
			return true;
		}
		// Otherwise, it depends on the type of card
		switch(move.Card.Value) {
		case CardController.VALUE_GUARD:
			// Guessing a card when all cards of that value are either on the discard pile, or in your own hand
			if(MyPerceptor.GetRevealedCardCount(move.TargetHandGuess) >= GameController.CARD_COUNT[move.TargetHandGuess]) {
				return true;
			} else if(MyPerceptor.GetCertainHandValue(move.Target) != move.TargetHandGuess && KnowAtLeastOneOtherHand) {
				// Playing a Guard against a player whose hand you don't know when you know someone else's hand
				return true;
			}
			return false;
		case CardController.VALUE_PRIEST:
			// Playing a Priest against a player whose hand you already know
			return (MyPerceptor.GetCertainHandValue(move.Target) > 0);
		case CardController.VALUE_BARON:
			// Playing a Baron when your other card is a guard is stupid, period
			if(otherCard.Value == CardController.VALUE_GUARD) {
				return true;
			} else if(otherCard.Value < MyPerceptor.GetCertainHandValue(move.Target)) {
				// Playing a Baron against a player who you know has as higher hand
				return true;
			}
			return false;
		case CardController.VALUE_PRINCE:
			// Playing a Prince against yourself in general
			if(move.Target == this) {
				return true;
			} else {
				// Playing a Prince against anyone else when you know who has the Princess
				int hasThePrincess = WhoHasThePrincess;
				return (hasThePrincess >= 0 && move.Target.SittingOrder != hasThePrincess);
			}
		case CardController.VALUE_KING:
			// Playing a King when your other card is a guard is just handing them the key to knock you out
			if(otherCard.Value == CardController.VALUE_GUARD) {
				return true;
			} else {
				// Playing a King against anyone who will have a chance to knock you out
				bool thereAreStillGuardsInPlay = (MyPerceptor.GetRevealedCardCount(CardController.VALUE_GUARD) < GameController.CARD_COUNT[CardController.VALUE_GUARD]);
				return (MyPerceptor.WillThisPlayerHaveAnotherTurn(move.Target) && thereAreStillGuardsInPlay);
			}
		default:
			return false;
		}
	}

	public override void Reset() {
		base.Reset();
		MyPerceptor.ResetMemory();
	}
}
