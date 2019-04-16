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

	// references to the loader, UI overlay, tile hierarchy, creation tool, and snap cursor
	private EditLoader lvlLoad = null;
	public MenuControl menu_panel;
	public PaletteControl palette_panel;
	public GameObject tile_map;
	public GenesisTile genesis_tile;
	public SnapCursor anchor_icon;

	// public read-accessibility state variables
	public levelData level_data { get; private set; }
	public int active_layer { get; private set; }
	public bool menu_mode { get; private set; }
	public bool edit_mode { get; private set; } // <*>
	public inputKeys get_keys { get; private set; }
	public inputKeys get_key_downs { get; private set; }
	// <*> edit_mode being false may be referred to in commenting as "creation mode"

	// private variables
	private bool isTileSelected;
	private tileData selectedTile;
	private bool editFlag;
	private tileData gtBackup;
	private Dictionary<GameObject, tileData> dataLookup;


	// inputKeys wraps keyboard input into a bit-flag enum
	[Flags]
	public enum inputKeys {
		None = 0x0,
		Space = 0x1,
		Tab = 0x2,
		Delete = 0x4,
		Click0 = 0x8,
		Click1 = 0x10,
		Q = 0x20,
		W = 0x40,
		E = 0x80,
		A = 0x100,
		S = 0x200,
		D = 0x400,
		One = 0x800,
		Two = 0x1000,
		Three = 0x2000,
		Four = 0x4000,
		Five = 0x8000,
		Six = 0x10000
	}
	// keyCodeList is an index mapping between Unity KeyCode and inputKeys
	private KeyCode[] keyCodeList = new KeyCode[] {
		KeyCode.None,
		KeyCode.Space,
		KeyCode.Tab,
		KeyCode.Delete,
		KeyCode.Mouse0,
		KeyCode.Mouse1,
		KeyCode.Q,
		KeyCode.W,
		KeyCode.E,
		KeyCode.A,
		KeyCode.S,
		KeyCode.D,
		KeyCode.Alpha1,
		KeyCode.Alpha2,
		KeyCode.Alpha3,
		KeyCode.Alpha4,
		KeyCode.Alpha5,
		KeyCode.Alpha6
	};

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>

			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			levelData inLevel;
			dataLookup = lvlLoad.supplyLevel(ref tile_map, out inLevel); // <2>
			level_data = inLevel;

			isTileSelected = false; // <3>
			selectedTile = new tileData();
			editFlag = false;
			gtBackup = new tileData();

			active_layer = 0; // <4>
			menu_mode = false;
			edit_mode = false;
			get_keys = inputKeys.None;
			get_key_downs = inputKeys.None;
		} else
			Destroy(gameObject); // <5>

		/*
		<1> set singleton instance
		<2> file is loaded and parsed
		<3> initializations for private variables
		<4> initializations for state variables
		<5> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>

		bool mold = menu_mode;
		menu_mode = checkKeyDowns(inputKeys.Space) ? !mold : mold; // <2>

		if (menu_mode) {
			if (!mold) { // <3>
				menu_panel.activate();
				genesis_tile.deactivate();
			}
		} else {
			if (mold) { // <4>
				menu_panel.deactivate();
				genesis_tile.activate();
			}

			if (checkKeyDowns(inputKeys.Click1)) anchor_icon.findNewAnchor(); // <5>

			if (edit_mode) {
				if (editFlag) genesis_tile.deactivate(); // <6>

				updateEditMode();
			} else {
				if (editFlag) genesis_tile.activate(); // <7>

				updateGT();
			}
		}

		editFlag = false; // <8>

		/*
		<1> get_keys and get_key_downs are updated
		<2> menu_mode is toggled whenever spacebar is pressed
		<3> if we entered menu_mode this frame, we activate menu and deactivate tool
		<4> if we exited menu_mode this frame, we deactivate menu and activate tool
		<5> right-clicks will update snap cursor location
		<6> deactivate tool when entering edit_mode
		<7> activate tool when exiting edit_mode
		<8> reset flag every frame, only set by toggleEdit()
		*/
	}

	/* Public Functions */

	// simply returns whether the given keys were being held during this frame
	public bool checkKeys (inputKeys inKeys)
	{ return (get_keys & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool checkKeyDowns (inputKeys inKeys)
	{ return (get_key_downs & inKeys) == inKeys; }

	// returns the tileData corresponding to the passed tile, and supplies it's layer
	public bool getDataFromTile (GameObject inTile, out tileData outData, out int outLayer)
	{
		if (!inTile.transform.IsChildOf(tile_map.transform)) { // <1>
			outLayer = 0;
			outData = new tileData();
			return false;
		} else {
			outLayer = inTile.transform.parent.GetSiblingIndex(); // <2>
			outData = dataLookup[inTile]; // <3>
			return true;
		}

		/*
		<1> If the passed tile isn't part of the map, log error and return default values
		<2> If it is, output the tile's layer by parent's sibling index
		<3> then return the tileData itself via dataLookup
		*/
	}

	// deletes the current scene and loads the MainMenu scene
	public void returnToMainMenu ()
	{
		// (!!) prompt if unsaved
		SceneManager.LoadScene(0);
	}

	// toggles edit_mode and makes associated changes to current
	public void toggleEdit ()
	{
		if (edit_mode) {
			genesis_tile.setProperties(gtBackup); // <1>
			gtBackup = isTileSelected ? selectedTile : new tileData(); // <2>
			genesis_tile.activate();
		} else {
			tileData td = gtBackup;
			gtBackup = getGTData(); // <3>

			if (isTileSelected) {
				selectedTile = td;
				genesis_tile.setProperties(selectedTile); // <4>
				genesis_tile.activate();
			}
			else genesis_tile.deactivate(); // <5>
		}

		edit_mode = !edit_mode; // <6>
		editFlag = true;

		/*
		<1> if we're in edit_mode, restore the genesis_tile to previous properties
		<2> if there's a tile selected, it's properties are stored in gtBackup
		<3> if we're in creation mode, current state of genesis_tile is stored
		<4> if there's a tile selected, it's properties are restored from gtBackup
		<5> if no tile is selected, we deactivate the tool
		<6> either way, edit_mode is toggled and flag is set
		*/
	}

	// (testing) save level to a file in plain text format
	public void saveFile (string filename)
	{
		string fpath = "Levels\\" + filename + ".txt";
		// (!!) this all is a kludge, just testing that it works
		List<tileData> tiles = new List<tileData>(dataLookup.Values);
		layerData layer = new layerData(0, tiles, new List<chkpntData>());
		List<layerData> layerList = new List<layerData>(new layerData[]{layer});
		levelData _levelDataName = new levelData(layerList, new List<warpData>());
		// (!!) _levelDataName will be replaced
		string[] lines = _levelDataName.serialize();

		File.WriteAllLines(fpath, lines);
	}

	/* Private Functions */

	// makes changes associated with the state of the genesis_tile
	private void updateGT ()
	{
		Vector3 v3 = anchor_icon.focus.toUnitySpace();
		v3.z = tile_map.transform.GetChild(active_layer).position.z;
		genesis_tile.transform.position = v3; // <1>

		if (checkKeyDowns(inputKeys.Tab)) genesis_tile.cycleColor(); // <2>

		if (checkKeyDowns(inputKeys.Q)) genesis_tile.rotate(false); // <3>
		if (checkKeyDowns(inputKeys.E)) genesis_tile.rotate(true);

		if (checkKeyDowns(inputKeys.One)) genesis_tile.selectType(0); // <4>
		if (checkKeyDowns(inputKeys.Two)) genesis_tile.selectType(1);
		if (checkKeyDowns(inputKeys.Three)) genesis_tile.selectType(2);
		if (checkKeyDowns(inputKeys.Four)) genesis_tile.selectType(3);
		if (checkKeyDowns(inputKeys.Five)) genesis_tile.selectType(4);
		if (checkKeyDowns(inputKeys.Six)) genesis_tile.selectType(5);

		if (checkKeyDowns(inputKeys.Click0)) addTile(); // <5>

		/*
		<1> in creation mode, the genesis_tile will follow the focus
		<2> when Tab is pressed, cycle through colors
		<3> Q and E rotate C-CW and CW, respectively
		<4> numeric keys assign tile type
		<5> if left click is made, tile is added to the level
		*/
	}

	// makes changes associated with being in edit_mode
	private void updateEditMode ()
	{
		if (isTileSelected) {
			Vector3 v3 = anchor_icon.focus.toUnitySpace();
			v3.z = tile_map.transform.GetChild(active_layer).position.z;
			genesis_tile.transform.position = v3; // <1>

			if (checkKeyDowns(inputKeys.Click0)) {
				addTile(); // <2>

				genesis_tile.setProperties(gtBackup); // <3>
				genesis_tile.deactivate();
				isTileSelected = false;
				return;
			}

			if (checkKeyDowns(inputKeys.Delete)) { // <4>
				genesis_tile.setProperties(gtBackup);
				isTileSelected = false;
			}
		} else {
			if (checkKeyDowns(inputKeys.Click0)) { // <5>
				Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
				Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
				if (!c2d) return; // <7>
				GameObject go = c2d.gameObject;
				removeTile(go); // <8>
			}
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if there is a selected tile, left click will place it again
		<3> we then restore genesis_tile to its backup, reset flag, and return
		<4> if there is a selected tile, Delete will simply forget it
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked on, return
		<8> if a tile was clicked on, it is removed
		*/
	}

	// adds a tile to the level based on current state of genesis_tile
	private void addTile ()
	{
		level_data.layerSet[active_layer].tileSet.Add(getGTData()); // <1>

		Transform tl = tile_map.transform.GetChild(active_layer);
		GameObject go = genesis_tile.getActiveTile();
		go.transform.SetParent(tl); // <2>

		/*
		<1> first, tileData is added to level_data
		<2> second, a corresponding tile is added to tile_map
		*/
	}

	// removes the specified tile from the level
	private void removeTile (GameObject inTile)
	{
		gtBackup = getGTData(); // <1>
		int tLayer;
		tileData tData;
		bool b = getDataFromTile(inTile, out tData, out tLayer); // <2>
		if (b) selectedTile = tData;
		else return; // <3>
		genesis_tile.activate();
		genesis_tile.setProperties(selectedTile); // <4>

		level_data.layerSet[tLayer].tileSet.Remove(selectedTile); // <5>
		isTileSelected = true; // <6>
		dataLookup.Remove(inTile);
		Destroy(inTile);

		/*
		<1> first, back up genesis_tile state
		<2> next, lookup the tile's tileData
		<3> if the specified tile is not part of tile_map, we ignore
		<4> then set the genesis_tile up to act like the selected tile
		<5> after all that, level_data is updated
		<6> finally, reset flag, remove from the lookup, and delete the tile
		*/
	}

	// updates get_keys and get_key_downs each frame
	private void updateInputs ()
	{
		get_keys = inputKeys.None;
		get_key_downs = inputKeys.None;
		int k = 0;

		for (int i = 0; i < 0x10001; i = (i == 0) ? 1 : i * 2) { // <1>
			KeyCode kc = keyCodeList[k++];
			if (Input.GetKey(kc)) get_keys = get_keys | (inputKeys) i;
			if (Input.GetKeyDown(kc)) get_key_downs = get_key_downs | (inputKeys) i;
		}

		/*
		<1> assigns enum flags by powers of 2
		*/
	}

	// returns a tileData representation of the current state of genesis_tile
	private tileData getGTData ()
	{
		GenesisTile gt = genesis_tile;
		return new tileData(gt.tileType, gt.tileColor, anchor_icon.focus, gt.tileRotation);
	}
}