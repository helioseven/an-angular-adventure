using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	/* Private Operations */

	// a struct that keeps track of what the hell is going on (what is active/inactive) when switching modes and/or tools
	private struct SelectedItem {

		public GameObject instance;
		public TileData? tileData;
		public ChkpntData? chkpntData;
		public WarpData? warpData;

		public SelectedItem (GameObject inInstance, TileData inTile)
		{
			instance = inInstance;
			tileData = inTile;
			chkpntData = null;
			warpData = null;
		}

		public SelectedItem (GameObject inInstance, ChkpntData inChkpnt)
		{
			instance = inInstance;
			tileData = null;
			chkpntData = inChkpnt;
			warpData = null;
		}

		public SelectedItem (GameObject inInstance, WarpData inWarp)
		{
			instance = inInstance;
			tileData = null;
			chkpntData = null;
			warpData = inWarp;
		}
	}

	// used when leaving editMode, places a given SelectedItem where it indicates it belongs
	private void addSelectedItem (SelectedItem inItem)
	{
		if (inItem.tileData.HasValue) {
			TileData td = inItem.tileData.Value;
			inItem.instance = addTile(td);
		} else if (inItem.chkpntData.HasValue) {
			ChkpntData cd = inItem.chkpntData.Value;
			inItem.instance = addSpecial(cd);
		} else if (inItem.warpData.HasValue) {
			WarpData wd = inItem.warpData.Value;
			inItem.instance = addSpecial(wd);
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
	private GameObject addTile ()
	{
		TileData td = tileCreator.GetTileData();
		levelData.tileSet.Add(td); // <1>

		Transform tl = tileMap.transform.GetChild(td.orient.layer);
		GameObject go = tileCreator.GetActiveCopy();
		go.transform.SetParent(tl); // <2>

		tile_lookup[go] = td; // <3>
		return go;

		/*
		<1> first, TileData is gathered from tileCreator and added to levelData
		<2> second, a corresponding tile copy from tileCreator is added to tileMap
		<3> lastly, the tile's gameObject is added to the lookup dictionary and returned
		*/
	}

	// adds a passed tileData to the level and returns a reference
	private GameObject addTile (TileData inTile)
	{
		levelData.tileSet.Add(inTile); // <1>

		Transform tl = tileMap.transform.GetChild(inTile.orient.layer);
		GameObject go = tileCreator.NewTile(inTile);
		go.transform.SetParent(tl); // <2>

		tile_lookup[go] = inTile; // <3>
		return go;

		/*
		<1> first, the given TileData is added to levelData
		<2> second, a corresponding new tile is added to tileMap
		<3> lastly, the tile's gameObject is added to the lookup dictionary and returned
		*/
	}

	// removes a given tile from the level
	private void removeTile (GameObject inTile)
	{
		tile_buffer = tileCreator.GetTileData(); // <1>
		TileData tData;
		bool b = IsMappedTile(inTile, out tData); // <2>
		if (b) selected_item = new SelectedItem(inTile, tData);
		else return; // <3>
		tileCreator.SetProperties(tData); // <4>
		setTool(tileCreator.gameObject);

		levelData.tileSet.Remove(tData); // <5>
		tile_lookup.Remove(inTile);
		Destroy(inTile);

		/*
		<1> first, back up tileCreator state
		<2> next, lookup the item's TileData
		<3> if the specified item is not part of tileMap, we ignore
		<4> then set the tileCreator up to act like the selected tile
		<5> after all that, levelData is updated
		<6> reset flag, remove from the lookup, and delete the tile
		*/
	}

	// adds a passed ChkpntData to the level and returns a reference
	private GameObject addSpecial (ChkpntData inChkpnt)
	{
		levelData.chkpntSet.Add(inChkpnt); // <1>

		Vector3 v3 = inChkpnt.locus.ToUnitySpace();
		v3.z = GetLayerDepth(inChkpnt.layer);
		GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
		go.transform.SetParent(chkpntMap.transform); // <2>

		chkpnt_lookup[go] = inChkpnt; // <3>
		return go;

		/*
		<1> first, the given ChkpntData is added to levelData
		<2> second, a corresponding new checkpoint is added to chkpntMap
		<3> lastly, the checkpoint's gameObject is added to the lookup dictionary and returned
		*/
	}

	// adds a passed WarpData to the level and returns a reference
	private GameObject addSpecial (WarpData inWarp)
	{
		levelData.warpSet.Add(inWarp); // <1>

		Vector3 v3 = inWarp.orient.locus.ToUnitySpace();
		v3.z = GetLayerDepth(inWarp.orient.layer);
		GameObject go = Instantiate(warpTool, v3, Quaternion.identity) as GameObject;
		go.transform.SetParent(warpMap.transform); // <2>

		warp_lookup[go] = inWarp; // <3>
		return go;

		/*
		<1> first, the given ChkpntData is added to levelData
		<2> second, a corresponding new checkpoint is added to chkpntMap
		<3> lastly, the checkpoint's gameObject is added to the lookup dictionary and returned
		*/
	}

	// removes a given special from the level
	private void removeSpecial (GameObject inSpecial)
	{
		ChkpntData cData;
		WarpData wData;
		if (IsMappedChkpnt(inSpecial, out cData)) { // <1>
			selected_item = new SelectedItem(inSpecial, cData);
			setTool(chkpntTool);

			levelData.chkpntSet.Remove(cData);
			chkpnt_lookup.Remove(inSpecial);
		} else if (IsMappedWarp(inSpecial, out wData)) { // <2>
			selected_item = new SelectedItem(inSpecial, wData);
			setTool(warpTool);

			levelData.warpSet.Remove(wData);
			warp_lookup.Remove(inSpecial);
		} else return; // <3>

		Destroy(inSpecial);

		/*
		<1> first, check to see whether the given item is a checkpoint
		<2> then check to see whether the given item is a warp
		<3> if neither simply return, otherwise destroy the object
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

	// simply adds layers to the level until there are enough layers to account for the given layer
	private void addLayers(int inLayer)
	{
		if (inLayer < tileMap.transform.childCount) return; // <1>
		for (int i = tileMap.transform.childCount; i <= inLayer; i++) { // <2>
			GameObject tileLayer = new GameObject("Layer #" + i.ToString());
			tileLayer.transform.position = new Vector3(0f, 0f, i * 2f);
			tileLayer.transform.SetParent(tileMap.transform);
		}

		/*
		<1> if there are already more layers than the passed index, simply return
		<2> otherwise, create layers until the passed index is reached
		*/
	}

	// instantiates GameObjects and builds lookup dictionaries based on the given LevelData
	private void buildLevel (LevelData inLevel)
	{
		GameObject[,] prefab_refs = new GameObject[6, 8];
		foreach (Transform tileGroup in tileCreator.transform)
			foreach (Transform tile in tileGroup) {
				int tgi = tileGroup.GetSiblingIndex();
				int ti = tile.GetSiblingIndex();
				prefab_refs[tgi, ti] = tile.gameObject; // <1>
			}

		foreach (TileData td in inLevel.tileSet) { // <2>
			addLayers(td.orient.layer); // <3>
			Transform tileLayer = tileMap.transform.GetChild(td.orient.layer);
			GameObject pfRef = prefab_refs[td.type, td.color];
			Vector3 v3 = td.orient.locus.ToUnitySpace();
			v3.z = tileLayer.position.z;
			Quaternion q = Quaternion.Euler(0, 0, 30 * td.orient.rotation);
			GameObject go = Instantiate(pfRef, v3, q) as GameObject;
			go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(tileLayer);
			tile_lookup.Add(go, td); // <4>
		}

		foreach (ChkpntData cd in inLevel.chkpntSet) { // <5>
			addLayers(cd.layer); // <6>
			Vector3 v3 = cd.locus.ToUnitySpace();
			v3.z = tileMap.transform.GetChild(cd.layer).position.z; // <7>
			GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
			go.GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(chkpntMap.transform);
			chkpnt_lookup.Add(go, cd); // <8>
		}

		foreach (WarpData wd in inLevel.warpSet) { // <9>
			addLayers(wd.orient.layer); // <10>
			Vector3 v3 = wd.orient.locus.ToUnitySpace();
			v3.z = tileMap.transform.GetChild(wd.orient.layer).position.z; // <11>
			Quaternion q = Quaternion.Euler(0, 0, 30 * wd.orient.rotation);
			GameObject go = Instantiate(warpTool, v3, q) as GameObject;
			go.GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(warpMap.transform);
			warp_lookup.Add(go, wd); // <12>
		}

		/*
		<1> first, prefab references are arrayed for indexed access
		<2> build each tile in the level
		<3> make sure there are enough layers for the new tile
		<4> add the GameObject,TileData pair to tile_lookup
		<5> build each checkpoint in the level
		<6> make sure there are enough layers for the new checkpoint
		<7> checkpoints' z positions are assigned by corresponding tileMap layer
		<8> add the GameObject,ChkpntData pair to chkpnt_lookup
		<9> build each warp in the level
		<10> make sure there are enough layers for the new warp
		<11> warps' z positions are assigned by corresponding tileMap layer
		<12> add the GameObject,WarpData pair to warp_lookup
		*/
	}
}