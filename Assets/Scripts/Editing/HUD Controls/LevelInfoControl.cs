using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelInfoControl : MonoBehaviour {

	// private variables
	private EditGM gm_ref;
	private Transform tm_ref;
	private string level_name;
	private int active_layer;
	private int layer_count;
	private int layer_tiles;
	private int level_tiles;

	void Awake ()
	{
		level_name = "";
		active_layer = 0;
		layer_count = 1;
		layer_tiles = 0;
		level_tiles = 0;

		updateUI();
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		tm_ref = gm_ref.tileMap.transform;
	}

	void Update ()
	{
		if (level_name != gm_ref.levelName) updateName();
		bool b1 = active_layer != gm_ref.activeLayer;
		bool b2 = layer_count != tm_ref.childCount;
		if (b1 || b2) updateLayers();
		b1 = layer_tiles != tm_ref.GetChild(gm_ref.activeLayer).childCount;
		b2 = level_tiles != getTileCount();
		if (b1 || b2) updateTiles();
	}

	/* Private Functions */

	// updates the name of the level
	private void updateName ()
	{
		level_name = gm_ref.levelName;
		updateUI();
	}

	// updates which is the active layer and the overall layer count
	private void updateLayers ()
	{
		active_layer = gm_ref.activeLayer;
		layer_count = tm_ref.childCount;
		updateUI();
	}

	// updates tile counts for active layer and entire level
	private void updateTiles ()
	{
		layer_tiles = tm_ref.GetChild(gm_ref.activeLayer).childCount;
		level_tiles = getTileCount();
		updateUI();
	}

	// gets a count of all tiles currently in the level
	private int getTileCount ()
	{
		int count = 0;
		foreach (Transform layer in tm_ref) count += layer.childCount;
		return count;
	}

	// updates the text variables inside the relevant UI sub-elements
	private void updateUI ()
	{
		transform.GetChild(0).GetComponent<Text>().text = level_name;
		string s = (active_layer + 1).ToString() + " / " + layer_count.ToString();
		transform.GetChild(2).GetComponent<Text>().text = s;
		s = layer_tiles.ToString() + " (" + level_tiles.ToString() + ")";
		transform.GetChild(4).GetComponent<Text>().text = s;
	}
}