using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIPlayerController : PlayerController {

	public AIGenericPerceptor MyPerceptor;

	public override void SwapHand(CardController newCard) {
		base.SwapHand(newCard);
		MyPerceptor.UpdateOwnHand(newCard, justDrawn);
	}
}
