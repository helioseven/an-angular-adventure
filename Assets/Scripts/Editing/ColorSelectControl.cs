using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelectControl : MonoBehaviour {

	// private variables
	private GenesisTile gt_ref;
	private RectTransform rt_ref;
	private int active_color;

	void Start ()
	{
		gt_ref = EditGM.instance.genesisTile;
		rt_ref = transform.GetChild(0).GetComponent<RectTransform>();
		active_color = 0;
	}

	void Update ()
	{
		int newColor = gt_ref.tileColor;
		if (active_color != newColor) {
			rt_ref.Rotate(new Vector3(0, 0, -45 * (newColor - active_color)));
			active_color = newColor;
		}
	}
}
