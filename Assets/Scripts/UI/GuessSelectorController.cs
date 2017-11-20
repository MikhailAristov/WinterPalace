using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuessSelectorController : MonoBehaviour, IClickable {

	public int Value;

	public int GetValue() {
		return Value;
	}

	public PlayerController GetOwner() {
		return null;
	}

	public CardController GetCard() {
		return null;
	}

}
