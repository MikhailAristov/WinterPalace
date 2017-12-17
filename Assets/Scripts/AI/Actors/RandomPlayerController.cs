using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlayerController : AIPlayerController {

	protected override IEnumerator PickAMove() {
		List<MoveData> availableMoves = myHand.GetLegalMoves(Game, this);
		availableMoves.AddRange(justDrawn.GetLegalMoves(Game, this));
		// Prevent stupid moves, i.e. ones that instantly knock you out
		MoveData chosenMove = availableMoves[0];
		int otherCardValue = 0;
		do {
			chosenMove = availableMoves[UnityEngine.Random.Range(0, availableMoves.Count)];
			otherCardValue = (chosenMove.Card == justDrawn) ? myHand.Value : justDrawn.Value;
		} while(CardController.IsKnockOutByPrincess(chosenMove.Card.Value, otherCardValue, chosenMove.Player == chosenMove.Target) ||
			CardController.IsKnockOutByCountess(chosenMove.Card.Value, otherCardValue));
		myNextMove = chosenMove;
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
	}

	public override CardController RevealHand(PlayerController toPlayer) {
		return myHand;
	}
}
