using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BestUtilityPlayerController : UtilityPlayerController {
	
	protected override IEnumerator PickAMove() {
		// Wait until the perceptor has finished working
		yield return new WaitUntil(() => MyPerceptor.READY);
		// Get available moves
		List<MoveData> availableMoves = myHand.GetLegalMoves(Game, this);
		availableMoves.AddRange(justDrawn.GetLegalMoves(Game, this));
		// Find the move wit the highest utility
		MoveData.DualUtility highestUtility = MoveData.DualUtility.MinValue;
		MoveData bestMove = availableMoves[0];
		foreach(MoveData move in availableMoves) {
			MoveData.DualUtility utility = GetMoveUtility(move);
			// Compare and discard
			if(utility.CompareTo(highestUtility) > 0) {
				bestMove = move;
				highestUtility = utility;
			}
		}
		// Return the highest-utility move
		myNextMove = bestMove;
	}
}
