using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	/* Update Mechanisms */

	// updates getInputs and getInputDowns with appropriate InputKeys
	private void updateInputs ()
	{
		bool[] b = new bool[23]{ // <1>
			Input.GetButton("Jump"),
			Input.GetButton("Palette"),
			Input.GetButton("Delete"),
			Input.GetButton("Mouse ButtonLeft"),
			Input.GetButton("Mouse ButtonRight"),
			Input.GetButton("ChkpntTool"),
			Input.GetButton("WarpTool"),
			Input.GetButton("Tile1"),
			Input.GetButton("Tile2"),
			Input.GetButton("Tile3"),
			Input.GetButton("Tile4"),
			Input.GetButton("Tile5"),
			Input.GetButton("Tile6"),
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
		};

		int k = 0;
		InputKeys now = InputKeys.None;
		for (int i = 1; i <= 0x400000; i = i * 2) { // <2>
			InputKeys ik = (InputKeys) i;
			if (b[k++] && !CheckInput(ik)) now = now | (InputKeys) i; // <3>
		}
		getInputDowns = now; // <4>

		k = 0;
		now = InputKeys.None;
		for (int i = 1; i <= 0x400000; i = i * 2) { // <5>
			if (b[k++]) now = now | (InputKeys) i;
		}
		getInputs = now;

		/*
		<1> get inputs from InputManager
		<2> enum bit flags are assigned by powers of 2
		<3> CheckInput relies on last frame data before its been updated
		<4> assign public member for inputdown flags
		<5> same as above for regular input flags
		*/
	}

	// makes changes associated with anchorIcon and layer changes
	private void updateLevel ()
	{
		if (CheckInputDown(InputKeys.ClickAlt)) anchorIcon.FindNewAnchor(); // <2>

		if (CheckInputDown(InputKeys.Out)) activateLayer(activeLayer - 1); // <3>
		if (CheckInputDown(InputKeys.In)) activateLayer(activeLayer + 1);

		/*
		<2> right-click will update snap cursor location
		<3> F and R will change active layer
		*/
	}

	// updates UI Overlay and Palette panels
	private void updateUI ()
	{
		bool isHUD = CheckInputDown(InputKeys.HUD);
		bool isPal = CheckInput(InputKeys.Palette);

		if (isHUD) hudPanel.SetActive(!hudPanel.activeSelf); // <1>

		if (paletteMode != isPal) {
			paletteMode = isPal;
			palettePanel.TogglePalette(); // <2>

			if (paletteMode) {
				current_tool.SetActive(false); // <3>
			} else {
				bool b = false; // <4>
				if (createMode) b = true;
				if (editMode && selected_item != new SelectedItem()) b = true;
				if (paintMode) b = true;

				if (b) current_tool.SetActive(true); // <5>
			}
		}

		/*
		<1> UI is toggled whenever spacebar is pressed
		<2> palette is toggled on whenever tab key is held down
		<3> whenever palette activates, current_tool is turned off
		<4> determine if current_tool should be active when palette deactivates
		<5> turn current_tool back on if so
		*/
	}

	// makes changes associated with being in createMode
	private void updateCreate ()
	{
		if (tool_mode == EditTools.Eraser) return; // <1>

		bool chkclck = CheckInputDown(InputKeys.ClickMain);
		if (tool_mode == EditTools.Tile) {
			updateTileProperties(); // <2>

			if (chkclck) addTile(); // <3>
		} else {
			Vector3 pos = anchorIcon.focus.ToUnitySpace(); // <4>
			pos.z = anchorIcon.transform.position.z;
			if (tool_mode == EditTools.Chkpnt && chkclck) {
				chkpntTool.transform.position = pos;
				ChkpntData cd = new ChkpntData(anchorIcon.focus, activeLayer);
				addSpecial(cd);
			}
			if (tool_mode == EditTools.Warp && chkclck) {
				warpTool.transform.position = pos;
				HexOrient ho = new HexOrient(anchorIcon.focus, 0, activeLayer);
				WarpData wd = new WarpData(false, true, ho, activeLayer + 1);
				addSpecial(wd);
			}
		}

		if (tool_mode != EditTools.Chkpnt && CheckInputDown(InputKeys.Chkpnt)) {
			current_tool.SetActive(false);
			setTool(EditTools.Chkpnt); // <5>
		}
		if (tool_mode != EditTools.Warp && CheckInputDown(InputKeys.Warp)) {
			current_tool.SetActive(false);
			setTool(EditTools.Warp);
		}

		InputKeys nums = InputKeys.One;
		nums |= InputKeys.Two;
		nums |= InputKeys.Three;
		nums |= InputKeys.Four;
		nums |= InputKeys.Five;
		nums |= InputKeys.Six;
		nums &= getInputDowns;
		if (nums != InputKeys.None) {
			current_tool.SetActive(false);
			setTool(EditTools.Tile); // <6>
		}

		current_tool.SetActive(true);

		/*
		<1> first, figure out which tool is active and return if none
		<2> Q and E rotate the tileCreator C-CW and CW, respectively
		<3> and then if left click is made, tile is added to the level
		<4> if one of the other two tools is active, we get an orientation for them
		<5> C and V activate the checkpoint and warp tools, respectively
		<6> numeric keys assign tile type and activate tileCreator tool
		*/
	}

	// makes changes associated with being in editMode
	private void updateEdit ()
	{
		if (selected_item != new SelectedItem()) {
			if (tool_mode == EditTools.Eraser) return; // <1>

			bool chkclck = CheckInputDown(InputKeys.ClickMain);
			Vector3 pos = anchorIcon.focus.ToUnitySpace(); // <4>
			pos.z = anchorIcon.transform.position.z;
			SelectedItem si = selected_item;
			if (tool_mode == EditTools.Tile) {
				updateTileProperties(); // <2>

				if (chkclck) addTile();
			} else {
				if (tool_mode == EditTools.Chkpnt && chkclck) {
					chkpntTool.transform.position = pos;
					ChkpntData cd = new ChkpntData(anchorIcon.focus, activeLayer);
					addSpecial(cd);
				}
				if (tool_mode == EditTools.Warp && chkclck) {
					warpTool.transform.position = pos;
					HexOrient ho = new HexOrient(anchorIcon.focus, 0, activeLayer);
					WarpData wd = new WarpData(false, true, ho, activeLayer + 1);
					addSpecial(wd);
				}
			}

			if (chkclck) {
				current_tool.SetActive(false); // <3>
				selected_item = new SelectedItem();
				return;
			}

			if (CheckInputDown(InputKeys.Delete)) { // <4>
				current_tool.SetActive(false);
				Destroy(selected_item.instance);
				selected_item = new SelectedItem();
			}
		} else if (CheckInputDown(InputKeys.ClickMain)) { // <5>
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
			if (!c2d) { // <7>
				selected_item = new SelectedItem();
				return;
			}
			GameObject go = c2d.gameObject;
			TileData td;
			if (IsMappedTile(go, out td)) { // <8>
				if (td.orient.layer != activeLayer) return;
				selected_item = new SelectedItem(null, td);
				tileCreator.SetProperties(td);
				removeTile(go); // <9>
				Destroy(go);
			} else {
				ChkpntData cd;
				WarpData wd;
				if (IsMappedChkpnt(go, out cd)) {
					selected_item = new SelectedItem(null, cd);
					setTool(EditTools.Chkpnt);
				}
				if (IsMappedWarp(go, out wd)) {
					selected_item = new SelectedItem(null, wd);
					setTool(EditTools.Warp);
				}
				removeSpecial(go); // <10>
				Destroy(go);
			}
			current_tool.SetActive(true);
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if tile selected, rotate, color-change, or replace according to input
		<3> if any tool used, turn off current_tool, deselect selected_item, and return
		<4> if there is a selected tile, Delete will destroy instance and forget
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked, null out selected_item and return
		<8> if tile is clicked, make it into new SelectedItem and remove
		<9> once SelectedItem emulates the tile, destroy it
		<10> if special is clicked, same as tile more or less
		*/
	}

	// make changes associated with being in paintMode
	private void updatePaint()
	{
		// stub
	}

	// makes changes associated with being in selectMode
	private void updateSelect ()
	{
		if (CheckInputDown(InputKeys.ClickMain)) { // <1>
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <2>
			if (!c2d || (selected_item.instance && (selected_item.instance == c2d.gameObject))) { // <3>
				selected_item = new SelectedItem();
				return;
			} else { // <4>
				GameObject go = c2d.gameObject;
				TileData td;
				if (IsMappedTile(go, out td)) selected_item = new SelectedItem(go, td);
				ChkpntData cd;
				if (IsMappedChkpnt(go, out cd)) selected_item = new SelectedItem(go, cd);
				WarpData wd;
				if (IsMappedWarp(go, out wd)) selected_item = new SelectedItem(go, wd);
			}
		}

		/*
		<1> in select mode, clicking is the only function
		<2> first find out what (if anything) was clicked on
		<3> if nothing was clicked on, or if the currently selected tile was clicked on, deselect and return
		<4> otherwise we select according to what was clicked on
		*/
	}

	//
	private void updateTileProperties ()
	{
		// update tile rotation
		int rot = tileCreator.tileOrient.rotation;
		if (CheckInputDown(InputKeys.CCW)) tileCreator.SetRotation(rot + 1);
		if (CheckInputDown(InputKeys.CW)) tileCreator.SetRotation(rot - 1);

		// update tile color
		if (CheckInputDown(InputKeys.ColorCCW)) tileCreator.CycleColor(false);
		if (CheckInputDown(InputKeys.ColorCW)) tileCreator.CycleColor(true);

		// update tile type
		if (CheckInputDown(InputKeys.One)) tileCreator.SelectType(0);
		if (CheckInputDown(InputKeys.Two)) tileCreator.SelectType(1);
		if (CheckInputDown(InputKeys.Three)) tileCreator.SelectType(2);
		if (CheckInputDown(InputKeys.Four)) tileCreator.SelectType(3);
		if (CheckInputDown(InputKeys.Five)) tileCreator.SelectType(4);
		if (CheckInputDown(InputKeys.Six)) tileCreator.SelectType(5);
	}
}