using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelectControl : MonoBehaviour {

	// private variables
	private GenesisTile gt_ref;
	private RectTransform rt_ref;
	private int active_color;
	private float start_time;
	private Quaternion rotation_origin;
	private Quaternion rotation_target;

	void Start ()
	{
		gt_ref = EditGM.instance.genesisTile;
		rt_ref = transform.GetChild(0).GetComponent<RectTransform>();
		active_color = 0;
		start_time = 0f;
	}

	void Update ()
	{
		int newColor = gt_ref.tileColor;
		if (active_color != newColor) {
			rt_ref.transform.GetChild(active_color).localScale = Vector3.one;

			start_time = Time.time;
			rotation_origin = rt_ref.transform.rotation;
			rotation_target = Quaternion.Euler(new Vector3(0, 0, -45f * newColor));

			active_color = newColor;
			rt_ref.transform.GetChild(active_color).localScale = Vector3.one * 1.2f;
		}

		float t = Time.time - start_time;
		if (t < 1f) {
			Quaternion q = Quaternion.RotateTowards(rotation_origin, rotation_target, 180 * t);
			rt_ref.transform.rotation = q;
		}
	}
}
