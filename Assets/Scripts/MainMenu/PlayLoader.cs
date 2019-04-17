using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class PlayLoader : MonoBehaviour {

	// prefab references (included in transform children)
	public GameObject tileLoader;

	// short-form pathname for the level to be loaded
	private string path;
	// 6x7 array of prefab references
	private GameObject[,] prefab_refs;

	void Awake ()
	{
		// prefabs are loaded from a single transform hierarchy
		prefab_refs = new GameObject[6, 7];
		foreach (Transform tileLayer in tileLoader.transform)
			foreach (Transform tile in tileLayer)
				prefab_refs[tileLayer.GetSiblingIndex(), tile.GetSiblingIndex()] = tile.gameObject;

		// filepath of level to be loaded
		// (!) currently just change the string and recompile :|
		// (!!) prompt for string instead
		path = "testLevel.txt";
		DontDestroyOnLoad(gameObject);
		// load Playing scene (PlayGM will call supplyLevel)
		SceneManager.LoadScene(1);
	}

	// supplies a hierarchy of tiles, a level representation, and a Vector2 indicating a starting location
	public void supplyLevel (ref GameObject tiles, out LevelData level, out Vector2 playerStart)
	{
		// initialization
		tiles.transform.position = Vector3.zero;
		playerStart = Vector2.zero;
		int layerCount = 0;
		// begin parsing file
		bool file_exists = File.Exists("Levels\\" + path);
		if (file_exists) {
			string[] lines = File.ReadAllLines("Levels\\" + path);
			level = FileParsing.ReadLevel(lines);
		} else {
			level = new LevelData();
			return;
		}

		// populate tile hierarchy
		foreach (LayerData ld in level.layerSet) {
			GameObject tileLayer = new GameObject();
			tileLayer.transform.position = new Vector3(0f, 0f, layerCount++ * 2f);
			tileLayer.transform.SetParent(tiles.transform);

			foreach (TileData td in ld.tileSet) {
				GameObject pfRef = prefab_refs[td.type, td.color];
				Quaternion q = Quaternion.Euler(0, 0, 30 * td.rotation);
				Vector3 v3 = td.locus.ToUnitySpace();
				v3.z = tileLayer.transform.position.z;
				GameObject go = Instantiate(pfRef, v3, q) as GameObject;
				go.transform.SetParent(tileLayer.transform);
			}
		}

		// hard-coded player start for now (!!) needs to change
		HexLocus hl = new HexLocus(0, 0, 0, 0, 0, -10);
		playerStart = hl.ToUnitySpace();

		// terminates this script when done
		Destroy(gameObject);
	}
}