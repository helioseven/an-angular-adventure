using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileSelectControl : MonoBehaviour {

	//
	private EditGM gm_ref;
	private bool is_active;
	private int active_tile;

	void Start ()
	{
		gm_ref = EditGM.instance;
	}

	void Update ()
	{
		if (active_tile != gm_ref.genesisTile.tileType) updateType();
		if (is_active == gm_ref.editMode) updateActive();
	}

	/* Private Functions */

	//
	private void updateType ()
	{
		transform.GetChild(active_tile).GetComponent<Image>().enabled = false;
		active_tile = gm_ref.genesisTile.tileType;
		if (is_active) transform.GetChild(active_tile).GetComponent<Image>().enabled = true;
	}

	//
	private void updateActive ()
	{
		is_active = !is_active;
		transform.GetChild(active_tile).GetComponent<Image>().enabled = is_active;
	}
}
