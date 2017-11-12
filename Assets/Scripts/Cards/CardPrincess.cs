using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPrincess : CardController {

	public override int Value { 
		get { return VALUE_PRINCESS; } 
	}

	public override bool RequiresTarget {
		get { return false; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		// The Princess is always played in standalone mode (even though you lose instantly)
		return new List<MoveData> { new MoveData(player, this) };
	}

	protected override void SpecificResolve(MoveData move) {
		Debug.LogFormat("{0} has played the {1} and is automatically knocked out!", move.Player, this);
		// For turn history:
		move.KnockedOut = move.Player;
		move.AdditionalDiscard = move.Player.GetHand();
		move.Player.KnockOut();
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		// The utility of playing the Princess is always negative, because it is an instant loss
		return MoveData.DualUtility.MinValue;
	}

}
