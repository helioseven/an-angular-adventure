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
		if (active_color != newColor) { // <1>
			rt_ref.transform.GetChild(active_color).localScale = Vector3.one; // <2>

			start_time = Time.time; // <3>
			rotation_origin = rt_ref.transform.rotation;
			rotation_target = Quaternion.Euler(new Vector3(0, 0, -45f * newColor)); // <4>

			active_color = newColor;
			rt_ref.transform.GetChild(active_color).localScale = Vector3.one * 1.2f; // <5>
		}

		float t = Time.time - start_time;
		if (t < 1f) { // <6>
			Quaternion q = Quaternion.RotateTowards(rotation_origin, rotation_target, 180 * t);
			rt_ref.transform.rotation = q; // <7>
		}

		/*
		<1> whenever the genesisTile changes color, this script reacts and updates target
		<2> the current target has its scale reset to one
		<3> start time for transition effect is logged
		<4> target rotations are simply increments of 45 degrees
		<5> the new target has its scale bumped up 20%
		<6> transitions are capped at 1 second in length
		<7> rotation for this frame is calculated and applied
		*/
	}
}
