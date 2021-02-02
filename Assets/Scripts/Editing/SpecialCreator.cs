using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

public class SpecialCreator : MonoBehaviour {

	// public variables
	public bool isWarp;

	// public read-accessibility state variables
	public HexOrient specOrient { get; private set; }

	// private variables
	private EditGM gm_ref;
	private SnapCursor anchor_ref;

	void Start ()
	{
		gm_ref = EditGM.instance; // <1>
		anchor_ref = gm_ref.anchorIcon;

		specOrient = new HexOrient();

		/*
		<1> reference to EditGM gives all information needed
		*/
	}

	void Update ()
	{
		HexLocus f = anchor_ref.focus; // <1>
		int r = isWarp ? specOrient.rotation : 0;
		int l = gm_ref.activeLayer;
		specOrient = new HexOrient(f, r, l);

		Quaternion q;
		transform.position = specOrient.ToUnitySpace(out q);
		transform.rotation = q;

		/*
		<1> when active, the special will follow the focus
		*/
	}

	// turns the transform in 30 degree increments
	public void SetRotation (int inRotation)
	{
		if (!isWarp) return;
		specOrient = new HexOrient(specOrient.locus, inRotation, specOrient.layer);
		Update();
	}

	// translates and rotates the transform according to given orientation
	public void SetOrientation (HexOrient inOrient)
	{
		if (!isWarp) inOrient.rotation = 0;
		specOrient = inOrient;
		Update();
	}
}
