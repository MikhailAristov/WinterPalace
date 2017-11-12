﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaivePosterioriPerceptor : AIGenericPerceptor {

	protected DiscardController Discard;

	public override bool READY {
		get { return true; } 
	}

	public override bool SomeoneKnowsMyHand {
		get {
			return false;
		}
	}

	void Update() {
		if(Discard == null && MyController.Game != null) {
			Discard = MyController.Game.DiscardPile;
		}
	}

	public override float GetCardProbabilityInDeck(int CardValue) {
		Debug.Assert(myHand != null);
		Debug.Assert(Discard != null);
		// Check the stuff
		if(CardValue >= CardController.VALUE_GUARD && CardValue <= CardController.VALUE_PRINCESS) {
			return ((float)GameController.CARD_COUNT[CardValue] - GetRevealedCardCount(CardValue)) / 
				(GameController.TOTAL_CARD_COUNT - GetCompleteRevealedCardCount());
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
