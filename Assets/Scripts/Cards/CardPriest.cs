using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPriest : CardController {

	public override int Value { 
		get { return VALUE_PRIEST; } 
	}

	public override bool RequiresTarget {
		get { return true; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		List<MoveData> result = new List<MoveData>();
		// The Priest is legal to play against any other player who is not knocked out yet
		foreach(PlayerController target in game.Players) {
			if(target != player && !target.KnockedOut) {
				result.Add(new MoveData(player, this, target));
			}
		}
		return result;
	}

	protected override void SpecificResolve(MoveData move) {
		Debug.Assert(move.Target != move.Player);
		// Learn the target's hand
		move.Player.LearnHand(move.Target);
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(move.Target != null);
		// No point in playing against protected opponents
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		if(move.Target.Protected) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		}
		// The utility of playing a Priest is the measure of the current player's uncertainty regarding the target player's hand,
		// but only if this player will have another turn, otherwise this knowledge is worthless (in which case the utility stays 0)
		if(perceptorData.WillThisPlayerHaveAnotherTurn(move.Player)) {
			float highestProbability = 0;
			float[] TargetHandProbabilities = perceptorData.GetCardProbabilitiesInHand(move.Target);
			for(int i = CardController.VALUE_GUARD; i <= CardController.VALUE_PRINCESS; i++) {
				if(TargetHandProbabilities[i] > highestProbability) {
					highestProbability = TargetHandProbabilities[i];
				}
			}
			// The uncertainty score is the inverse of certainty
			result.Utility = 1f - highestProbability;
		}
		return result;
	}
}
