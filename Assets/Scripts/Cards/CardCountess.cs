using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardCountess : CardController {

	public override int Value { 
		get { return VALUE_COUNTESS; } 
	}

	public override bool RequiresTarget {
		get { return false; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		// The Countess is always played in standalone mode
		// The rule that you MUST play it when your other card is a King or a Prince will be handled in the Resolve()
		return new List<MoveData> { new MoveData(player, this) };
	}

	protected override void SpecificResolve(MoveData move) {
		// The Countess has no effect
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(otherCard != null);
		// The Countess' utility is very high if the other card is a Prince or a King (i.e. the utility of not losing the round)
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		if(IsKnockOutByCountess(otherCard.Value, move.Card.Value)) {
			result.Rank = MoveData.RANK_MUST_PLAY;
		} else if(!move.Player.Game.TheKingAndBothPrincesAreDiscarded) {
			// Otherwise, it's the utility of potential misdirection (unless the King and both Princes are already discarded)
			result.Utility = Mathf.Max(0, (5f - otherCard.Value) / 4);
		}
		return result;
	}
}
