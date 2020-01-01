using UnityEngine;

public class HeartController : MonoBehaviour {

	public const float SIZE = 1f;
	public const float MOVEMENT_THRESHOLD = 0.1f;

	public Vector3 TargetPosition;
	public bool hasStopped;

    private static GameController Game;

	// Use this for initialization
	void Start() {
		// Apply a random rotation around the Z axis
		Vector3 randomRotation = new Vector3(0, 0, UnityEngine.Random.Range(-179f, 180f));
		transform.Rotate(randomRotation);
		// While moving, appear in the foreground
		hasStopped = false;
		GetComponent<SpriteRenderer>().sortingLayerName = "Foreground";
        // Save a reference to the game controller for later
        if(Game == null) {
            Game = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        }
    }

	// Update is called once per frame
	void Update() {
		// The heart are spawned in motion and they don't need any updates after they come to a halt
		if(hasStopped) {
			return;
		}

		// Move towards the target position
		if(Vector2.Distance(transform.localPosition, TargetPosition) > MOVEMENT_THRESHOLD) {
			transform.localPosition = Game.TurboMode ? TargetPosition : Vector3.Lerp(transform.localPosition, TargetPosition, CardController.LERP_FACTOR * Time.deltaTime);
			// Slowly rotate counter-clockwise as you move
			transform.Rotate(new Vector3(0, 0, 1f));
		} else {
			// Upon stopping, move into the background
			GetComponent<SpriteRenderer>().sortingLayerName = "Hearts";
			hasStopped = true;
		}
	}
}
