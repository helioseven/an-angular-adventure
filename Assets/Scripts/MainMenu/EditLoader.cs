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
	private TileCreator tc_ref;
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
		<3> load Editing scene (EditGM will call supplyLevel)
		*/
	}

	// supplies the tileMap with gameObjects and supplies a level representation, then returns a lookup mapping
	public Dictionary<GameObject,TileData> supplyLevel (
		ref GameObject tile_map,
		ref GameObject chkpnt_map,
		ref GameObject warp_map,
		out LevelData level)
	{
		tc_ref = EditGM.instance.tileCreator;

		foreach (Transform tileGroup in tc_ref.transform)
			foreach (Transform tile in tileGroup) {
				int tgi = tileGroup.GetSiblingIndex();
				int ti = tile.GetSiblingIndex();
				prefab_refs[tgi, ti] = tile.gameObject; // <1>
			}

		Dictionary<GameObject, TileData> returnDict = new Dictionary<GameObject,TileData>();
		tile_map.transform.position = Vector3.zero;

		bool file_exists = File.Exists("Levels\\" + path); // <2>
		if (file_exists) {
			string[] lines = File.ReadAllLines("Levels\\" + path);
			level = FileParsing.ReadLevel(lines); // <3>
		} else {
			Debug.Log("File not found, loading new level.");
			level = new LevelData();
			level.tileSet = new List<TileData>(); // <4>
		}

		foreach (TileData td in level.tileSet) { // <5>
			addLayers(tile_map, td.orient.layer); // <6>
			Transform tileLayer = tile_map.transform.GetChild(td.orient.layer);
			GameObject pfRef = prefab_refs[td.type, td.color];
			Vector3 v3 = td.orient.locus.ToUnitySpace();
			v3.z = tileLayer.position.z;
			Quaternion q = Quaternion.Euler(0, 0, 30 * td.orient.rotation);
			GameObject go = Instantiate(pfRef, v3, q) as GameObject;
			go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(tileLayer);
			returnDict.Add(go, td); // <7>
		}

		foreach (ChkpntData cd in level.chkpntSet) {
			GameObject cpRef = EditGM.instance.chkpntTool;
			Vector3 v3 = cd.locus.ToUnitySpace();
			v3.z = tile_map.transform.GetChild(cd.layer).position.z;
			GameObject go = Instantiate(cpRef, v3, Quaternion.identity) as GameObject;
			go.GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(chkpnt_map.transform);
		}

		foreach (WarpData wd in level.warpSet) {
			GameObject warpRef = EditGM.instance.warpTool;
			Vector3 v3 = wd.orient.locus.ToUnitySpace();
			v3.z = tile_map.transform.GetChild(wd.orient.layer).position.z;
			Quaternion q = Quaternion.Euler(0, 0, 30 * wd.orient.rotation);
			GameObject go = Instantiate(warpRef, v3, q) as GameObject;
			go.GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(warp_map.transform);
		}

		Destroy(gameObject); // <10>
		return returnDict;

		/*
		<1> prefab references are arrayed for indexed access
		<2> first, check to see whether the file exists
		<3> if file exists, it is loaded and parsed
		<4> if file doesn't exist, empty level is created
		<5> create Unity instances for each tile
		<6> make sure we have enough layers in the level for the given layer index
		<7> add the GameObject, TileData pair to the lookup
		<8> create Unity instances for each checkpoint
		<9> create Unity instances for each warp
		<10> when script is done, it schedules self-termination and returns
		*/
	}

	/* Private Functions */

	// simply adds layers to the level until there are enough layers to account for the given layer
	private void addLayers(GameObject inMap, int inLayer)
	{
		if (inLayer < inMap.transform.childCount) return; // <1>
		for (int i = inMap.transform.childCount; i <= inLayer; i++) { // <2>
			GameObject tileLayer = new GameObject("Layer #" + i.ToString());
			tileLayer.transform.position = new Vector3(0f, 0f, i * 2f);
			tileLayer.transform.SetParent(inMap.transform);
		}

		/*
		<1> if there are already more layers than the passed index, simply return
		<2> otherwise, create layers until the passed index is reached
		*/
	}
}
