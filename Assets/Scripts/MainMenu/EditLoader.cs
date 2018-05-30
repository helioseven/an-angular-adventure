using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditLoader : MonoBehaviour {

	// short-form pathname for the level to be loaded
	private string path;
	// (??)
	private string rawString;
	public bool isFileFound { get; private set; }

	private GenesisTile gt;

	void Awake ()
	{
		// filepath of level to be loaded
		// (!!) prompt for string instead
//		path = "testLevel.txt";
		path = "http://ada.evergreen.edu/~bardre09/sosProject/testLevel.txt";
		isFileFound = false;
		StartCoroutine(getLevelData());

		DontDestroyOnLoad(gameObject);
		// load Playing scene (PlayGM will call supplyLevel)
		SceneManager.LoadScene(2);
	}

	// supplies a Dictionary of <tile, tileData> pairs representing the level
	public void supplyLevel (out Dictionary<GameObject, tileData> level)
	{
		// initialization
		gt = EditGM.instance.genesisTile;
		level = new Dictionary<GameObject, tileData>();
//		string[] lines = File.ReadAllLines("Assets\\Levels\\" + path);
		string[] lines = rawString.Split(new Char[] {'\n'});

		// begin parsing file and building level
		if (lines.Length < 3) {
			Debug.LogError("File could not be read correctly.");
			return;
		}

		// after the first two lines of the file, all remaining lines represent tiles
		for (int i = 2; i < lines.Length - 1; i++) {
			string[] vals = lines[i].Split(new Char[] {' '});
			int t = Int32.Parse(vals[0]);
			int c = Int32.Parse(vals[1]);
			hexLocus hl = new hexLocus(
				Int32.Parse(vals[2]),
				Int32.Parse(vals[3]),
				Int32.Parse(vals[4]),
				Int32.Parse(vals[5]),
				Int32.Parse(vals[6]),
				Int32.Parse(vals[7]));
			int r = Int32.Parse(vals[8]);

			tileData td = new tileData(hl, r, t, c);
			level.Add(gt.newTile(td), td);
		}

		// terminates this script when done
		Destroy(gameObject);
	}

	// (??)
	private IEnumerator getLevelData()
	{
		using (WWW www = new WWW(path)) {
			yield return www;

			rawString = www.text;
			isFileFound = true;
		}
	}
}
