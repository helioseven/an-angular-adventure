using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class LevelInfoControl : MonoBehaviour {

	// private variables
	private EditGM gm_ref;
	private Transform tm_ref;
	private Text name_display;
	private Text layers_display;
	private Text tiles_display;
	private Text anchor_display;

	private string level_name;
	private int active_layer;
	private int layer_count;
	private int layer_tiles;
	private int level_tiles;
	private HexLocus anchor_locus;

	void Awake ()
	{
		level_name = "";
		active_layer = 0;
		layer_count = 1;
		layer_tiles = 0;
		level_tiles = 0;
		anchor_locus = new HexLocus();
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		tm_ref = gm_ref.tileMap.transform;

		name_display = transform.GetChild(0).GetComponent<Text>();
		Transform t = transform.GetChild(1);
		layers_display = t.GetChild(1).GetComponent<Text>();
		tiles_display = t.GetChild(3).GetComponent<Text>();
		anchor_display = t.GetChild(5).GetComponent<Text>();
	}

	void Update ()
	{
		bool b = false;

		/*
		if (level_name != gm_ref.levelName) {
			level_name = gm_ref.levelName;
			b = true;
		}
		*/
		int al = gm_ref.activeLayer;
		if (active_layer != al) {
			active_layer = al;
			b = true;
		}
		if (layer_count != tm_ref.childCount) {
			layer_count = tm_ref.childCount;
			b = true;
		}
		if (layer_tiles != tm_ref.GetChild(al).childCount) {
			layer_tiles = tm_ref.GetChild(al).childCount;
			b = true;
		}
		if (level_tiles != getTileCount()) {
			level_tiles = getTileCount();
			b = true;
		}
		if (anchor_locus != gm_ref.anchorIcon.anchor) {
			anchor_locus = gm_ref.anchorIcon.anchor;
			b = true;
		}

		if (b) updateUI();
	}

	/* Public Functions */

	// updates the text variables inside the relevant UI sub-elements
	public void updateUI ()
	{
		name_display.text = level_name;

		string s = (active_layer + 1).ToString() + " / " + layer_count.ToString();
		layers_display.text = s;

		s = layer_tiles.ToString() + " (" + level_tiles.ToString() + ")";
		tiles_display.text = s;

		anchor_display.text = anchor_locus.PrettyPrint();
	}

	/* Private Functions */

	// gets a count of all tiles currently in the level
	private int getTileCount ()
	{
		int count = 0;
		foreach (Transform layer in tm_ref) count += layer.childCount;
		return count;
	}
}
