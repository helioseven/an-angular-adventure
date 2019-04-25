using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteControl : MonoBehaviour {

	// public read-accessibility state variables
	public bool activeSelf { get { return gameObject.activeSelf; } private set {} }

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
	public void Activate (Vector2 inV2) {
		if (activeSelf) return; // <1>

		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas_rt, inV2, main_cam, out local_position);
		local_rt.localPosition = local_position; // <2>

		gameObject.SetActive(true);

		/*
		<1> does nothing if the panel is already active
		<2> the passed position is translated into local rect space, and the panel is moved there
		*/
	}

	// deactivates the panel
	public void Deactivate () {
		gameObject.SetActive(false);
	}
}
