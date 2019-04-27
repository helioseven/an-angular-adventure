using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileSelectControl : MonoBehaviour {

	// private variables
	private EditGM gm_ref;
	private GenesisTile gt_ref;
	private bool is_active;
	private int active_tile;
	private int active_color;

	void Start ()
	{
		gm_ref = EditGM.instance;
		gt_ref = gm_ref.genesisTile;
		is_active = true;
		active_tile = 0;
		active_color = 0;
	}

	void Update ()
	{
		if (active_tile != gt_ref.tileType) updateType();
		if (active_color != gt_ref.tileColor) updateColor();
		if (is_active == gm_ref.editMode) updateActive();
	}

	/* Private Functions */

	// updates active state for current selector
	private void updateActive ()
	{
		is_active = !is_active;
		transform.GetChild(active_tile).GetComponent<Image>().enabled = is_active; // <1>

		/*
		<1> the active selector is only turned on if is_active
		*/
	}

	// updates which selector is active
	private void updateType ()
	{
		transform.GetChild(active_tile).GetComponent<Image>().enabled = false; // <1>
		active_tile = gt_ref.tileType;
		if (is_active) transform.GetChild(active_tile).GetComponent<Image>().enabled = true; // <2>

		/*
		<1> turn off the image renderer for the previous selector
		<2> turn on the image renderer for the newly active selector
		*/
	}

	// updates the color of each selector's tile
	private void updateColor ()
	{
		int newColor = gt_ref.tileColor;
		foreach (Transform selector in transform) {
			Transform t = gt_ref.transform.GetChild(selector.GetSiblingIndex()).GetChild(newColor).GetChild(0); // <1>
			Sprite newSprite = t.GetComponent<SpriteRenderer>().sprite; // <2>
			selector.GetChild(0).GetChild(0).GetComponent<Image>().sprite = newSprite; // <3>
		}

		/*
		<1> gets the appropriate transform in the genesisTile hierarchy
		<2> gets the sprite from that that transform
		<3> assigns that sprite to the appropriate selector
		*/
	}
}