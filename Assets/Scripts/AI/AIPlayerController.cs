public abstract class AIPlayerController : PlayerController {

	public AIGenericPerceptor MyPerceptor;

    protected override void Start() {
        if(MyPerceptor == null || !MyPerceptor.isActiveAndEnabled) {
            MyPerceptor = GetFirstActivePerceptor();
        }
        base.Start();
    }

	public override void SwapHand(CardController newCard) {
		base.SwapHand(newCard);
		MyPerceptor.UpdateOwnHand(newCard, justDrawn);
	}

    /// <summary>
    /// Loops through all perceptors attached to the parent GameObject and returns the first active one, throwing an exception if none is found.
    /// </summary>
    /// <returns>The first active perceptor of the parent GameObject.</returns>
    public AIGenericPerceptor GetFirstActivePerceptor() {
        foreach(AIGenericPerceptor per in gameObject.GetComponents<AIGenericPerceptor>()) {
            if(per.isActiveAndEnabled) {
                return per;
            }
        }
        throw new System.InvalidOperationException(gameObject.name + " has no active perceptors!");
    }
}
