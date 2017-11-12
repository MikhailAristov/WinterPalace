using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardController : MonoBehaviour {

	public const float VERTICAL_SPACING = 1.4f;
	public const float HORIZONTAL_SPACING = 0.5f;
	// Global X-coordinate beyond which the card counts as in discard pile
	public const float HORIZONTAL_THRESHOLD = 6.5f;

	public int[] CardCount;
	public int TotalCount;

	// Use this for initialization
	void Start() {
		// There are eight different cards in the game
		CardCount = new int[CardController.VALUE_PRINCESS + 1];
		Reset();
	}

	// Puts the specified card onto the discard pile
	public void Put(CardController card) {
		Debug.LogFormat("{0} comes onto the discard pile...", card);
		// Flip the card if it's not already flipped
		if(!card.FaceUp) {
			card.FlipUp();
		}
		// Move the card to my hand
		card.transform.SetParent(transform);
		card.TargetPosition = getNextCardPosition(card.Value);
		card.TargetRotation = Quaternion.identity;
		// Move the card to the proper height immediately
		card.FrontSide.sortingOrder = card.Value * 10 + CardCount[card.Value];
		card.transform.localPosition = new Vector3(card.transform.localPosition.x, card.transform.localPosition.y, getZAxisOffset(card.Value));
		// Update the counter
		CardCount[card.Value] += 1;
		TotalCount += 1;
	}

	// Calculate the placement of the next card of a given value
	private Vector3 getNextCardPosition(int CardValue) {
		Vector3 result = Vector3.zero;
		result.x = VERTICAL_SPACING * ((float)CardValue - 4.5f);
		result.y = -HORIZONTAL_SPACING * CardCount[CardValue];
		result.z = getZAxisOffset(CardValue);
		return result;
	}

	private float getZAxisOffset(int CardValue) {
		return (-CardController.THICKNESS * CardCount[CardValue]);
	}

	public void Reset() {
		Array.Clear(CardCount, 0, CardCount.Length);
		TotalCount = 0;
	}
}
