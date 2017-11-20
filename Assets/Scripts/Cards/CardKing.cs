using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardKing : CardController {

	public override int Value { 
		get { return VALUE_KING; } 
	}

	public override bool RequiresTarget {
		get { return true; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override bool CanBePlayedAgainstOneself {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		List<MoveData> result = new List<MoveData>();
		// The King is legal to play against any other player who is not knocked out yet
		foreach(PlayerController target in game.Players) {
			if(target != player && !target.KnockedOut) {
				result.Add(new MoveData(player, this, target));
			}
		}
		return result;
	}

	protected override void SpecificResolve(MoveData move) {
		Debug.Assert(move.Target != move.Player);
		// The players swap hands
		PlayerController p = move.Player, t = move.Target;
		CardController pHand = p.GetHand(), tHand = t.GetHand();
		p.SwapHand(tHand);
		t.SwapHand(pHand);
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(move.Target != null);
		Debug.Assert(otherCard != null);
		// No point in playing against protected opponents
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		if(otherCard.Value == CardController.VALUE_COUNTESS) {
			result.Rank = MoveData.RANK_NEVER_EVER;
			return result;
		} else if(move.Target.Protected || otherCard.Value == CardController.VALUE_GUARD || 
			(perceptorData.GetCardProbabilityInHand(move.Target, CardController.VALUE_GUARD) > 0) && perceptorData.WillThisPlayerHaveAnotherTurn(move.Target)) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		}
		// The Kings's utility is the difference between the marginal utilities of own hand and the (estimated) target's hand
		float ownMarginalHandValueUtility = PlayerController.GetMarginalHandValueUtility(move.Player.Game, otherCard.Value);
		// Estimate the target player's hand and marginal utility
		float targetsMarginalHandValueUtility = PlayerController.GetMarginalHandValueUtility(move.Player.Game, perceptorData.GetExpectedHandValue(move.Target));
		// Normalize the difference before returning
		result.Utility = (targetsMarginalHandValueUtility - ownMarginalHandValueUtility + 1f) / 2;
		return result;
	}

}
