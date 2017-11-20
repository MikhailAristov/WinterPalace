using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPrince : CardController {

	public override int Value { 
		get { return VALUE_PRINCE; } 
	}

	public override bool RequiresTarget {
		get { return true; }
	}

	public override bool RequiresTargetHandGuess {
		get { return false; }
	}

	public override bool CanBePlayedAgainstOneself {
		get { return true; }
	}

	public override List<MoveData> GetLegalMoves(GameController game, PlayerController player) {
		List<MoveData> result = new List<MoveData>();
		// The Prince is legal to play against any player who is not knocked out yet
		// This is also includes the player themselves, in case they want to cycle their hand or every other player is protected by a Handmaid
		foreach(PlayerController target in game.Players) {
			if(!target.KnockedOut) {
				result.Add(new MoveData(player, this, target));
			}
		}
		return result;
	}

	// This card can be played against oneself
	protected override void SpecificResolve(MoveData move) {
		// Check if the player made the target discard the Princess
		CardController tHand = move.Target.GetHand();
		if(tHand.Value == CardController.VALUE_PRINCESS) {
			// For turn history:
			move.KnockedOut = move.Target;
			move.AdditionalDiscard = move.Target.GetHand();
			// If the target discards the Princess, they are knocked out
			Debug.LogFormat("{0} had to discard the {1} and is knocked out!", move.Target, tHand);
			move.Target.KnockOut();
		} else {
			// For turn history:
			move.AdditionalDiscard = move.Target.GetHand();
			// The target must discard their hand and draw a new card
			move.Target.DiscardHand();
			move.Target.DrawNewCard();
		}
	}

	public override MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData) {
		Debug.Assert(move.Card == this);
		Debug.Assert(move.Target != null);
		// No point in playing against protected opponents
		MoveData.DualUtility result = MoveData.DualUtility.Default;
		if(otherCard.Value == CardController.VALUE_COUNTESS) {
			result.Rank = MoveData.RANK_NEVER_EVER;
			return result;
		} else if(move.Target.Protected) {
			result.Rank = MoveData.RANK_BARELY_SENSIBLE;
		}
		// The Prince's utility is the marginal utility of the (estimated) target's hand,
		// compared to the estimate marginal utility of a draw from deck
		float targetHandMarginalUtility = PlayerController.GetMarginalHandValueUtility(move.Player.Game, perceptorData.GetExpectedHandValue(move.Target));
		float deckDrawMarginalUtility = PlayerController.GetMarginalHandValueUtility(move.Player.Game, perceptorData.GetExpectedDeckValue());
		// The resulting utility against opponents and against oneself is reversed
		if(move.Target == move.Player) {
			result.Utility = Mathf.Clamp01(deckDrawMarginalUtility - targetHandMarginalUtility);
			if(otherCard.Value == CardController.VALUE_PRINCESS) {
				result.Rank = MoveData.RANK_NEVER_EVER;
				return result;
			}
		} else {
			result.Utility = Mathf.Clamp01(targetHandMarginalUtility - deckDrawMarginalUtility);
			// If you are certain the other player has the Princess, playing the Prince against them is paramount
			if(perceptorData.GetCertainHandValue(move.Target) == CardController.VALUE_PRINCESS) {
				result.Rank = MoveData.RANK_PARAMOUNT;
			}
		}
		return result;
	}

}
