using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuessSelectorController : MonoBehaviour {

	public GameController Game;
	public int Value;

	void OnMouseDown() {
		if(Game.HumanPlayer != null) {
			Game.HumanPlayer.InterruptClickOnGameSelector(this);
		}
	}
}
