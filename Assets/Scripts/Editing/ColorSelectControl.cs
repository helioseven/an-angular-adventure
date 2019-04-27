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
		rt_ref = gameObject.GetComponent<RectTransform>();
		active_color = 0;
	}

	void Update ()
	{
		if (active_color != gt_ref.tileColor)
			rt_ref.Rotate(new Vector3(0, 0, 45 * (gt_ref.tileColor - active_color)));
	}
}
