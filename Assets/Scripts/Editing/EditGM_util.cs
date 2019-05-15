using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getKeys & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getKeyDowns & inKeys) == inKeys; }

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