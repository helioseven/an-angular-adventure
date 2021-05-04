using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	/* Public Structs */

	// a struct that keeps track of what the hell is going on (what is active/inactive) when switching modes and/or tools
	public struct SelectedItem {

		public GameObject instance;
		public TileData? tileData;
		public ChkpntData? chkpntData;
		public WarpData? warpData;

		// there are a bunch of places where we currently use
		// "new SelectedItem()" where we probably want to be
		// using "SelectedItem.identity" or some such instead

		public SelectedItem (TileData inTile) : this (null, inTile) {}

		public SelectedItem (ChkpntData inChkpnt) : this (null, inChkpnt) {}

		public SelectedItem (WarpData inWarp) : this (null, inWarp) {}

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

		public static bool operator ==(SelectedItem si1, SelectedItem si2)
		{
			if (si1.instance != si2.instance) return false;
			if (si1.tileData != si2.tileData) return false;
			if (si1.chkpntData != si2.chkpntData) return false;
			if (si1.warpData != si2.warpData) return false;
			return true;
		}

		public static bool operator !=(SelectedItem si1, SelectedItem si2) { return !(si1 == si2); }

		// .NET expects this behavior to be overridden when overriding ==/!= operators
		public override bool Equals(System.Object obj)
		{
			SelectedItem? inSI = obj as SelectedItem?;
			if (!inSI.HasValue) return false;
			else return this == inSI.Value;
		}

		// .NET expects this behavior to be overridden when overriding ==/!= operators
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	/* Public Operations */

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode) return; // <1>

		if (selected_item != new SelectedItem()) {
			if (editMode) addSelectedItem(); // <2>

			if (selected_item.tileData.HasValue) {
				tileCreator.SetProperties(selected_item.tileData.Value);
				setTool(EditTools.Tile); // <3>
			}
			if (selected_item.chkpntData.HasValue) setTool(EditTools.Chkpnt);
			if (selected_item.warpData.HasValue) setTool(EditTools.Warp);// <4>
		} else {
			TileData td = tileCreator.GetTileData();
			selected_item = new SelectedItem(td);
			setTool(EditTools.Tile); // <5>
		}

		current_mode = EditorMode.Create;

		/*
		<1> if already in createMode, simply escape
		<2> if exiting editMode, add selected_item back to the level
		<3> if selected_item is a tile, use its tileData to set tile tool
		<4> set tool to chkpnt or warp tool as appropriate
		<5> if no selected_item, default to tile tool
		*/
	}

	// switches into editMode
	public void EnterEdit ()
	{
		if (editMode) return; // <1>

		if (selected_item != new SelectedItem()) {
			if (selected_item.tileData.HasValue) {
				tileCreator.SetProperties(selected_item.tileData.Value);
				setTool(EditTools.Tile); // <2>
			}
			if (selected_item.chkpntData.HasValue) setTool(EditTools.Chkpnt);
			if (selected_item.warpData.HasValue) setTool(EditTools.Warp);
			removeSelectedItem(); // <3>
			Destroy(selected_item.instance);
			selected_item.instance = null;

		} else {
			setTool(EditTools.Tile); // <4>
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

		if (selected_item != new SelectedItem()) {
			if (selected_item.tileData.HasValue) tileCreator.SetProperties(selected_item.tileData.Value); // <2>
			if (editMode) addSelectedItem(); // <3>
			else selected_item = new SelectedItem(); // <4>
		}

		setTool(EditTools.Tile); // <5>
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

		if (editMode && selected_item != new SelectedItem()) addSelectedItem(); // <2>
		if (selected_item.instance == null) selected_item = new SelectedItem();

		current_tool.SetActive(false); // <3>
		current_mode = EditorMode.Select;

		/*
		<1> only do anyting if currently in creationMode or editMode
		<2> conditional logic for switching out of editMode while an object is selected
		<3> current_tool should always be disabled in selectMode
		*/
	}

	// (!!)(incomplete) deletes the current scene and loads the MainMenu scene
	public void ReturnToMainMenu ()
	{ SceneManager.LoadScene(0); } // (!!) should prompt if unsaved

	// (!!)(incomplete) save level to a file in plain text format
	public void SaveFile (string levelName)
	{
		// (!!) should prompt for string instead
		string fname = levelName + ".txt";
		string fpath = Path.Combine(new string[]{"Levels", fname});

		string[] lines = levelData.Serialize();
		File.WriteAllLines(fpath, lines);
	}

	/* Private Operations */

	// sets level name property with passed string
	public void setLevelName (string inName)
	{
		if (inName.Length <= 100) level_name = inName; // <1>

		/*
		<1> level names are capped at 100 characters for now
		*/
	}

	// returns a list of all HUD elements currently under the mouse
	private List<RaycastResult> raycastAllHUD ()
	{
		PointerEventData ped = new PointerEventData(eventSystem);
		ped.position = Input.mousePosition;

		List<RaycastResult> results = new List<RaycastResult>();
		uiRaycaster.Raycast(ped, results);

		return results;
	}
}
