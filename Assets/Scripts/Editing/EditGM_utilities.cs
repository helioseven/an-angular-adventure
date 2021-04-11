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
	public enum EditorMode {
		Select,
		Edit,
		Create,
		Paint
	}

	// EditTools establishes the different tools usable in the editor
	public enum EditTools {
		Tile,
		Chkpnt,
		Warp,
		Eraser
	}

	// InputKeys wraps keyboard input into a bit-flag enum
	[Flags]
	public enum InputKeys {
		None = 0x0,
		HUD = 0x1,
		Palette = 0x2,
		Delete = 0x4,
		ClickMain = 0x8,
		ClickAlt = 0x10,
		Chkpnt = 0x20,
		Warp = 0x40,
		One = 0x80,
		Two = 0x100,
		Three = 0x200,
		Four = 0x400,
		Five = 0x800,
		Six = 0x1000,
		CCW = 0x2000,
		Up = 0x4000,
		CW = 0x8000,
		In = 0x10000,
		Left = 0x20000,
		Down = 0x40000,
		Right = 0x80000,
		Out = 0x100000,
		ColorCCW = 0x200000,
		ColorCW = 0x400000
	}

	/* Private Utilities */

	// cycles through all layers, calculates distance, and sets opacity accordingly
	private void activateLayer (int inLayer)
	{
		bool b = (inLayer < 0) || (inLayer >= tileMap.transform.childCount);
		if (b) return; // <1>
		else activeLayer = inLayer; // <2>

		foreach (Transform layer in tileMap.transform) {
			int layerNumber = layer.GetSiblingIndex();
			int distance = Math.Abs(layerNumber - activeLayer);
			if (activeLayer > layerNumber) distance += 2; // <3>
			setLayerOpacity(layer, distance); // <4>
		}

		// update opacity for all checkpoints
		foreach (Transform checkpoint in chkpntMap.transform) {
			ChkpntData cd;
			bool ok = IsMappedChkpnt(checkpoint.gameObject, out cd);
			int layerNumber = INACTIVE_LAYER;
			if (ok) layerNumber = cd.layer;
			int distance = Math.Abs(layerNumber - activeLayer);
			if (activeLayer > layerNumber) distance += 2;
			setCheckpointOpacity(checkpoint, distance);
		}

		Vector3 v3 = anchorIcon.transform.position;
		v3.z = GetLayerDepth();
		anchorIcon.transform.position = v3; // <5>

		/*
		<1> if invalid layer index is given, fail quietly
		<2> otherwise update activeLayer and continue
		<3> dim layers in front of active layer extra
		<4> ordinal distance from activeLayer is calculated, and opacity set accordingly
		<5> add active layer depth and move the snap cursor to the new location
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

	// used when leaving editMode, places selected_item where it indicates it belongs
	private void addSelectedItem ()
	{
		if (selected_item == new SelectedItem()) return; // <1>
		if (selected_item.tileData.HasValue) {
			TileData td = selected_item.tileData.Value;
			selected_item.instance = addTile(td); // <2>
		} else if (selected_item.chkpntData.HasValue) {
			ChkpntData cd = selected_item.chkpntData.Value;
			selected_item.instance = addSpecial(cd); // <2>
		} else if (selected_item.warpData.HasValue) {
			WarpData wd = selected_item.warpData.Value;
			selected_item.instance = addSpecial(wd); // <2>
		}

		/*
		<1> if nothing is selected, escape
		<2> for each item type, use item data to restore item
		*/
	}

	// adds a passed ChkpntData to the level and returns a reference
	private GameObject addSpecial (ChkpntData inChkpnt)
	{
		levelData.chkpntSet.Add(inChkpnt); // <1>

		Vector3 v3 = inChkpnt.locus.ToUnitySpace();
		v3.z = GetLayerDepth(inChkpnt.layer);
		GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
		go.GetComponent<SpecialCreator>().enabled = false;
		go.transform.SetParent(chkpntMap.transform); // <2>

		chkpnt_lookup[go] = inChkpnt; // <3>
		return go;

		/*
		<1> first, the given ChkpntData is added to levelData
		<2> corresponding checkpoint object is added to chkpntMap
		<3> resulting gameObject is added to lookup dictionary and returned
		*/
	}

	// adds a passed WarpData to the level and returns a reference
	private GameObject addSpecial (WarpData inWarp)
	{
		levelData.warpSet.Add(inWarp); // <1>

		Vector3 v3 = inWarp.orient.locus.ToUnitySpace();
		v3.z = GetLayerDepth(inWarp.orient.layer);
		GameObject go = Instantiate(warpTool, v3, Quaternion.identity) as GameObject;
		go.GetComponent<SpecialCreator>().enabled = false;
		go.transform.SetParent(warpMap.transform); // <2>

		warp_lookup[go] = inWarp; // <3>
		return go;

		/*
		<1> first, the given ChkpntData is added to levelData
		<2> corresponding checkpoint object is added to chkpntMap
		<3> resulting gameObject is added to lookup dictionary and returned
		*/
	}

	// adds a default tile to the level and returns a reference
	private GameObject addTile ()
	{
		TileData td = tileCreator.GetTileData(); // <1>
		return addTile(td);

		/*
		<1> uses tileCreator state for parameterless tile addition
		*/
	}

	// adds a passed tileData to the level and returns a reference
	private GameObject addTile (TileData inTile)
	{
		levelData.tileSet.Add(inTile); // <1>

		GameObject go = tileCreator.NewTile(inTile); // <2>
		Transform tl = tileMap.transform.GetChild(inTile.orient.layer);
		go.transform.SetParent(tl);

		tile_lookup[go] = inTile; // <3>
		return go;

		/*
		<1> first, the given TileData is added to levelData
		<2> then new tile object is created and added to tileMap
		<3> add tile's gameObject to the tile lookup and return it
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
			Quaternion q;
			Vector3 v3 = td.orient.ToUnitySpace(out q);
			GameObject go = Instantiate(pfRef, v3, q) as GameObject;
			go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(tileLayer);
			tile_lookup.Add(go, td); // <4>
		}

		foreach (ChkpntData cd in inLevel.chkpntSet) { // <5>
			addLayers(cd.layer); // <6>
			Vector3 v3 = cd.locus.ToUnitySpace();
			v3.z = GetLayerDepth(cd.layer); // <7>
			GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
			go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
			go.transform.SetParent(chkpntMap.transform);
			go.SetActive(true);
			go.GetComponent<SpecialCreator>().enabled = false;
			chkpnt_lookup.Add(go, cd); // <8>
		}

		foreach (WarpData wd in inLevel.warpSet) { // <9>
			addLayers(wd.orient.layer); // <10>
			Quaternion q;
			Vector3 v3 = wd.orient.ToUnitySpace(out q);
			GameObject go = Instantiate(warpTool, v3, q) as GameObject;
			go.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
			go.transform.SetParent(warpMap.transform);
			go.SetActive(true);
			go.GetComponent<SpecialCreator>().enabled = false;
			warp_lookup.Add(go, wd); // <11>
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
		<11> add the GameObject,WarpData pair to warp_lookup
		*/
	}

	// used when entering editMode with an item selected, which removes it
	private void removeSelectedItem ()
	{
		if (selected_item.tileData.HasValue) {
			removeTile(selected_item.instance);
			tileCreator.SetProperties(selected_item.tileData.Value); // <1>
			setTool(EditTools.Tile); // <2>
		} else if (selected_item.chkpntData.HasValue) {
			removeSpecial(selected_item.instance);
			setTool(EditTools.Chkpnt); // <2>
		} else if (selected_item.warpData.HasValue) {
			removeSpecial(selected_item.instance);
			setTool(EditTools.Warp); // <2>
		}

		/*
		<1> if selected_item is a tile, use tileData to set tileCreator
		<2> in all cases, remove selected_item from level and set tool
		*/
	}

	// removes a given special from the level
	private void removeSpecial (GameObject inSpecial)
	{
		ChkpntData cData;
		WarpData wData;
		if (IsMappedChkpnt(inSpecial, out cData)) { // <1>
			selected_item = new SelectedItem(inSpecial, cData);
			setTool(EditTools.Chkpnt);

			levelData.chkpntSet.Remove(cData);
			chkpnt_lookup.Remove(inSpecial); // <3>
		} else if (IsMappedWarp(inSpecial, out wData)) { // <2>
			selected_item = new SelectedItem(inSpecial, wData);
			setTool(EditTools.Warp);

			levelData.warpSet.Remove(wData);
			warp_lookup.Remove(inSpecial); // <3>
		} else return; // <4>

		Destroy(inSpecial); // <5>

		/*
		<1> check to see whether the given item is a checkpoint
		<2> check to see whether the given item is a warp
		<3> set selected_item and tool then remove item from level and lookup
		<4> if neither, simply return
		<5> if either, destroy the passed object
		*/
	}

	// removes a given tile from the level
	private void removeTile (GameObject inTile)
	{
		TileData tData;
		bool b = IsMappedTile(inTile, out tData); // <1>
		if (!b) return; // <2>

		levelData.tileSet.Remove(tData);
		tile_lookup.Remove(inTile); // <3>

		/*
		<1> lookup the item's TileData
		<2> if the passed GameObject is not part of tileMap, we escape
		<3> otherwise remove tile from the level and tile_lookup
		*/
	}

	// sets the opacity of all tiles within a layer using ordinal distance from activeLayer
	private void setLayerOpacity (Transform tileLayer, int distance)
	{
		float alpha = 1f;
		int layer = DEFAULT_LAYER;
		if (distance != 0) { // <1>
			alpha = (float)Math.Pow(0.5, (double)distance); // <2>
			layer = INACTIVE_LAYER;
		}
		Color color = new Color(1f, 1f, 1f, alpha);

		foreach (Transform tile in tileLayer) {
			tile.gameObject.layer = layer;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = color;
			if (tile.GetChild(0).GetChild(0)) {
				Debug.Log("yea");
				tile.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = color;
			}
		}

		/*
		<1> active layer gets default values, otherwise opacity and layer are calculated
		<2> alpha is calculated as (1/2)^distance
		*/
	}

	// set opacity and physics by given distance for given checkpoint
	private void setCheckpointOpacity (Transform checkpoint, int distance)
	{
		float alpha = 1f;
		int layer = DEFAULT_LAYER;
		if (distance != 0) { // <1>
			alpha = (float)Math.Pow(0.5, (double)distance); // <2>
			layer = INACTIVE_LAYER;
		}
		Color color = new Color(1f, 1f, 1f, alpha);

		checkpoint.gameObject.layer = layer;
		checkpoint.GetChild(0).GetComponent<SpriteRenderer>().color = color;

		/*
		<1> active layer gets default values, otherwise opacity and layer are calculated
		<2> alpha is calculated as (1/2)^distance
		*/
	}

	// sets the currently active tool
	private void setTool (EditTools inTool)
	{
		switch (inTool) {
			case EditTools.Tile:
				current_tool = tileCreator.gameObject;
				break;
			case EditTools.Chkpnt:
				current_tool = chkpntTool;
				break;
			case EditTools.Warp:
				current_tool = warpTool;
				break;
			case EditTools.Eraser:
				// missing implementation
				current_tool = null;
				break;
			default:
				break;
		}

		tool_mode = inTool;
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

	// returns first collider hit on active layer under click
	public Collider2D GetObjectClicked ()
	{
		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		Plane p = new Plane(Vector3.forward, -2f * activeLayer); // <1>
		float f;
		p.Raycast(r, out f);
		Vector3 v3 = r.GetPoint(f); // <2>
		v3.z -= 1f;
		r = new Ray(v3, Vector3.forward);
		return Physics2D.GetRayIntersection(r, 2f).collider; // <3>

		/*
		<1> use plane at active layer depth
		<2> get point of intersection with active layer plane
		<3> cast a forward-facing ray through layer at intersection point
		*/
	}

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
		outData = new ChkpntData();
		if (!inChkpnt || !chkpnt_lookup.ContainsKey(inChkpnt)) { // <1>
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
}
