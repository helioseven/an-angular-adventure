using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

public class SpecialCreator : MonoBehaviour {

	// public read-accessibility state variables
	private EditGM gm_ref;
	private SnapCursor anchor_ref;

	void Start ()
	{
		gm_ref = EditGM.instance; // <1>

		/*
		<1> reference to EditGM gives all information needed
		*/
	}

	void Update ()
	{
		Vector3 v3 = gm_ref.anchorIcon.focus.ToUnitySpace(); // <1>
		v3.z = gm_ref.GetLayerDepth(gm_ref.activeLayer); // <2>
		transform.position = v3;

		/*
		<1> when active, the special creator will follow the focus
		<2> get our Z-depth from the active layer
		*/
	}
}