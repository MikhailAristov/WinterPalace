using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardGuard : CardController {

	private const float REASONABLE_DOUBT_CUTOFF = 1f / GameController.TOTAL_CARD_COUNT;

	public override int Value { 
		get { return VALUE_GUARD; } 
	}

	public override bool RequiresTarget {
		get { return true; }
	}

	public override bool RequiresTargetHandGuess {
		get { return true; }
	}

	public override bool CanBePlayedAgainstOneself {
		get { return false; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		List<MoveData> result = new List<MoveData>();
		// The Guard is legal to play against any other player who is not knocked out yet
		foreach(PlayerController target in game.Players) {
			if(target != player && !target.KnockedOut) {
				// Any guess between 2 (Priest) and 8 (Princess) is legal (guessing another Guard is not)
				for(int valueGuess = VALUE_PRIEST; valueGuess <= VALUE_PRINCESS; valueGuess++) {
					result.Add(new MoveData(player, this, target, valueGuess));
				}
			}
		}
		return result;
	}

	protected override void SpecificResolve(MoveData move) {
		Debug.Assert(move.Target != move.Player);
		// Check the target's hand
		CardController tHand = move.Target.GetHand();
		if(tHand.Value == move.TargetHandGuess) {
			// For turn history:
			move.KnockedOut = move.Target;
			move.AdditionalDiscard = move.Target.GetHand();
			// On a correct guess, the target is knocked out
			Debug.LogFormat("{0} has correctly guessed that {1} has a {2}! {1} is knocked out!", move.Player, move.Target, tHand);
			move.Target.KnockOut();
		} else {
			Debug.LogFormat("{0} has guessed \"{1}\" but that is incorrect, so nothing happens!", move.Player, CardController.NAMES[move.TargetHandGuess]);
		}
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(move.Target != null);
		Debug.Assert(move.TargetHandGuess >= VALUE_PRIEST && move.TargetHandGuess <= VALUE_PRINCESS);
		// No point in playing against protected opponents, or when the likelihoood of guessing right is too low
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		float LikelihoodOfCorrectGuess = perceptorData.GetCardProbabilityInHand(move.Target, move.TargetHandGuess);
		if(LikelihoodOfCorrectGuess < REASONABLE_DOUBT_CUTOFF || perceptorData.GetRevealedCardCount(move.TargetHandGuess) >= GameController.CARD_COUNT[move.TargetHandGuess]) {
			result.Rank = MoveData.RANK_NEVER_EVER;
			return result;
		} else if(move.Target.Protected) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		} else if(perceptorData.GetCertainHandValue(move.Target) == move.TargetHandGuess) {
			result.Rank = MoveData.RANK_PARAMOUNT;
		}
		// Utility of playing a Guard is just the certainty that the other player has the card you were going to guess
		result.Utility = LikelihoodOfCorrectGuess;
		return result;
	}

}
