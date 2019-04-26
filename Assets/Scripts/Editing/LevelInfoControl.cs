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

	//
	public void LoadInfo (string inName, int inCount)
	{
		levelName = inName;
		layerCount = inCount;

		tileMap = EditGM.instance.tileMap.transform;
		foreach (Transform t in tileMap) levelTiles += t.childCount;

		SetActiveLayer(0);
	}

	//
	public void AddLayer ()
	{
		layerCount++;
		updateUI();
	}

	//
	public void SetActiveLayer (int inLayer)
	{
		if ((inLayer < 0) || (inLayer >= layerCount)) return;
		activeLayer = inLayer;
		layerTiles = tileMap.GetChild(activeLayer).childCount;

		updateUI();
	}

	//
	public void AddTile ()
	{
		layerTiles++;
		levelTiles++;
		updateUI();
	}

	//
	public void RemoveTile ()
	{
		layerTiles--;
		levelTiles--;
		updateUI();
	}

	/* Private Functions */

	//
	private void updateUI ()
	{
		transform.GetChild(0).GetComponent<Text>().text = levelName;
		string s = (activeLayer + 1).ToString() + " / " + layerCount.ToString();
		transform.GetChild(3).GetComponent<Text>().text = s;
		s = layerTiles.ToString() + " (" + levelTiles.ToString() + ")";
		transform.GetChild(4).GetComponent<Text>().text = s;
	}
}