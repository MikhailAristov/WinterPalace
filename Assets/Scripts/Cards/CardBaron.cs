using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBaron : CardController {

	public override int Value { 
		get { return VALUE_BARON; } 
	}

	public override bool RequiresTarget {
		get { return true; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		List<MoveData> result = new List<MoveData>();
		// The Baron is legal to play against any other player who is not knocked out yet
		foreach(PlayerController target in game.Players) {
			if(target != player && !target.KnockedOut) {
				result.Add(new MoveData(player, this, target));
			}
		}
		return result;
	}

	protected override void SpecificResolve(MoveData move) {
		Debug.Assert(move.Target != move.Player);
		// Both players learn each other's hands
		PlayerController p = move.Player, t = move.Target;
		p.LearnHand(t);
		t.LearnHand(p);
		// The one with the lower hand value is knocked out
		CardController pHand = p.GetHand(), tHand = t.GetHand();
		int pValue = pHand.Value, tValue = tHand.Value;
		if(pValue > tValue) {
			// For turn history:
			move.KnockedOut = t;
			move.AdditionalDiscard = tHand;
			Debug.LogFormat("{0} had the lower hand ({1}) and is knocked out!", t, tHand);
			t.KnockOut();
		} else if(tValue > pValue) {
			// For turn history:
			move.KnockedOut = p;
			move.AdditionalDiscard = pHand;
			Debug.LogFormat("{0} had the lower hand ({1}) and is knocked out!", p, pHand);
			p.KnockOut();
		} else {
			Debug.LogFormat("{0} and {1} had equal-value hands, so nothing happens!", p, t);
		}
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(move.Target != null);
		Debug.Assert(otherCard != null);
		// No point in playing against protected opponents
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		if(move.Target.Protected || otherCard.Value == CardController.VALUE_GUARD) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		}
		// The Baron's utility is the certainty that the other player's card is lower than your own
		for(int i = CardController.VALUE_GUARD; i < otherCard.Value; i++) {
			result.Utility += perceptorData.GetCardProbabilityInHand(move.Target, i);
		}
		// However, if the chance of tying or losing is above the certainty threshold, also drop the rank
		if((1f - result.Utility) > AIGenericPerceptor.CERTAINTY_THRESHOLD) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		}
		return result;
	}
}
