using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditGM : MonoBehaviour {

	// singleton instance
	[HideInInspector] public static EditGM instance = null;

	// references to UI elements, snap cursor, creation tool, checkpoint tool, warp tool, and tile hierarchy
	public GameObject hudPanel;
	public PaletteControl palettePanel;
	public TileCreator tileCreator;
	public GameObject chkpntTool;
	public GameObject warpTool;
	public SnapCursor anchorIcon;
	public GameObject tileMap;

	// public read-accessibility state variables
	public InputKeys getKeys { get; private set; }
	public InputKeys getKeyDowns { get; private set; }
	public string levelName { get; private set; }
	public LevelData levelData { get; private set; }
	public int activeLayer { get; private set; }
	public bool paletteMode { get; private set; }
	public bool createMode { get; private set; }
	public bool editMode { get; private set; }
	public bool selectMode { get; private set; }

	// private variables
	private EditLoader lvl_load;
	private SelectedItem? selected_item;
	private GameObject current_tool;
	private TileData tile_buffer;
	private Dictionary<GameObject, TileData> data_lookup;

	// InputKeys wraps keyboard input into a bit-flag enum
	[Flags]
	public enum InputKeys {
		None = 0x0,
		Space = 0x1,
		Tab = 0x2,
		Delete = 0x4,
		Click0 = 0x8,
		Click1 = 0x10,
		Q = 0x20,
		W = 0x40,
		E = 0x80,
		R = 0x100,
		A = 0x200,
		S = 0x400,
		D = 0x800,
		F = 0x1000,
		Z = 0x2000,
		X = 0x4000,
		C = 0x8000,
		V = 0x10000,
		One = 0x20000,
		Two = 0x40000,
		Three = 0x80000,
		Four = 0x100000,
		Five = 0x200000,
		Six = 0x400000
	}
	// key_code_list is an index mapping between Unity KeyCode and InputKeys
	private KeyCode[] key_code_list = new KeyCode[] {
		KeyCode.None,
		KeyCode.Space,
		KeyCode.Tab,
		KeyCode.Delete,
		KeyCode.Mouse0,
		KeyCode.Mouse1,
		KeyCode.Q,
		KeyCode.W,
		KeyCode.E,
		KeyCode.R,
		KeyCode.A,
		KeyCode.S,
		KeyCode.D,
		KeyCode.F,
		KeyCode.Z,
		KeyCode.X,
		KeyCode.C,
		KeyCode.V,
		KeyCode.Alpha1,
		KeyCode.Alpha2,
		KeyCode.Alpha3,
		KeyCode.Alpha4,
		KeyCode.Alpha5,
		KeyCode.Alpha6
	};

	// a struct to help us keep track of what the hell is going on (active/inactive) when switching modes and/or tools
	private struct SelectedItem {

		public GameObject instance;
		public TileData? tileData;
		public ChkpntData? chkpntData;
		public WarpData? warpData;
		public int layerIndex;

		public SelectedItem (GameObject inInstance, TileData inTile, int inIndex)
		{
			instance = inInstance;
			tileData = inTile;
			chkpntData = null;
			warpData = null;
			layerIndex = inIndex;
		}

		public SelectedItem (GameObject inInstance, ChkpntData inChkpnt, int inIndex)
		{
			instance = inInstance;
			tileData = null;
			chkpntData = inChkpnt;
			warpData = null;
			layerIndex = inIndex;
		}

		public SelectedItem (GameObject inInstance, WarpData inWarp, int inIndex)
		{
			instance = inInstance;
			tileData = null;
			chkpntData = null;
			warpData = inWarp;
			layerIndex = inIndex;
		}
	}

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>

			current_tool = tileCreator.gameObject; // <2>
			selected_item = null;
			tile_buffer = new TileData();

			hudPanel.SetActive(false); // <3>
			chkpntTool.SetActive(false);
			warpTool.SetActive(false);
			getKeys = InputKeys.None;
			getKeyDowns = InputKeys.None;
			activeLayer = 0;
			paletteMode = false;
			createMode = true;
			editMode = false;
			selectMode = false;

			lvl_load = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			levelName = lvl_load.levelName;
			LevelData inLevel;
			data_lookup = lvl_load.supplyLevel(ref tileMap, out inLevel); // <4>
			levelData = inLevel;
			activateLayer(activeLayer); // <5>
		} else
			Destroy(gameObject); // <6>

		/*
		<1> set singleton instance
		<2> initializations for private variables
		<3> initializations for connected state variables
		<4> file is loaded and parsed
		<5> first layer is activated
		<6> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>
		if (paletteMode) return; // <3>
		updateLevel(); // <4>
		if (createMode) updateCreate(); // <5>
		if (editMode) updateEdit(); // <6>
		if (selectMode) updateSelect(); // <7>

		/*
		<1> getKeys and getKeyDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> if the palette is active, skip the rest
		<4> anchorIcon and layer changes are updated
		<5> current tool is updated for createMode
		<6> current tool is updated for editMode
		<7> current tool is updated for selectMode
		*/
	}

	/* Public Functions */

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getKeys & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getKeyDowns & inKeys) == inKeys; }

	// simply returns the z value of the current layer's transform
	public float GetLayerDepth ()
	{ return tileMap.transform.GetChild(activeLayer).position.z; }

	// if passed object is a tile, supplies corresponding TileData and it's layer
	public bool IsMappedTile (GameObject inTile, out TileData outData, out int outLayer)
	{
		if (!inTile || !inTile.transform.IsChildOf(tileMap.transform)) { // <1>
			outLayer = 0;
			outData = new TileData();
			return false;
		} else {
			outLayer = inTile.transform.parent.GetSiblingIndex(); // <2>
			outData = data_lookup[inTile]; // <3>
			return true;
		}

		/*
		<1> If the passed tile isn't part of the map, output default values and return false
		<2> If it is, output the tile's layer by parent's sibling index
		<3> then output the TileData itself via data_lookup, and return true
		*/
	}

	// deletes the current scene and loads the MainMenu scene
	public void ReturnToMainMenu ()
	{ SceneManager.LoadScene(0); } // (!!) should prompt if unsaved

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode || !(editMode || selectMode)) return; // <1>
		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <2>

		tileCreator.SetProperties(tile_buffer); // <3>
		tileCreator.SetActive(true);
		createMode = true;
		editMode = false;
		selectMode = false;

		/*
		<1> only do anything if currently in editMode or selectMode
		<2> conditional logic for switching out of editMode while an object is selected
		<3> tileCreator values are recovered from tile_buffer, and is then activated
		*/
	}

	// switches into editMode
	public void EnterEdit ()
	{
		if (editMode || !(createMode || selectMode)) return; // <1>
		if (createMode) tile_buffer = tileCreator.GetTileData(); // <2>

		if (selected_item.HasValue) removeSelectedItem(selected_item.Value); // <3>
		else tileCreator.SetActive(false); // <4>
		createMode = false;
		editMode = true;
		selectMode = false;

		/*
		<1> only do anyting if currently in creationMode or selectMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching into editMode while an object is selected
		<4> if nothing is selected, make sure tileCreator is disabled
		*/
	}

	// switches into selectMode
	public void EnterSelect ()
	{
		if (selectMode || !(createMode || editMode)) return; // <1>
		if (createMode) tile_buffer = tileCreator.GetTileData(); // <2>

		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <3>
		tileCreator.SetActive(false); // <4>
		createMode = false;
		editMode = false;
		selectMode = true;

		/*
		<1> only do anyting if currently in creationMode or editMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching out of editMode while an object is selected
		<4> tileCreator should always be disabled in selectMode
		*/
	}

	// (!!)(testing) save level to a file in plain text format
	public void SaveFile (string filename)
	{
		string fpath = "Levels\\" + filename + ".txt";
		// (!!) this all is a kludge, just testing that it works
		List<TileData> tiles = new List<TileData>(data_lookup.Values);
		LayerData layer = new LayerData(0, tiles, new List<ChkpntData>());
		List<LayerData> layerList = new List<LayerData>(new LayerData[]{layer});
		LevelData _LevelDataName = new LevelData(layerList, new List<WarpData>());
		// (!!) _LevelDataName will be replaced
		string[] lines = _LevelDataName.Serialize();

		File.WriteAllLines(fpath, lines);
	}

	/* Private Functions */

	// updates getKeys and getKeyDowns each frame
	private void updateInputs ()
	{
		getKeys = InputKeys.None;
		getKeyDowns = InputKeys.None;
		int k = 0;

		for (int i = 0; i < 0x400001; i = (i == 0) ? 1 : i * 2) { // <1>
			KeyCode kc = key_code_list[k++];
			if (Input.GetKey(kc)) getKeys = getKeys | (InputKeys) i;
			if (Input.GetKeyDown(kc)) getKeyDowns = getKeyDowns | (InputKeys) i;
		}

		/*
		<1> assigns enum flags by powers of 2
		*/
	}

	// updates UI Overlay and Palette panels
	private void updateUI ()
	{
		bool sbCKD = CheckKeyDowns(InputKeys.Space);
		bool tabCK = CheckKeys(InputKeys.Tab);

		if (sbCKD) hudPanel.SetActive(!hudPanel.activeSelf); // <1>

		if (paletteMode != tabCK) {
			palettePanel.TogglePalette(); // <2>
			// (!!) something going wrong here
			// current_tool.SetActive(!current_tool.activeSelf);
		}
		paletteMode = palettePanel.gameObject.activeSelf;

		/*
		<1> UI is toggled whenever spacebar is pressed
		<2> palette is toggled on whenever tab key is held down
		*/
	}

	// makes changes associated with anchorIcon and layer changes
	private void updateLevel ()
	{
		if (CheckKeyDowns(InputKeys.Click1)) anchorIcon.FindNewAnchor(); // <2>

		if (CheckKeyDowns(InputKeys.F)) activateLayer(activeLayer - 1); // <3>
		if (CheckKeyDowns(InputKeys.R)) activateLayer(activeLayer + 1);

		/*
		<2> right-click will update snap cursor location
		<3> F and R will change active layer
		*/
	}

	// makes changes associated with being in createMode
	private void updateCreate ()
	{
		bool b1 = current_tool == tileCreator.gameObject;
		bool b2 = current_tool == chkpntTool;
		bool b3 = current_tool == warpTool;
		if (!b1 && !b2 && !b3) return; // <1>

		if (b1) {
			int rot = tileCreator.tileRotation;
			if (CheckKeyDowns(InputKeys.Q)) tileCreator.SetRotation(rot + 1); // <2>
			if (CheckKeyDowns(InputKeys.E)) tileCreator.SetRotation(rot - 1);

			if (CheckKeyDowns(InputKeys.Z)) tileCreator.CycleColor(false);
			if (CheckKeyDowns(InputKeys.X)) tileCreator.CycleColor(true);

			if (CheckKeyDowns(InputKeys.Click0)) addTile(tileCreator.GetTileData(), activeLayer); // <3>
		}

		Vector3 pos = anchorIcon.focus.ToUnitySpace(); // <4>
		pos.z = anchorIcon.transform.position.z;
		if (b2) {
			chkpntTool.transform.position = pos;

			if (CheckKeyDowns(InputKeys.Click0)) addSpecial(new ChkpntData(false, anchorIcon.focus), activeLayer);
		}
		if (b3) {
			warpTool.transform.position = pos;

			WarpData wd = new WarpData(false, true, activeLayer, activeLayer + 1, anchorIcon.focus);
			if (CheckKeyDowns(InputKeys.Click0)) addSpecial(wd, activeLayer);
		}

		if (!b2 && CheckKeyDowns(InputKeys.C)) setTool(chkpntTool); // <5>
		if (!b3 && CheckKeyDowns(InputKeys.V)) setTool(warpTool);
		bool bType = false;
		if (CheckKeyDowns(InputKeys.One)) { tileCreator.SelectType(0); bType = true; }
		if (CheckKeyDowns(InputKeys.Two)) { tileCreator.SelectType(1); bType = true; }
		if (CheckKeyDowns(InputKeys.Three)) { tileCreator.SelectType(2); bType = true; }
		if (CheckKeyDowns(InputKeys.Four)) { tileCreator.SelectType(3); bType = true; }
		if (CheckKeyDowns(InputKeys.Five)) { tileCreator.SelectType(4); bType = true; }
		if (CheckKeyDowns(InputKeys.Six)) { tileCreator.SelectType(5); bType = true; }
		if (!b1 && bType) setTool(tileCreator.gameObject); // <6>

		/*
		<1> first, figure out which tool is active and return if none
		<2> Q and E rotate the tileCreator C-CW and CW, respectively
		<3> and then if left click is made, tile is added to the level
		<4> if one of the other two tools is active, we get an orientation for them
		<5> C and V activate the checkpoint and warp tools, respectively
		<6> numeric keys assign tile type and activate tileCreator
		*/
	}

	// makes changes associated with being in editMode
	private void updateEdit ()
	{
		if (selected_item.HasValue) {
			Vector3 v3 = anchorIcon.focus.ToUnitySpace();
			v3.z = GetLayerDepth();
			tileCreator.transform.position = v3; // <1>

			if (CheckKeyDowns(InputKeys.Click0)) {
				addTile(tileCreator.GetTileData(), activeLayer); // <2>

				tileCreator.SetProperties(tile_buffer);
				tileCreator.SetActive(false); // <3>
				selected_item = null;
				return;
			}

			if (CheckKeyDowns(InputKeys.Delete)) { // <4>
				tileCreator.SetActive(false);
				selected_item = null;
			}
		} else if (CheckKeyDowns(InputKeys.Click0)) { // <5>
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
			if (!c2d) { // <7>
				selected_item = null;
				return;
			}
			GameObject go = c2d.gameObject;
			TileData td;
			int i;
			if (IsMappedTile(go, out td, out i)) { // <8>
				selected_item = new SelectedItem(go, td, i);
				removeTile(go);
			}
			// else remove chkpnt or warp
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if there is a selected tile, left click will place it again
		<3> we then restore tileCreator to its backup, deselect selected_item and return
		<4> if there is a selected tile, Delete will simply forget it
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked on, deselect selected_item and return
		<8> if a tile was clicked on, it is made into a new SelectedItem and removed
		*/
	}

	// makes changes associated with being in selectMode
	private void updateSelect ()
	{
		//
	}

	// used when leaving editMode, places a given SelectedItem where it indicates it belongs
	private void addSelectedItem (SelectedItem inItem)
	{
		if (inItem.tileData.HasValue) {
			TileData td = inItem.tileData.Value;
			inItem.instance = addTile(td, inItem.layerIndex);
		} else if (inItem.chkpntData.HasValue) {
			ChkpntData cd = inItem.chkpntData.Value;
			inItem.instance = addSpecial(cd, inItem.layerIndex);
		} else if (inItem.warpData.HasValue) {
			WarpData wd = inItem.warpData.Value;
			inItem.instance = addSpecial(wd, inItem.layerIndex);
		}
	}

	// used when entering editMode with an item selected, removes the current instance of SelectedItem
	private void removeSelectedItem (SelectedItem inItem)
	{
		if (inItem.tileData.HasValue) {
			removeTile(inItem.instance);
			tileCreator.SetProperties(inItem.tileData.Value);
			setTool(tileCreator.gameObject);
		} else if (inItem.chkpntData.HasValue) {
			removeSpecial(inItem.instance);
			setTool(chkpntTool);
		} else if (inItem.warpData.HasValue) {
			removeSpecial(inItem.instance);
			setTool(warpTool);
		}
	}

	// adds a passed tileData to the level and returns a reference
	private GameObject addTile (TileData inData, int inLayer)
	{
		levelData.layerSet[inLayer].tileSet.Add(inData); // <1>

		Transform tl = tileMap.transform.GetChild(activeLayer);
		GameObject go = tileCreator.GetActiveTile();
		go.transform.SetParent(tl); // <2>

		data_lookup[go] = inData; // <3>
		return go;

		/*
		<1> first, TileData is added to levelData
		<2> second, a corresponding tile is added to tileMap
		<3> lastly, the tile's gameObject is added to the lookup dictionary and returned
		*/
	}

	// removes the specified tile from the level
	private void removeTile (GameObject inTile)
	{
		tile_buffer = tileCreator.GetTileData(); // <1>
		int tLayer;
		TileData tData;
		bool b = IsMappedTile(inTile, out tData, out tLayer); // <2>
		if (b) selected_item = new SelectedItem(inTile, tData, activeLayer);
		else return; // <3>
		tileCreator.SetProperties(tData); // <4>
		tileCreator.SetActive(true);

		levelData.layerSet[tLayer].tileSet.Remove(tData); // <5>
		data_lookup.Remove(inTile);
		Destroy(inTile);

		/*
		<1> first, back up tileCreator state
		<2> next, lookup the tile's TileData
		<3> if the specified tile is not part of tileMap, we ignore
		<4> then set the tileCreator up to act like the selected tile
		<5> after all that, levelData is updated
		<6> reset flag, remove from the lookup, and delete the tile
		*/
	}

	//
	private GameObject addSpecial (ChkpntData inChkpnt, int inLayer)
	{
		//
		Debug.Log("Place checkpoint.");
		return new GameObject();
	}

	//
	private GameObject addSpecial (WarpData inWarp, int inLayer)
	{
		//
		Debug.Log("Place checkpoint.");
		return new GameObject();
	}

	//
	private void removeSpecial (GameObject inSpecial)
	{
		//
		Debug.Log("Remove special.");
	}

	// sets the currently active tool
	private void setTool (GameObject inTool)
	{
		bool b = false;
		b |= inTool == chkpntTool;
		b |= inTool == tileCreator.gameObject;
		b |= inTool == warpTool;
		if (!b) return;

		current_tool.SetActive(false);
		current_tool = inTool;
		current_tool.SetActive(true);
	}

	// cycles through all layers, calculates distance, and sets opacity accordingly
	private void activateLayer (int inLayer)
	{
		bool b = (inLayer < 0) || (inLayer >= tileMap.transform.childCount);
		if (b) return; // <1>
		else activeLayer = inLayer; // <2>

		foreach (Transform layer in tileMap.transform) {
			int d = layer.GetSiblingIndex();
			d = Math.Abs(d - activeLayer);
			setLayerOpacity(layer, d); // <3>
		}

		Vector3 v3 = anchorIcon.transform.position;
		v3.z = GetLayerDepth();
		anchorIcon.transform.position = v3; // <4>

		/*
		<1> if invalid layer index is given, fail quietly
		<2> otherwise update activeLayer and continue
		<3> ordinal distance from activeLayer is calculated, and opacity set accordingly
		<4> add active layer depth and move the snap cursor to the new location
		*/
	}

	// sets the opacity of all tiles within a layer using ordinal distance from activeLayer
	private void setLayerOpacity (Transform tileLayer, int distance)
	{
		float a = 1f; // <1>
		int l = 0; // <2>
		if (distance != 0) { // <3>
			a = 1f / (distance + 1f);
			l = 9;
		}
		Color c = new Color(1f, 1f, 1f, a);

		foreach (Transform tile in tileLayer) { // <4>
			tile.gameObject.layer = l;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
		}

		/*
		<1> a represents an alpha value
		<2> l represents the physics layer we will be setting
		<3> if this isn't the active layer, opacity and layer are set accordingly
		<4> the calculated opacity and layer are applied to all tiles within the layer
		*/
	}
}