using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using circleXsquares;

public class MenuControl : MonoBehaviour {

	// reference to the main camera in the scene
	private Camera mainCam;

	// position with respect to parent rect transform
	private Vector2 localVec2;
	// reference to parent canvas' rect transform
	private RectTransform canvasRT;
	// reference to this panel's rect transform
	private RectTransform localRT;

	public void Awake () {
		mainCam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
		localVec2 = Vector2.zero;

		// this panel will not initialize properly if not the child of a canvas
		Canvas c = GetComponentInParent<Canvas>();
		if (c) {
			canvasRT = c.transform as RectTransform;
			localRT = transform as RectTransform;
		}

		gameObject.SetActive(false);
	}

	// activates the panel and places it at the given location
	public void activate () {
		Vector2 inVec2 = Vector2.zero;
		// does nothing the panel is already active or has no canvas
		if (gameObject.activeSelf && localRT) return;

		// the passed position is translated into local rect space, and the panel is moved there
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, inVec2, mainCam, out localVec2);
		localRT.localPosition = localVec2;

		gameObject.SetActive(true);
	}

	// deactivates the panel
	public void deactivate () {
		// does nothing the panel is already inactive or has no canvas
		if (!gameObject.activeSelf && localRT) return;

		gameObject.SetActive(false);
	}
}