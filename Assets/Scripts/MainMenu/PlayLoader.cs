using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class PlayLoader : MonoBehaviour {

	// short-form pathname for the level to be loaded
	private string path;
	// 6x7 array of prefab references
	private GameObject[,] prefabRefs;

	// prefab references
	public GameObject tileLoader;

	void Awake ()
	{
		// prefabs are loaded from a single transform hierarchy
		prefabRefs = new GameObject[6, 8];
		for (int i = 0; i < 6; i++) {
			GameObject tlGroup = tileLoader.transform.GetChild(i).gameObject;
			for (int j = 0; j < 8; j++) {
				prefabRefs[i, j] = tlGroup.transform.GetChild(j).gameObject;
			}
		}

		// filepath of level to be loaded
		// (!) currently just change the string and recompile :|
		// (!!) prompt for string instead
		path = "testLevel.txt";
		DontDestroyOnLoad(gameObject);
		// load Playing scene (PlayGM will call supplyLevel)
		SceneManager.LoadScene(1);
	}

	// supplies a HashSet of tiles representing the level, and a Vector2 representing a starting location
	public void supplyLevel (out HashSet<GameObject> level, out Vector2 playerStart)
	{
		// initialization
		level = new HashSet<GameObject>();
		playerStart = Vector2.zero;
		// begin parsing file
		string[] lines = File.ReadAllLines("Levels\\" + path);
		levelData ld = FileParsing.readLevel(lines);

		foreach (tileData td in ld.layerSet[0].tileSet) {
			GameObject pfRef = prefabRefs[td.type, td.color];
			Quaternion q = Quaternion.Euler(0, 0, 30 * td.rotation);
			GameObject go = Instantiate(pfRef, td.locus.toUnitySpace(), q) as GameObject;
			level.Add(go);
		}

		// hard-coded player start for now (!!) needs to change
		hexLocus hl = new hexLocus(0, 0, 0, 0, 0, -10);
		playerStart = hl.toUnitySpace();

		// terminates this script when done
		Destroy(gameObject);
	}
}