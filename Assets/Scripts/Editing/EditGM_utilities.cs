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

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getInputs & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getInputDowns & inKeys) == inKeys; }

	// returns an InputKeys according to various axes and buttons
	public InputKeys GetAllInputs () {
		bool[] b = new bool[23]{ // <1>
			Input.GetButton("Jump"),
			Input.GetButton("Palette"),
			Input.GetButton("Delete"),
			Input.GetButton("Mouse ButtonLeft"),
			Input.GetButton("Mouse ButtonRight"),
			Input.GetAxis("Rotate") < 0,
			Input.GetAxis("Vertical") > 0,
			Input.GetAxis("Rotate") > 0,
			Input.GetAxis("Depth") > 0,
			Input.GetAxis("Horizontal") < 0,
			Input.GetAxis("Vertical") < 0,
			Input.GetAxis("Horizontal") > 0,
			Input.GetAxis("Depth") < 0,
			Input.GetAxis("CycleColor") < 0,
			Input.GetAxis("CycleColor") > 0,
			Input.GetButton("ChkpntTool"),
			Input.GetButton("WarpTool"),
			Input.GetButton("Tile1"),
			Input.GetButton("Tile2"),
			Input.GetButton("Tile3"),
			Input.GetButton("Tile4"),
			Input.GetButton("Tile5"),
			Input.GetButton("Tile6")
		};

		int k = 0;
		InputKeys now = InputKeys.None;
		for (int i = 1; i < 0x400001; i = i * 2) { // <2>
			if (b[k++]) now = now | (InputKeys) i;
		}

		return now;

		/*
		<1> first, determine the state of various inputs
		<2> then assign enum flags by powers of 2
		*/
	}

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