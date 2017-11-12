using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour {

	public string DisplayName;
	public bool Protected;
	public bool KnockedOut;
	public int TotalDiscardedValue;
	public int HeartCount;

	public GameController Game;
	public int SittingOrder;

	public Transform Hand;
	public Transform Front;
	public Transform HeartContainer;
	public RectTransform SpeechOutput;

	protected CardController myHand;
	protected CardController justDrawn;
	protected MoveData myNextMove;

	public bool IsBusy {
		get { return (drawingANewHand || takingMyTurn || waitingForTheDraw || waitingForCardPeek); }
	}
	protected bool drawingANewHand;
	protected bool takingMyTurn;
	protected bool waitingForTheDraw;
	protected bool waitingForCardPeek;

	// Use this for initialization
	protected void Start() {
		Reset();
		HeartCount = 0;
		// If I am flipped upside down, move the heart container to the (global) left of me, rather than the right
		if(transform.localEulerAngles.z != 0) {
			HeartContainer.Translate(-2f * HeartContainer.localPosition.x, 0, 0);
		}
	}

	public void DrawNewCard() {
		Debug.Assert(Game != null);
		Debug.Assert(myHand == null);
		Debug.Assert(justDrawn == null);
		// Keep it simple stupid
		drawingANewHand = true;
		StartCoroutine(drawToEmptyHand());
	}

	protected IEnumerator drawToEmptyHand() {
		Debug.Assert(myHand == null);
		// Draw a card and wait for the animation to finish
		waitingForTheDraw = true;
		StartCoroutine(drawCard());
		yield return new WaitUntil(() => !waitingForTheDraw);
		// Move the card just drawn to the hand
		myHand = justDrawn;
		justDrawn = null;
		// Call addition operations if necessary
		postDrawHook();
		// Return
		drawingANewHand = false;
	}

	protected IEnumerator drawCard() {
		// Draw a card from the deck
		justDrawn = Game.Deck.Draw();
		// Call addition operations if necessary
		postDrawHook();
		// Move the card to my hand
		justDrawn.MoveTo(Hand.transform);
		// Wait until card stops
		yield return new WaitUntil(() => (!justDrawn.isInMotion));
		waitingForTheDraw = false;
	}

	protected abstract void postDrawHook();

	protected abstract void postMoveSelectHook();

	public void NextTurn() {
		Debug.Assert(Game != null);
		Debug.Assert(myHand != null);
		Debug.Assert(justDrawn == null);
		// Reset protection, if necessary
		if(Protected) {
			Game.DiscardPile.Put(Front.GetComponentInChildren<CardHandmaid>());
			Protected = false;
		}
		// Keep it simple stupid
		takingMyTurn = true;
		StartCoroutine(takeATurn());
	}

	protected IEnumerator takeATurn() {
		// Draw a card and wait for the animation to finish
		waitingForTheDraw = true;
		StartCoroutine(drawCard());
		yield return new WaitUntil(() => (!waitingForTheDraw));
		// Also wait for the perceptor evaluation, if any
		if(Game.PerceptorEvaluator != null) {
			yield return new WaitUntil(() => Game.PerceptorEvaluator.READY);
		}
		// Pick a move with eithercard from the hand
		myNextMove = null;
		StartCoroutine(PickAMove());
		yield return new WaitUntil(() => (myNextMove != null));
		Debug.LogFormat("{0}!", myNextMove);
		if(myNextMove.Card == justDrawn) {
			// Wait for the card to stop moving
			yield return new WaitUntil(() => (!justDrawn.isInMotion));
		} else {
			myHand = justDrawn;
		}
		justDrawn = null;
		postMoveSelectHook();
		// Flip the card
		myNextMove.Card.FlipUp();
		bool keepInFront = false;
		// If the card has a target, point it towards said target
		if(myNextMove.Target != null) {
			// Take the vector towards the Front position and rotate it towards the target player
			myNextMove.Card.MoveTo(transform);
			Vector2 toTarget = transform.InverseTransformPoint(myNextMove.Target.transform.position);
			Vector2 toFront = transform.InverseTransformPoint(Front.transform.position);
			myNextMove.Card.TargetPosition = toTarget.normalized * toFront.magnitude;
			myNextMove.Card.TargetRotation = Quaternion.FromToRotation(toFront, toTarget);
			// If this is a Guard and a guess has to be specified, also say the guess
			if(myNextMove.TargetHandGuess > 0) {
				Say(string.Format("{0}?", CardController.NAMES[myNextMove.TargetHandGuess]));
			}
		} else {
			// The Handmaid moves to the front of the player instead
			if(myNextMove.Card.GetType() == typeof(CardHandmaid)) {
				myNextMove.Card.MoveTo(Front.transform);
				keepInFront = true;
			}
		}
		// Wait for the card stops moving
		yield return new WaitUntil(() => (!myNextMove.Card.isInMotion));
		ShutUp();
		// Resolve the actual move
		myNextMove.Card.Resolve(myNextMove);
		TotalDiscardedValue += myNextMove.Card.Value;
		// Discard the played card and wait until it's there before ending the turn (unless it's a Handmaid, who stays)
		if(!keepInFront) {
			Game.DiscardPile.Put(myNextMove.Card);
			yield return new WaitUntil(() => (myNextMove.Card.transform.position.x > DiscardController.HORIZONTAL_THRESHOLD));
		}
		// Add the turn to turn history
		Game.TurnHistory.Add(myNextMove);
		// End turn
		takingMyTurn = false;
	}

	// This is the AI's domain...
	protected abstract IEnumerator PickAMove();

	public void Say(string text) {
		Debug.Assert(SpeechOutput != null);
		// Ensure the output is always rotated to match the screen
		SpeechOutput.rotation = Quaternion.identity;
		// Set the text contents
		SpeechOutput.gameObject.GetComponentInChildren<UnityEngine.UI.Text>().text = text;
		// Set the object active
		SpeechOutput.gameObject.SetActive(true);
	}

	public void ShutUp() {
		Debug.Assert(SpeechOutput != null);
		SpeechOutput.gameObject.SetActive(false);
	}

	public void DiscardHand() {
		Debug.LogFormat("{0} discards the {1}.", this, myHand);
		Game.DiscardPile.Put(myHand);
		myHand = null;
	}

	public virtual void SwapHand(CardController newCard) {
		Debug.LogFormat("{0} swaps {1} for a {2}.", this, myHand, newCard);
		myHand = newCard;
		myHand.MoveTo(Hand.transform);
	}

	public CardController GetHand() {
		return ((myHand != null) ? myHand : justDrawn);
	}

	public abstract CardController RevealHand(PlayerController toPlayer);

	public abstract void LearnHand(PlayerController owner);

	public void KnockOut() {
		Debug.LogFormat("{0} is out of the round!", this);
		DiscardHand();
		KnockedOut = true;
	}

	public virtual void Reset() {
		// Send all cards back into the deck
		foreach(CardController cc in GetComponentsInChildren<CardController>()) {
			cc.MoveTo(Game.Deck.transform);
		}
		// Remove references to cards
		myHand = null;
		justDrawn = null;
		myNextMove = null;
		// Reset the gameplay parameters
		drawingANewHand = false;
		takingMyTurn = false;
		waitingForTheDraw = false;
		Protected = false;
		KnockedOut = false;
		TotalDiscardedValue = 0;
	}

	public static float GetMarginalHandValueUtility(GameController Game, float ExpectedHandValue) {
		Debug.Assert(Game != null);
		Debug.Assert(ExpectedHandValue >= CardController.VALUE_GUARD);
		// Own hand value
		float handValueUtility = ExpectedHandValue / CardController.VALUE_PRINCESS;
		// The initial draw is disregarded
		float deckSizeAfterInitialDraw = GameController.TOTAL_CARD_COUNT - Game.Players.Length;
		float urgencyToKeepHighCards = (deckSizeAfterInitialDraw - Game.Deck.CountCardsLeft) / (deckSizeAfterInitialDraw - 1);
		// Return the absolute difference (fuzzy XOR) between the two
		return handValueUtility * urgencyToKeepHighCards;
	}

	public override string ToString() {
		return DisplayName;
	}
}
