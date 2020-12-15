using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	/* Enums */

	// EditorMode establishes the different modes the editor can be in
	[Flags]
	public enum EditorMode {
		Select = 0x0,
		Edit = 0x1,
		Create = 0x2,
		Paint = 0x4
	}

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

	/* Public Operations */

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode) return; // <1>

		if (selected_item.instance) {
			if (selected_item.tileData.HasValue) {
				tileCreator.SetProperties(selected_item.tileData.Value);
				setTool(tileCreator.gameObject); // <2>
			}
			if (selected_item.chkpntData.HasValue) setTool(chkpntTool);
			if (selected_item.warpData.HasValue) setTool(warpTool);
			if (editMode) addSelectedItem(); // <3>
			else selected_item = new SelectedItem(); // <4>
		} else {
			setTool(tileCreator.gameObject); // <5>
		}

		current_mode = EditorMode.Create;

		/*
		<1> if already in createMode, simply escape
		<2> if selected_item is a tile, use its tileData to set tile tool
		<3> if exiting editMode, add selected_item back to the level
		<4> if not exiting editMode, simply unselect selected_item
		<5> if no selected_item, default to tile tool
		*/
	}

	// switches into editMode
	public void EnterEdit ()
	{
		if (editMode) return; // <1>

		if (selected_item.instance) {
			if (selected_item.tileData.HasValue) {
				tileCreator.SetProperties(selected_item.tileData.Value);
				setTool(tileCreator.gameObject); // <2>
			}
			if (selected_item.chkpntData.HasValue) setTool(chkpntTool);
			if (selected_item.warpData.HasValue) setTool(warpTool);
			removeSelectedItem(); // <3>
		} else {
			setTool(tileCreator.gameObject); // <4>
		}

		current_mode = EditorMode.Edit;

		/*
		<1> if already in editMode, simply escape
		<2> if selected_item is a tile, use its tileData to set tile tool
		<3> regardless of item selected, unselect it
		<4> if no selected_item, default to tile tool
		*/
	}

	// switches into paintMode
	public void EnterPaint ()
	{
		if (paintMode) return; // <1>

		if (selected_item.instance) {
			if (selected_item.tileData.HasValue) tileCreator.SetProperties(selected_item.tileData.Value); // <2>
			if (editMode) addSelectedItem(); // <3>
			else selected_item = new SelectedItem(); // <4>
		}

		setTool(tileCreator.gameObject); // <5>
		current_mode = EditorMode.Paint;

		/*
		<1> if already in paintMode, simply escape
		<2> if selected_item is a tile, use its tileData to set tile tool
		<3> if in editMode, add selected_item back to the level
		<4> if not in editMode, simply unselect selected_item
		<5> always enter paintMode with tile tool enabled
		*/
	}

	// switches into selectMode
	public void EnterSelect ()
	{
		if (selectMode) return; // <1>

		if (editMode && selected_item.instance) addSelectedItem(); // <2>

		current_tool.SetActive(false); // <3>
		current_mode = EditorMode.Select;

		/*
		<1> only do anyting if currently in creationMode or editMode
		<2> conditional logic for switching out of editMode while an object is selected
		<3> current_tool should always be disabled in selectMode
		*/
	}

	/* Public Utilities */

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getKeyDowns & inKeys) == inKeys; }

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getKeys & inKeys) == inKeys; }

	// simply returns the z value of the current layer's transform
	public float GetLayerDepth ()
	{ return GetLayerDepth(activeLayer); }

	// simply returns the z value of the given layer's transform
	public float GetLayerDepth (int inLayer)
	{ return tileMap.transform.GetChild(inLayer).position.z; }

	// if passed object is a tile, supplies corresponding TileData
	public bool IsMappedTile (GameObject inTile, out TileData outData)
	{
		if (!inTile || !tile_lookup.ContainsKey(inTile)) { // <1>
			outData = new TileData();
			return false;
		} else {
			outData = tile_lookup[inTile]; // <2>
			return true;
		}

		/*
		<1> If the passed tile isn't part of the map, output default values and return false
		<2> If it is, then output the TileData itself via tile_lookup and return true
		*/
	}

	// if passed object is a checkpoint, supplies corresponding ChkpntData
	public bool IsMappedChkpnt (GameObject inChkpnt, out ChkpntData outData)
	{
		if (!inChkpnt || !chkpnt_lookup.ContainsKey(inChkpnt)) { // <1>
			outData = new ChkpntData();
			return false;
		} else {
			outData = chkpnt_lookup[inChkpnt]; // <2>
			return true;
		}

		/*
		<1> If the passed checkpoint isn't part of the map, output default values and return false
		<2> If it is, then output the ChkpntData itself via chkpnt_lookup and return true
		*/
	}

	// if passed object is a checkpoint, supplies corresponding WarpData
	public bool IsMappedWarp (GameObject inWarp, out WarpData outData)
	{
		if (!inWarp || !warp_lookup.ContainsKey(inWarp)) { // <1>
			outData = new WarpData();
			return false;
		} else {
			outData = warp_lookup[inWarp]; // <2>
			return true;
		}

		/*
		<1> If the passed checkpoint isn't part of the map, output default values and return false
		<2> If it is, then output the ChkpntData itself via chkpnt_lookup and return true
		*/
	}

	// deletes the current scene and loads the MainMenu scene
	public void ReturnToMainMenu ()
	{ SceneManager.LoadScene(0); } // (!!) should prompt if unsaved

	// (!!)(incomplete) save level to a file in plain text format
	public void SaveFile (string filename)
	{
		// (!!) should prompt for string instead
		string fpath = "Levels\\" + filename + ".txt";

		string[] lines = levelData.Serialize();
		File.WriteAllLines(fpath, lines);
	}
}