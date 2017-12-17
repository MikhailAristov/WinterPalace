using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardController : MonoBehaviour, IClickable {

	public const int VALUE_GUARD = 1;
	public const int VALUE_PRIEST = 2;
	public const int VALUE_BARON = 3;
	public const int VALUE_HANDMAID = 4;
	public const int VALUE_PRINCE = 5;
	public const int VALUE_KING = 6;
	public const int VALUE_COUNTESS = 7;
	public const int VALUE_PRINCESS = 8;

	public static string[] NAMES = { "unknown", "Guard", "Priest", "Baron", "Freylina", "Knyaz", "Tsar", "Tsaritsa", "Tsarevna" };

	public string DisplayName {
		get { return NAMES[Value]; }
	}
	public abstract int Value { get; }
	public abstract bool RequiresTarget { get; }
	public abstract bool RequiresTargetHandGuess { get; }
	public abstract bool CanBePlayedAgainstOneself { get; }
	public bool FaceUp;

	public Vector3 TargetPosition;
	public Quaternion TargetRotation;

	public SpriteRenderer FrontSide;
	public SpriteRenderer BackSide;

	public const float HEIGHT = 4.2f;
	public const float WIDTH = 3f;
	public const float THICKNESS = 0.01f;

	public const float MOVEMENT_THRESHOLD = 0.1f;
	public const float ROTATION_THRESHOLD = 1f;
	public static float LERP_FACTOR = 2f;

	protected bool isMoving;
	protected bool isRotating;

	public bool isInMotion {
		get { return (isMoving || isRotating); }
	}

	protected void Start() {
		isMoving = false;
		isRotating = false;
	}
	
	// Update is called once per frame
	protected void Update() {
		// Flip the card over if necessary
		if(FaceUp ^ FrontSide.enabled) {
			FrontSide.enabled = FaceUp;
		}
		// Move the card to its destination if necessary
		if(Vector3.Distance(transform.localPosition, TargetPosition) > MOVEMENT_THRESHOLD) {
			transform.localPosition = Vector3.Lerp(transform.localPosition, TargetPosition, LERP_FACTOR * Time.deltaTime);
			if(!isMoving) {
				LiftUp();
				isMoving = true;
			}
		} else if(isMoving) {
			LowerDown();
			isMoving = false;
		}

		// Rotate the card if necessary
		if(Quaternion.Angle(transform.localRotation, TargetRotation) > ROTATION_THRESHOLD) {
			transform.localRotation = Quaternion.Slerp(transform.localRotation, TargetRotation, LERP_FACTOR * Time.deltaTime);
			isRotating = true;
		} else if(isRotating) {
			isRotating = false;
		}
	}

	// Flip the card face up
	public void FlipUp() {
		FaceUp = true;
	}

	// Ditton in reverse
	public void FlipDown() {
		FaceUp = false;
	}

	// During movement, update the sorting layer priority so it appears above all other cards
	protected void LiftUp() {
		if(!FaceUp && BackSide.sortingOrder < 100) {
			BackSide.sortingOrder = 200 - BackSide.sortingOrder;
		}
	}

	// Ditto in reverse
	protected void LowerDown() {
		if(!FaceUp && BackSide.sortingOrder > 100) {
			BackSide.sortingOrder = 200 - BackSide.sortingOrder;
		}
	}

	// Chances the card's target position/rotation to match a new parent object
	public void MoveTo(Transform newParent) {
		LiftUp();
		isMoving = true;
		transform.SetParent(newParent);
		TargetPosition = Vector3.zero;
		TargetRotation = Quaternion.identity;
	}

	public override string ToString() {
		return string.Format("{0} ({1})", DisplayName, Value);
	}

	public abstract List<MoveData> GetLegalMoves(GameController game, PlayerController player);

	public void Resolve(MoveData move) {
		// Check whether the Countess must-play rule has been violated
		if(IsKnockOutByCountess(move.Card.Value, move.Player.GetHand().Value)) {
			Debug.LogFormat("{0} has played a {1} even though they had a Countess in hand -- knocked out for cheating!", move.Player, move.Card);
			// For turn history:
			move.KnockedOut = move.Player;
			move.AdditionalDiscard = move.Player.GetHand();
			move.Player.KnockOut();
			return;
		}
		// Check protection
		if(move.Target != null && move.Target.Protected) {
			Debug.LogFormat("{0} is protected by a Handmaid, so nothing happens!", move.Target);
			move.NoEffect = true;
			return;
		}
		// Specific card resolution implemented in the respective class
		SpecificResolve(move);
	}

	protected abstract void SpecificResolve(MoveData move);

	public static bool IsKnockOutByPrincess(int PlayedCardValue, int OtherCardValue, bool TargetIsMyself) {
		return (PlayedCardValue == CardController.VALUE_PRINCESS ||
			(TargetIsMyself && PlayedCardValue == CardController.VALUE_PRINCE && OtherCardValue == CardController.VALUE_PRINCESS));
	}

	public static bool IsKnockOutByCountess(int playedCardValue, int otherCardValue) {
		return (otherCardValue == CardController.VALUE_COUNTESS && 
			(playedCardValue == CardController.VALUE_PRINCE || playedCardValue == CardController.VALUE_KING));
	}

	public abstract MoveData.DualUtility EstimateMoveUtility(MoveData move, CardController otherCard, AIGenericPerceptor perceptorData);

	public int GetValue() {
		return Value;
	}

	public PlayerController GetOwner() {
		if(transform.parent.name == "Hand") {
			return transform.parent.GetComponentInParent<PlayerController>();
		} else {
			Debug.LogWarningFormat("{0} is not held by any player! Parent: {1}", this, transform.parent);
			return null;
		}
	}

	public CardController GetCard() {
		return this;
	}

	public void HighlightWithColor(Color Highlight) {
		FrontSide.color = Highlight;
		BackSide.color = Highlight;
	}

	public void ResetHighlighting() {
		HighlightWithColor(Color.white);
	}
}
