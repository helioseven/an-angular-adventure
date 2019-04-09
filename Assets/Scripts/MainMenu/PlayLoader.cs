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

	// prefab references (included in transform children)
	public GameObject tileLoader;

	void Awake ()
	{
		// prefabs are loaded from a single transform hierarchy
		prefabRefs = new GameObject[6, 7];
		for (int i = 0; i < 6; i++) {
			GameObject tlGroup = tileLoader.transform.GetChild(i).gameObject;
			for (int j = 0; j < 7; j++) {
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

	// supplies a HashSet of tiles, a level representation, and a Vector2 indicating a starting location
	public void supplyLevel (ref GameObject tiles, out levelData level, out Vector2 playerStart)
	{
		// initialization
		tiles.transform.position = Vector3.zero;
		playerStart = Vector2.zero;
		// begin parsing file
		string[] lines = File.ReadAllLines("Levels\\" + path);
		level = FileParsing.readLevel(lines);
		int levelCount = 0;

		// populate tile hierarchy
		foreach (layerData ld in level.layerSet) {
			GameObject tileLayer = new GameObject();
			tileLayer.transform.position = new Vector3(0f, 0f, levelCount++ * 2f);
			tileLayer.transform.SetParent(tiles.transform);

			foreach (tileData td in ld.tileSet) {
				GameObject pfRef = prefabRefs[td.type, td.color];
				Quaternion q = Quaternion.Euler(0, 0, 30 * td.rotation);
				Vector3 v3 = td.locus.toUnitySpace();
				v3.z = tileLayer.transform.position.z;
				GameObject go = Instantiate(pfRef, v3, q) as GameObject;
				go.transform.SetParent(tileLayer.transform);
			}
		}

		// hard-coded player start for now (!!) needs to change
		hexLocus hl = new hexLocus(0, 0, 0, 0, 0, -10);
		playerStart = hl.toUnitySpace();

		// terminates this script when done
		Destroy(gameObject);
	}
}