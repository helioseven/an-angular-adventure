using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditLoader : MonoBehaviour {

	private string path;
	private GenesisTile gtRef;
	private GameObject[,] prefabRefs;

	void Awake ()
	{
		path = "testLevel.txt"; // <1>
		prefabRefs = new GameObject[6, 7];
		DontDestroyOnLoad(gameObject); // <2>
		SceneManager.LoadScene(2); // <3>

		/*
		<1> path is hard coded (!!), should be prompt
		<2> this loader stays awake when next scene is loaded
		<3> load Playing scene (PlayGM will call supplyLevel)
		*/
	}

	// supplies a hierarchy of tiles and a level representation, then returns a lookup mapping
	public Dictionary<GameObject,tileData> supplyLevel (ref GameObject tile_map, out levelData level)
	{
		gtRef = EditGM.instance.genesis_tile;

		foreach (Transform tileGroup in gtRef.transform)
			foreach (Transform tile in tileGroup) {
				int tgi = tileGroup.GetSiblingIndex();
				int ti = tile.GetSiblingIndex();
				prefabRefs[tgi, ti] = tile.gameObject; // <1>
			}

		Dictionary<GameObject, tileData> returnDict = new Dictionary<GameObject,tileData>();
		tile_map.transform.position = Vector3.zero;
		int layerCount = 0;

		bool file_exists = File.Exists("Levels\\" + path); // <2>
		if (file_exists) {
			string[] lines = File.ReadAllLines("Levels\\" + path);
			level = FileParsing.readLevel(lines); // <3>
		} else {
			Debug.Log("File not found, loading new level.");
			level = new levelData();
			level.layerSet = new List<layerData>();
			layerData empty_layer = new layerData();
			empty_layer.tileSet = new List<tileData>();
			level.layerSet.Add(empty_layer); // <4>
		}

		foreach (layerData ld in level.layerSet) {
			GameObject tileLayer = new GameObject();
			tileLayer.transform.position = new Vector3(0f, 0f, layerCount++ * 2f);
			tileLayer.transform.SetParent(tile_map.transform); // <5>

			foreach (tileData td in ld.tileSet) {
				GameObject pfRef = prefabRefs[td.type, td.color];
				Quaternion q = Quaternion.Euler(0, 0, 30 * td.rotation);
				Vector3 v3 = td.locus.toUnitySpace();
				v3.z = tileLayer.transform.position.z;
				GameObject go = Instantiate(pfRef, v3, q) as GameObject;
				go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
				go.transform.SetParent(tileLayer.transform); // <6>
				returnDict.Add(go, td); // <7>
			}
		}

		Destroy(gameObject); // <8>
		return returnDict;

		/*
		<1> prefab references are arrayed for indexed access
		<2> first, check to see whether the file exists
		<3> if file exists, it is loaded and parsed
		<4> if file doesn't exist, empty level is created
		<5> each tile layer is parented to the tile_map object
		<6> each tile is parented to its respective layer
		<7> add the GameObject, tileData pair to the lookup
		<8> when script is done, it self-terminates
		*/
	}
}
