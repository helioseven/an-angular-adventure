using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteControl : MonoBehaviour {

	// private variables
	private Camera main_cam;
	private Vector2 local_position;
	private RectTransform canvas_rt;
	private RectTransform local_rt;

	public void Awake () {
		main_cam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
		local_position = Vector2.zero;

		Canvas c = GetComponentInParent<Canvas>();
		if (c) {
			canvas_rt = c.transform as RectTransform;
			local_rt = transform as RectTransform;
		} else { // <1>
			Debug.LogError("Failed to find the canvas.");
			canvas_rt = new RectTransform();
			local_rt = new RectTransform();
		}

		gameObject.SetActive(false);

		/*
		<1> this panel will not initialize properly if not the child of a canvas
		*/
	}

	// activates the panel and places it at the given location
	public void TogglePalette () {
		if (gameObject.activeSelf) gameObject.SetActive(false); // <1>
		else {
			Vector2 lp, mP = Input.mousePosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas_rt, mP, main_cam, out lp);
			local_position = lp;
			local_rt.localPosition = local_position; // <2>

			gameObject.SetActive(true);
		}

		/*
		<1> if the panel is already active, deactivate it
		<2> otherwise, the mouse position is translated into local rect space and the panel is moved there
		*/
	}
}
