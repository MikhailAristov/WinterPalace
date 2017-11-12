using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveData {

	public const int RANK_MUST_PLAY = 1000;
	public const int RANK_PARAMOUNT = 200;
	public const int RANK_NORMAL = 100;
	public const int RANK_ONLY_IF_NECESSARY = 50;
	public const int RANK_BARELY_SENSIBLE = 20;
	public const int RANK_NEVER_EVER = -1000;

	// Who plays the card
	public PlayerController Player;

	// Which card is being played
	public CardController Card;

	// Which other player the card is being played against
	public PlayerController Target;

	// Guard-card only: The guessed value of the target's hand
	public int TargetHandGuess;

	// After resolution: The effect was blocked by an active Handmaid
	public bool NoEffect = false;

	// After resolution: Which player (if any) has been knocked out
	public PlayerController KnockedOut;

	// After resolution: Which card (if any) was discarded in addition to the one that was played
	public CardController AdditionalDiscard;

	// Convenience constructors:
	public MoveData(PlayerController p, CardController c) {
		Player = p;
		Card = c;
	}

	public MoveData(PlayerController p, CardController c, PlayerController t) {
		Player = p;
		Card = c;
		Target = t;
	}

	public MoveData(PlayerController p, CardController c, PlayerController t, int g) {
		Debug.Assert(c.GetType() == typeof(CardGuard));
		Debug.Assert(g > CardController.VALUE_GUARD && g <= CardController.VALUE_PRINCESS);
		Player = p;
		Card = c;
		Target = t;
		TargetHandGuess = g;
	}

	public override string ToString() {
		if(TargetHandGuess > 0) {
			return string.Format("{0} plays a {1} against {2}, guessing \"{3}\"", Player, Card, Target, CardController.NAMES[TargetHandGuess]);
		} else if(Target != null) {
			return string.Format("{0} plays a {1} against {2}", Player, Card, Target);
		} else {
			return string.Format("{0} plays a {1}", Player, Card);
		}
	}

	public struct DualUtility : IComparable<MoveData.DualUtility> {
		
		public static DualUtility MinValue {
			get { return new DualUtility(MoveData.RANK_NEVER_EVER, float.MinValue); }
		}

		public static DualUtility Default {
			get { return new DualUtility(MoveData.RANK_NORMAL, 0); }
		}

		// Dual utility score: Rank
		public int Rank;

		// Dual utility score: Utility
		public float Utility;

		public DualUtility(int r, float u) {
			Rank = r;
			Utility = u;
		}

		public int CompareTo(MoveData.DualUtility that) {
			if(this.Rank.CompareTo(that.Rank) == 0) {
				return this.Utility.CompareTo(that.Utility);
			} else {
				return this.Rank.CompareTo(that.Rank);
			}
		}

		public override string ToString() {
			return string.Format("[{0:00000}] {1:0.000}", Rank, Utility);
		}
	}
}
