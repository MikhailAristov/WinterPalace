using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHandmaid : CardController {

	public override int Value { 
		get { return VALUE_HANDMAID; } 
	}

	public override bool RequiresTarget {
		get { return false; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		// The Handmaid is always played in standalone mode
		return new List<MoveData> { new MoveData(player, this) };
	}

	protected override void SpecificResolve(MoveData move) {
		// Set the protected state
		move.Player.Protected = true;
		// Move the card to the front
		move.Card.transform.SetParent(move.Player.Front.transform);
		move.Card.transform.localPosition = Vector3.zero;
		move.Card.transform.localRotation = Quaternion.identity;
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(otherCard != null);
		// The Handmaid's utility is just the marginal utility of the card remaining in the player's hand
		return new MoveData.DualUtility(MoveData.RANK_NORMAL, PlayerController.GetMarginalHandValueUtility(move.Player.Game, otherCard.Value));
	}

}
