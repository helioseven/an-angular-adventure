using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using circleXsquares;

public class MenuControl : MonoBehaviour {

	// reference to the main camera in the scene
	private Camera mainCam;

	// private variables
	private Vector2 localPosition;
	private RectTransform canvasRT;
	private RectTransform localRT;

	public void Awake () {
		mainCam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
		localPosition = Vector2.zero;

		Canvas c = GetComponentInParent<Canvas>();
		if (c) {
			canvasRT = c.transform as RectTransform;
			localRT = transform as RectTransform;
		} else { // <1>
			Debug.LogError("Failed to find the canvas.");
			canvasRT = new RectTransform();
			localRT = new RectTransform();
		}

		gameObject.SetActive(false);

		/*
		<1> this panel will not initialize properly if not the child of a canvas
		*/
	}

	// activates the panel and places it at the given location
	public void activate () {
		Vector2 inVec2 = Vector2.zero;
		if (gameObject.activeSelf && localRT) return; // <1>

		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, inVec2, mainCam, out localPosition);
		localRT.localPosition = localPosition; // <2>

		gameObject.SetActive(true);

		/*
		<1> does nothing the panel is already active or has no canvas
		<2> the passed position is translated into local rect space, and the panel is moved there
		*/
	}

	// deactivates the panel
	public void deactivate () {
		if (!gameObject.activeSelf && localRT) return; // <1>

		gameObject.SetActive(false);

		/*
		<1> does nothing the panel is already inactive or has no canvas
		*/
	}
}