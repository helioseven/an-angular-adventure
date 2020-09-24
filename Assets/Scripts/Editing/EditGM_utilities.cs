using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	// InputKeys wraps keyboard input into a bit-flag enum
	[Flags]
	public enum InputKeys {
		None = 0x0,
		HUD = 0x1,
		Palette = 0x2,
		Delete = 0x4,
		ClickMain = 0x8,
		ClickAlt = 0x10,
		CCW = 0x20,
		Up = 0x40,
		CW = 0x80,
		In = 0x100,
		Left = 0x200,
		Down = 0x400,
		Right = 0x800,
		Out = 0x1000,
		ColorCCW = 0x2000,
		ColorCW = 0x4000,
		Chkpnt = 0x8000,
		Warp = 0x10000,
		One = 0x20000,
		Two = 0x40000,
		Three = 0x80000,
		Four = 0x100000,
		Five = 0x200000,
		Six = 0x400000
	}

	/* Public Operations */

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode || !(editMode || selectMode)) return; // <1>
		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <2>

		tileCreator.SetProperties(tile_buffer); // <3>
		setTool(tileCreator.gameObject);
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
		else current_tool.SetActive(false); // <4>
		createMode = false;
		editMode = true;
		selectMode = false;

		/*
		<1> only do anyting if currently in creationMode or selectMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching into editMode while an object is selected
		<4> if nothing is selected, make sure current_tool is disabled
		*/
	}

	// switches into selectMode
	public void EnterSelect ()
	{
		if (selectMode || !(createMode || editMode)) return; // <1>
		if (createMode) tile_buffer = tileCreator.GetTileData(); // <2>

		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <3>
		current_tool.SetActive(false); // <4>
		createMode = false;
		editMode = false;
		selectMode = true;

		/*
		<1> only do anyting if currently in creationMode or editMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching out of editMode while an object is selected
		<4> current_tool should always be disabled in selectMode
		*/
	}

	/* Public Utilities */

	// simply returns whether the given keys were being held during this frame
	public bool CheckInput (InputKeys inKeys)
	{ return (getInputs & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckInputDown (InputKeys inKeys)
	{ return (getInputDowns & inKeys) == inKeys; }

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

	// (!!)(incomplete) deletes the current scene and loads the MainMenu scene
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