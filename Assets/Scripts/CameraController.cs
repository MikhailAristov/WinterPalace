using System;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public const float VISIBLE_TABLE_WIDTH = 23f;
	public const float VISIBLE_TABLE_HEIGHT = 14f;

	private float adjustedToAspectRatio;

	// Use this for initialization
	void Start() {
		adjustToAspectRatio();
	}

	// Update is called once per frame
	void Update() {
		if(Mathf.Abs(Camera.main.aspect - adjustedToAspectRatio) > 0.01f) {
			adjustToAspectRatio();
		}
	}

	// Adjust the camera's orthographic "zoom" so that the entire table is visible in the current resolution
	private void adjustToAspectRatio() {
		Camera.main.orthographicSize = (float)Math.Round(VISIBLE_TABLE_WIDTH / 2 / Camera.main.aspect, 1);
		adjustedToAspectRatio = Camera.main.aspect;
	}

	// Stores a screenshot
	public void takeScreenshot() {
		string filepath = String.Format("{0}/screenshot_{1:yyyyMMddHHmmssfff}.png", Application.persistentDataPath, System.DateTime.Now);
		ScreenCapture.CaptureScreenshot(filepath);
		Debug.LogFormat("Screenshot saved to {0}.", filepath);
	}
}
