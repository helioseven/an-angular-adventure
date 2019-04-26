using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelInfoControl : MonoBehaviour {

	// public read-accessibility state variables
	public string levelName { get; private set; }
	public int activeLayer { get; private set; }
	public int layerCount { get; private set; }
	public int layerTiles { get; private set; }
	public int levelTiles { get; private set; }

	// private variables
	private Transform tileMap;

	void Awake ()
	{
		levelName = "N/A";
		activeLayer = 0;
		layerCount = 1;
		layerTiles = 0;
		levelTiles = 0;

		updateUI();
	}

	// initialization function, to be called by EditLoader
	public void LoadInfo (string inName, int inCount)
	{
		levelName = inName;
		layerCount = inCount;

		tileMap = EditGM.instance.tileMap.transform;
		foreach (Transform t in tileMap) levelTiles += t.childCount;

		SetActiveLayer(0);
	}

	// adds one to layer count
	public void AddLayer ()
	{
		layerCount++;
		updateUI();
	}

	// sets the active layer and updates info accordingly
	public void SetActiveLayer (int inLayer)
	{
		if ((inLayer < 0) || (inLayer >= layerCount)) return; // <1>
		activeLayer = inLayer;
		layerTiles = tileMap.GetChild(activeLayer).childCount; // <2>

		updateUI();

		/*
		<1> if invalid layerIndex is given, fail quietly
		<2> update tile count for this layer by counting tileMap layer's children
		*/
	}

	// adds one to both the layer tile count and the level tile count
	public void AddTile ()
	{
		layerTiles++;
		levelTiles++;
		updateUI();
	}

	// removes one from both the layer tile count and the level tile count
	public void RemoveTile ()
	{
		layerTiles--;
		levelTiles--;
		updateUI();
	}

	/* Private Functions */

	// updates the text variables inside the relevant UI sub-elements
	private void updateUI ()
	{
		transform.GetChild(0).GetComponent<Text>().text = levelName;
		string s = (activeLayer + 1).ToString() + " / " + layerCount.ToString();
		transform.GetChild(3).GetComponent<Text>().text = s;
		s = layerTiles.ToString() + " (" + levelTiles.ToString() + ")";
		transform.GetChild(4).GetComponent<Text>().text = s;
	}
}