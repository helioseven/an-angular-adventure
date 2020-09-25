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
			Input.GetButtonDown("Jump"),
			Input.GetButtonDown("Palette"),
			Input.GetButtonDown("Delete"),
			Input.GetButtonDown("Mouse ButtonLeft"),
			Input.GetButtonDown("Mouse ButtonRight"),
			Input.GetButtonDown("ChkpntTool"),
			Input.GetButtonDown("WarpTool"),
			Input.GetButtonDown("Tile1"),
			Input.GetButtonDown("Tile2"),
			Input.GetButtonDown("Tile3"),
			Input.GetButtonDown("Tile4"),
			Input.GetButtonDown("Tile5"),
			Input.GetButtonDown("Tile6"),
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
			if (i >= 0x2000) {
				InputKeys ik = (InputKeys) i;
				if (b[k++] && !CheckInput(ik)) now = now | (InputKeys) i; // <3>
			} else {
				if (b[k++]) now = now | (InputKeys) i;
			}
		}
		getInputDowns = now; // <4>

		b = new bool[23]{ // <5>
			Input.GetAxis("Jump") > 0,
			Input.GetAxis("Palette") > 0,
			Input.GetAxis("Delete") > 0,
			Input.GetAxis("Mouse ButtonLeft") > 0,
			Input.GetAxis("Mouse ButtonRight") > 0,
			Input.GetAxis("ChkpntTool") > 0,
			Input.GetAxis("WarpTool") > 0,
			Input.GetAxis("Tile1") > 0,
			Input.GetAxis("Tile2") > 0,
			Input.GetAxis("Tile3") > 0,
			Input.GetAxis("Tile4") > 0,
			Input.GetAxis("Tile5") > 0,
			Input.GetAxis("Tile6") > 0,
			Input.GetAxis("Rotate") < 0,
			Input.GetAxis("Vertical") > 0,
			Input.GetAxis("Rotate") > 0,
			Input.GetAxis("Depth") > 0,
			Input.GetAxis("Horizontal") < 0,
			Input.GetAxis("Vertical") < 0,
			Input.GetAxis("Horizontal") > 0,
			Input.GetAxis("Depth") < 0,
			Input.GetAxis("CycleColor") < 0,
			Input.GetAxis("CycleColor") > 0
		};

		k = 0;
		now = InputKeys.None;
		for (int i = 1; i <= 0x400000; i = i * 2) {
			if (b[k++]) now = now | (InputKeys) i;
		}
		getInputs = now;

		/*
		<1> do InputDowns first so we can compare vs last frame inputs
		<2> enum bit flags are assigned by powers of 2
		<3> CheckInput tells us whether this input was
		<4> assign public member for inputdown flags
		<5> same as above for regular input flags
		*/
	}

	// updates UI Overlay and Palette panels
	private void updateUI ()
	{
		bool sbCID = CheckInputDown(InputKeys.HUD);
		bool tabCI = CheckInput(InputKeys.Palette);

		if (sbCID) hudPanel.SetActive(!hudPanel.activeSelf); // <1>

		if (paletteMode != tabCI) {
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
		if (CheckInputDown(InputKeys.ClickAlt)) anchorIcon.FindNewAnchor(); // <2>

		if (CheckInputDown(InputKeys.Out)) activateLayer(activeLayer - 1); // <3>
		if (CheckInputDown(InputKeys.In)) activateLayer(activeLayer + 1);

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
			int rot = tileCreator.tileOrient.rotation;
			if (CheckInputDown(InputKeys.CCW)) tileCreator.SetRotation(rot + 1); // <2>
			if (CheckInputDown(InputKeys.CW)) tileCreator.SetRotation(rot - 1);

			if (CheckInputDown(InputKeys.ColorCCW)) tileCreator.CycleColor(false);
			if (CheckInputDown(InputKeys.ColorCW)) tileCreator.CycleColor(true);

			if (CheckInputDown(InputKeys.ClickMain)) addTile(); // <3>
		}

		Vector3 pos = anchorIcon.focus.ToUnitySpace(); // <4>
		pos.z = anchorIcon.transform.position.z;
		if (b2) {
			chkpntTool.transform.position = pos;

			if (CheckInputDown(InputKeys.ClickMain)) addSpecial(new ChkpntData(anchorIcon.focus, activeLayer));
		}
		if (b3) {
			warpTool.transform.position = pos;

			WarpData wd = new WarpData(false, true, new HexOrient(anchorIcon.focus, 0, activeLayer), activeLayer + 1);
			if (CheckInputDown(InputKeys.ClickMain)) addSpecial(wd);
		}

		if (!b2 && CheckInputDown(InputKeys.Chkpnt)) setTool(chkpntTool); // <5>
		if (!b3 && CheckInputDown(InputKeys.Warp)) setTool(warpTool);
		bool bType = false;
		if (CheckInputDown(InputKeys.One)) { tileCreator.SelectType(0); bType = true; }
		if (CheckInputDown(InputKeys.Two)) { tileCreator.SelectType(1); bType = true; }
		if (CheckInputDown(InputKeys.Three)) { tileCreator.SelectType(2); bType = true; }
		if (CheckInputDown(InputKeys.Four)) { tileCreator.SelectType(3); bType = true; }
		if (CheckInputDown(InputKeys.Five)) { tileCreator.SelectType(4); bType = true; }
		if (CheckInputDown(InputKeys.Six)) { tileCreator.SelectType(5); bType = true; }
		if (!b1 && bType) setTool(tileCreator.gameObject); // <6>

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
		if (selected_item.HasValue) {
			SelectedItem si = selected_item.Value;
			if (CheckInputDown(InputKeys.ClickMain)) {
				if (si.tileData.HasValue) {
					addTile(); // <2>
					tileCreator.SetProperties(tile_buffer);
				}

				current_tool.SetActive(false); // <3>
				selected_item = null;
			}
		} else if (CheckInputDown(InputKeys.ClickMain)) { // <5>
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
			if (!c2d) { // <7>
				selected_item = null;
				return;
			}
			GameObject go = c2d.gameObject;
			TileData td;
			if (IsMappedTile(go, out td)) { // <8>
				selected_item = new SelectedItem(go, td);
				removeTile(go);
			}
			ChkpntData cd;
			WarpData wd;
			if (IsMappedChkpnt(go, out cd)) selected_item = new SelectedItem(go, cd);
			if (IsMappedWarp(go, out wd)) selected_item = new SelectedItem(go, wd);
			removeSpecial(go); // <9>
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if there is a selected tile, left click will place it again
		<3> then restore tileCreator, turn off current_tool, deselect selected_item, and return
		<4> if there is a selected tile, Delete will simply forget it
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked on, null out selected_item and return
		<8> if a tile was clicked on, it is made into a new SelectedItem and removed
		<9> otherwise out click was on a special, which is made into a new SelectedItem and removed
		*/
	}

	// makes changes associated with being in selectMode
	private void updateSelect ()
	{
		if (CheckInputDown(InputKeys.ClickMain)) { // <1>
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <2>
			if (!c2d || (selected_item.HasValue && (selected_item.Value.instance == c2d.gameObject))) { // <3>
				selected_item = null;
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
}