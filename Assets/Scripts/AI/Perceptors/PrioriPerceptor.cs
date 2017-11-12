using System;
using System.Collections.Generic;
using UnityEngine;

public class PrioriPerceptor : AIGenericPerceptor {

	public override bool READY {
		get { return true; } 
	}

	public override bool SomeoneKnowsMyHand {
		get {
			return false;
		}
	}

	public override float GetCardProbabilityInDeck(int CardValue) {
		if(CardValue >= 0 && CardValue < GameController.CARD_COUNT.Length) {
			return ((float)GameController.CARD_COUNT[CardValue] / GameController.TOTAL_CARD_COUNT);
		} else {
			throw new ArgumentOutOfRangeException("CardValue");
		}
	}

	public override float GetCardProbabilityInHand(PlayerController Player, int CardValue) {
		return GetCardProbabilityInDeck(CardValue);
	}

	public override void ResetMemory() {
		// Nothing to do
	}

	public override void RevealHand(PlayerController toPlayer) {
		// Nothing to do
	}

	public override void LearnHand(PlayerController ofPlayer, CardController card) {
		// Nothing to do
	}
}
