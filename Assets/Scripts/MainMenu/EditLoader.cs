﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditLoader : MonoBehaviour {

	// short-form pathname for the level to be loaded
	private string path;

	// prefab references
	public GameObject diaTile;
	public GameObject hexTile;
	public GameObject sqrTile;
	public GameObject trapTile;
	public GameObject triTile;
	public GameObject wedTile;

	private GameObject[] tiles;

	void Awake ()
	{
		//
		tiles = new GameObject[6] {triTile, diaTile, trapTile, hexTile, sqrTile, wedTile};

		// filepath of level to be loaded
		// (!!) prompt for string instead
		path = "testLevel.txt";
		DontDestroyOnLoad(gameObject);
		// load Playing scene (PlayGM will call supplyLevel)
		SceneManager.LoadScene(2);
	}

	// supplies a Dictionary of <tile, hexLocus> pairs representing the level
	public void supplyLevel (out Dictionary<GameObject, hexLocus> level)
	{
		// initialization
		level = new Dictionary<GameObject, hexLocus>();
		string[] lines = File.ReadAllLines("Assets\\Levels\\" + path);

		// begin parsing file and building level
		if (lines.Length < 3) {
			Debug.LogError("File could not be read correctly.");
			return;
		}

		// after the first two lines of the file, all remaining lines represent tiles
		for (int i = 2; i < lines.Length; i++) {
			string[] vals = lines[i].Split(new Char[] {' '});
			int j = Int32.Parse(vals[0]);
			int k = Int32.Parse(vals[1]);
			hexLocus hl = new hexLocus(
				Int32.Parse(vals[2]),
				Int32.Parse(vals[3]),
				Int32.Parse(vals[4]),
				Int32.Parse(vals[5]),
				Int32.Parse(vals[6]),
				Int32.Parse(vals[7]));
			int r = Int32.Parse(vals[8]);

			GameObject go = Instantiate(tiles[j], hl.toUnitySpace(), Quaternion.identity) as GameObject;
			Genesis_Tile et = go.GetComponent<Genesis_Tile>();
			for (int c = 0; c < k; c++) et.cycleColor();
			for (int c = 0; c < r; c++) et.rotate(false);
			level.Add(go, hl);
		}

		// terminates this script when done
		Destroy(gameObject);
	}
}