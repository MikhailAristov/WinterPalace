using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPlayerController : PlayerController {

	protected const float CARD_PEEK_DURATION = 1f;

	protected bool WaitingForInput = false;
	protected CardController LatestInput = null;
	protected PlayerController LatestInputOwner = null;

	public GameObject GuessSelector;
	protected bool WaitingForGuess = false;
	protected int LatestGuess = 0;

	protected override IEnumerator PickAMove() {
		Debug.Assert(myHand != null);
		Debug.Assert(justDrawn != null);
		// Move the cards apart for better visibility
		myHand.TargetPosition += new Vector3(1.5f, 0, 0);
		justDrawn.TargetPosition -= new Vector3(1.5f, 0, 0);
		// Initialize the target move
		MoveData myMove = new MoveData(this, myHand);
		bool nextMoveIsValid = false;
		CardController RemainingHand = null;
		do {
			// Wait for input of the card to be played next
			WaitingForInput = true;
			LatestInput = null;
			yield return new WaitUntil(() => (LatestInput != null));
			WaitingForInput = false;
			// If the card selected isn't one of ours, try again
			if(LatestInput != myHand && LatestInput != justDrawn) {
				continue;
			}
			// Also check for illegal moves before proceeding
			RemainingHand = (LatestInput == myHand) ? justDrawn : myHand;
			RemainingHand.ResetHighlighting();
			if(CardController.IsKnockOutByPrincess(LatestInput.Value) ||
			   CardController.IsKnockOutByCountess(LatestInput.Value, RemainingHand.Value)) {
				LatestInput.HighlightWithColor(Color.red);
				continue;
			}
			myMove.Card = LatestInput;
			// Now ask for further inputs depending on the type of the color
			if(myMove.Card.RequiresTarget) {
				// Highlight the card
				myMove.Card.HighlightWithColor(Color.green);
				// Wait for input of the target for this card
				WaitingForInput = true;
				LatestInput = null;
				yield return new WaitUntil(() => (LatestInput != null));
				WaitingForInput = false;
				// Set the target
				myMove.Target = LatestInputOwner;
				if(myMove.Card.RequiresTargetHandGuess) {
					// Highlight the targeted player's hand
					LatestInput.HighlightWithColor(Color.blue);
					// Show the guess selector and wait for input
					GuessSelector.SetActive(true);
					WaitingForGuess = true;
					LatestGuess = 0;
					yield return new WaitUntil(() => (LatestGuess > 0));
					WaitingForGuess = false;
					// Set the guess
					myMove.TargetHandGuess = LatestGuess;
					// Hide the guess selector and remove highlighting
					GuessSelector.SetActive(false);
					LatestInput.ResetHighlighting();
				}
				// Reset highlighting
				myMove.Card.ResetHighlighting();
			}
			nextMoveIsValid = true;
		} while(!nextMoveIsValid);
		// Update the next move
		myNextMove = myMove;
		// Move the card that won't be played back to center
		RemainingHand.TargetPosition = Vector2.zero;
	}

	public override void LearnHand(PlayerController owner) {
		waitingForCardPeek = true;
		StartCoroutine(ShowAnothersHand(owner, owner.RevealHand(this)));
	}

	protected IEnumerator ShowAnothersHand(PlayerController owner, CardController card) {
		card.FlipUp();
		float waitUntil = Time.timeSinceLevelLoad + CARD_PEEK_DURATION;
		yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil));
		// Flip it down if the card is still in the other player's hand
		if(card.GetOwner() == owner) {
			card.FlipDown();
		}
		waitingForCardPeek = false;
	}

	public override CardController RevealHand(PlayerController toPlayer) {
		return myHand;
	}

	public override void SwapHand(CardController newCard) {
		myHand.FlipDown();
		newCard.FlipUp();
		base.SwapHand(newCard);
	}

	protected override void postDrawHook() {
		// Flip the card that was just drawn up
		if(justDrawn != null) {
			justDrawn.FlipUp();
		}
	}

	protected override void postMoveSelectHook() {
		return;
	}

	public void InterruptClickOnCard(CardController Card) {
		if(!WaitingForInput) {
			return;
		}
		Debug.Assert(LatestInput == null);
		// Only save the input if it has a defined owner
		LatestInputOwner = Card.GetOwner();
		if(LatestInputOwner != null) {
			LatestInput = Card;
		}
	}

	public void InterruptClickOnGameSelector(GuessSelectorController Guess) {
		if(!WaitingForGuess) {
			return;
		}
		Debug.Assert(LatestGuess == 0);
		LatestGuess = Guess.Value;
	}
}
