using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckController : MonoBehaviour {

	private Stack<CardController> CARDS;

	private int initialDeckSize;
	private int[] CardCount;
	private const float VERTICAL_SHIFT = CardController.WIDTH / 15;

	public int CountCardsDrawn {
		get { return initialDeckSize - CARDS.Count; }
	}

	public int CountCardsLeft {
		get { return CARDS.Count; }
	}

	// For benchmarking
	public int TopCardValue {
		get { return (CARDS.Count > 0) ? CARDS.Peek().Value : 0;}
	}

	// Use this for initialization
	void Start() {
		CARDS = new Stack<CardController>();
		CardCount = new int[CardController.VALUE_PRINCESS + 1];
	}

	// Shuffles the deck contents
	public void Shuffle() {
		CARDS.Clear();
		Array.Clear(CardCount, 0, CardCount.Length);
		// Get all card objects currently in the deck
		CardController[] cardControllers = GetComponentsInChildren<CardController>();
		initialDeckSize = cardControllers.Length;
		Debug.Assert(initialDeckSize == GameController.TOTAL_CARD_COUNT);
		// Shuffle the objects in the array randomly
		int swapWith;
		CardController tempCC;
		for(int i = 0; i < cardControllers.Length; i++) {
			// Pick a random element from the unsorted deck
			swapWith = UnityEngine.Random.Range(i, cardControllers.Length);
			// Swap the picked element with the current one in the loop
			tempCC = cardControllers[swapWith];
			cardControllers[swapWith] = cardControllers[i];
			cardControllers[i] = tempCC;
			// Feed the shuffled cards into the main stack of cards
			CARDS.Push(cardControllers[i]);
			CardCount[cardControllers[i].Value] += 1;
			// Update the sorting order of the sprites correspondingly
			cardControllers[i].BackSide.sortingOrder = i - initialDeckSize;
			cardControllers[i].FrontSide.sortingOrder = cardControllers[i].Value * 20 + i;
			// Flip the card down, just in case
			cardControllers[i].FlipDown();
			// Place the cards slighly apart to make it visible how many remain
			cardControllers[i].MoveTo(transform);
			cardControllers[i].TargetPosition = new Vector2(VERTICAL_SHIFT * (i - 6), 0);
		}
	}

	// Gets the next card from the stack
	public CardController Draw() {
		CardController topCard = CARDS.Pop();
		CardCount[topCard.Value] -= 1;
		return topCard;
	}

	// Get remaining card distribution
	public float[] GetCardDistribution() {
		float[] result = new float[CardCount.Length];
		if(CARDS.Count > 0) {
			for(int i = 0; i < CardCount.Length; i++) {
				result[i] = CardCount[i] / CARDS.Count;
			}
		}
		return result;
	}
}
