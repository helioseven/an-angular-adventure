using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInfoControl : MonoBehaviour {

	//
	private EditGM gm_ref;
	private GenesisTile gt_ref;
	private int tile_type;
	private int tile_color;

	void Awake ()
	{
		tile_type = 0;
		tile_color = 0;
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		gt_ref = gm_ref.genesisTile;
	}

	void Update ()
	{
		//
		bool b1 = tile_type != gt_ref.tileType;
		bool b2 = tile_color != gt_ref.tileColor;
		if (b1 || b2) updateDisplay();
	}

	/* Private Functions */

	private void updateDisplay ()
	{
		//
	}
}