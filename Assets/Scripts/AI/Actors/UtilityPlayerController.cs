using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UtilityPlayerController : AIPlayerController {

	protected const float CONSIDERATION_WEIGHT_MOVE_UTILITY = 1f;
	protected const float CONSIDERATION_WEIGHT_MARGINAL_HAND_VALUE = 0.4f;
	protected const float CONSIDERATION_WEIGHT_OPPONENT_KNOWLEDGE = 0.3f;

	protected const float CONSIDERATION_WEIGHT_TACTICAL_DANGER = 0.3f;
	protected const float CONSIDERATION_WEIGHT_STRATEGIC_DANGER = 0.3f;
	protected const float CONSIDERATION_WEIGHT_GRUDGE = 0.2f;

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

	protected MoveData.DualUtility GetMoveUtility(MoveData move) {
		CardController remainingHand = (move.Card == justDrawn) ? myHand : justDrawn;
		// Get basic move utility
		MoveData.DualUtility moveUtility = move.Card.EstimateMoveUtility(move, remainingHand, MyPerceptor);
		// Get remaining hand value utility
		float marginalHandValueUtility = GetMarginalHandValueUtility(Game, remainingHand.Value);
		// Get the utility of playing the current hand given that the opponent knows it
		float opponentKnowledgeUtility = GetOpponentKnowledgeUtility();
		// Weighted sum of utilities
		MoveData.DualUtility result = new MoveData.DualUtility(moveUtility.Rank, CONSIDERATION_WEIGHT_MOVE_UTILITY * moveUtility.Utility +
		                              CONSIDERATION_WEIGHT_MARGINAL_HAND_VALUE * marginalHandValueUtility +
		                              CONSIDERATION_WEIGHT_OPPONENT_KNOWLEDGE * opponentKnowledgeUtility +
		                              CONSIDERATION_WEIGHT_TACTICAL_DANGER * GetTacticalDanger(move.Target, remainingHand) +
		                              CONSIDERATION_WEIGHT_STRATEGIC_DANGER * GetStrategicDanger(move.Target) +
		                              CONSIDERATION_WEIGHT_GRUDGE * GetGrudgeAgainst(move.Target));
		// Drop the rank on new game to avoid knocking out the player immediately
		if(move.Target == Game.HumanPlayer && !MyPerceptor.HumanPlayerHasActed) {
			result.Rank = Mathf.Min(result.Rank, MoveData.RANK_ONLY_IF_NECESSARY);
		}
		return result;
	}

	public override CardController RevealHand(PlayerController toPlayer) {
		MyPerceptor.RevealHand(toPlayer);
		return myHand;
	}

	protected float GetOpponentKnowledgeUtility() {
		return (MyPerceptor.SomeoneKnowsMyHand ? 1f : 0);
	}

	protected float GetTacticalDanger(PlayerController Player, CardController RemainingHand) {
		if(Player == null || Player == this) {
			return 0;
		}
		float sum = 0;
		foreach(PlayerController p in Game.Players) {
			sum += MyPerceptor.HowManyOthersHasPlayerKnockedOut(p);
		}
		float roundHistoryDanger = (sum > 0) ? ((float)MyPerceptor.HowManyOthersHasPlayerKnockedOut(Player)) / sum : 0;
		float playerKnowledgeDanger = (MyPerceptor.PlayerThinksMyHandIs(Player) == RemainingHand.Value) ? 1f : 0;
		return (roundHistoryDanger + playerKnowledgeDanger) / 2f;
	}

	protected float GetStrategicDanger(PlayerController Player) {
		if(Player == null || Player == this) {
			return 0;
		}
		float sum = 0;
		foreach(PlayerController p in Game.Players) {
			if(p != this) {
				sum += p.HeartCount;
			}
		}
		if(sum > 0) {
			return ((float)Player.HeartCount) / sum;
		} else {
			return 0;
		}
	}

	protected float GetGrudgeAgainst(PlayerController Player) {
		if(Player == null || Player == this) {
			return 0;
		}
		float sum = 0;
		foreach(PlayerController p in Game.Players) {
			sum += MyPerceptor.HowManyOthersHasPlayerKnockedOut(p);
		}
		if(sum > 0) {
			return ((float)MyPerceptor.HowManyOthersHasPlayerKnockedOut(Player)) / sum;
		} else {
			return 0;
		}
	}

	public override void Reset() {
		base.Reset();
		MyPerceptor.ResetMemory();
	}
}
