using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditLoader : MonoBehaviour {

	// public read-accessibility state variables
	public string levelName { get; private set; }

	// private variables
	private string path;
	private GenesisTile gt_ref;
	private GameObject[,] prefab_refs;

	void Awake ()
	{
		levelName = "testLevel"; // <1>
		path = levelName + ".txt";
		prefab_refs = new GameObject[6, 8];
		DontDestroyOnLoad(gameObject); // <2>
		SceneManager.LoadScene(2); // <3>

		/*
		<1> levelName is hard coded (!!), should be prompted
		<2> this loader stays awake when next scene is loaded
		<3> load Playing scene (PlayGM will call supplyLevel)
		*/
	}

	// supplies a hierarchy of tiles and a level representation, then returns a lookup mapping
	public Dictionary<GameObject,TileData> supplyLevel (ref GameObject tile_map, out LevelData level)
	{
		gt_ref = EditGM.instance.genesisTile;

		foreach (Transform tileGroup in gt_ref.transform)
			foreach (Transform tile in tileGroup) {
				int tgi = tileGroup.GetSiblingIndex();
				int ti = tile.GetSiblingIndex();
				prefab_refs[tgi, ti] = tile.gameObject; // <1>
			}

		Dictionary<GameObject, TileData> returnDict = new Dictionary<GameObject,TileData>();
		tile_map.transform.position = Vector3.zero;
		int layerCount = 0;

		bool file_exists = File.Exists("Levels\\" + path); // <2>
		if (file_exists) {
			string[] lines = File.ReadAllLines("Levels\\" + path);
			level = FileParsing.ReadLevel(lines); // <3>
		} else {
			Debug.Log("File not found, loading new level.");
			level = new LevelData();
			level.layerSet = new List<LayerData>();
			LayerData empty_layer = new LayerData();
			empty_layer.tileSet = new List<TileData>();
			level.layerSet.Add(empty_layer); // <4>
		}

		foreach (LayerData ld in level.layerSet) {
			GameObject tileLayer = new GameObject();
			tileLayer.transform.position = new Vector3(0f, 0f, layerCount++ * 2f);
			tileLayer.transform.SetParent(tile_map.transform); // <5>

			foreach (TileData td in ld.tileSet) {
				GameObject pfRef = prefab_refs[td.type, td.color];
				Quaternion q = Quaternion.Euler(0, 0, 30 * td.rotation);
				Vector3 v3 = td.locus.ToUnitySpace();
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
		<7> add the GameObject, TileData pair to the lookup
		<8> when script is done, it self-terminates
		*/
	}
}
