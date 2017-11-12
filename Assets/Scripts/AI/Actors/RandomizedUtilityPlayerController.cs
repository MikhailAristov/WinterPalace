using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizedUtilityPlayerController : UtilityPlayerController {

	private const float RELATIVE_CUTOFF_POINT = 0.7f;

	protected override IEnumerator PickAMove() {
		// Wait until the perceptor has finished working
		yield return new WaitUntil(() => MyPerceptor.READY);
		// Get available moves
		List<MoveData> availableMoves = myHand.GetLegalMoves(Game, this);
		availableMoves.AddRange(justDrawn.GetLegalMoves(Game, this));
		// Calculate utilities of every move
		MoveData.DualUtility[] moveUtility = new MoveData.DualUtility[availableMoves.Count];
		MoveData.DualUtility highestUtility = MoveData.DualUtility.MinValue;
		for(int i = 0; i < availableMoves.Count; i++) {
			moveUtility[i] = GetMoveUtility(availableMoves[i]);
			if(moveUtility[i].CompareTo(highestUtility) > 0) {
				highestUtility = moveUtility[i];
			}
		}
		// Calculate cutoff for random selection
		float cutoff = Mathf.Max(0, RELATIVE_CUTOFF_POINT * highestUtility.Utility);
		// Return a weighted random of the resulting moves
		myNextMove = availableMoves[AIUtil.GetWeightedRandom(moveUtility, highestUtility.Rank, cutoff)];
	}
}
