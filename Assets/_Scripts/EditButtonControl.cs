using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditButtonControl : MonoBehaviour {

	private Image image;

	// finds the image associated with, and adds a listener to, the button
	void Awake ()
	{
		image = gameObject.GetComponent<Image>();

		gameObject.GetComponent<Button>().onClick.AddListener(toggleColor);
	}

	// button doesn't even track it's own state, just asks the EditGM
	public void toggleColor ()
	{
		// bEdit button is 75% grey in creation mode, 100% white in edit mode
		image.color = EditGM.instance.menuMode ?
			new Color(0.75f, 0.75f, 0.75f, 1.0f) : new Color(1.0f, 1.0f, 1.0f, 1.0f);
	}
}
