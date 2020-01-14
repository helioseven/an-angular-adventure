using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

	// updates getKeys and getKeyDowns each frame
	private void updateInputs ()
	{
		getKeys = InputKeys.None;
		getKeyDowns = InputKeys.None;
		int k = 0;

		for (int i = 0; i < 0x400001; i = (i == 0) ? 1 : i * 2) { // <1>
			KeyCode kc = key_code_list[k++];
			if (Input.GetKey(kc)) getKeys = getKeys | (InputKeys) i;
			if (Input.GetKeyDown(kc)) getKeyDowns = getKeyDowns | (InputKeys) i;
		}

		/*
		<1> assigns enum flags by powers of 2
		*/
	}

	// updates UI Overlay and Palette panels
	private void updateUI ()
	{
		bool sbCKD = CheckKeyDowns(InputKeys.Space);
		bool tabCK = CheckKeys(InputKeys.Tab);

		if (sbCKD) hudPanel.SetActive(!hudPanel.activeSelf); // <1>

		if (paletteMode != tabCK) {
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
		if (CheckKeyDowns(InputKeys.Click1)) anchorIcon.FindNewAnchor(); // <2>

		if (CheckKeyDowns(InputKeys.F)) activateLayer(activeLayer - 1); // <3>
		if (CheckKeyDowns(InputKeys.R)) activateLayer(activeLayer + 1);

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
			if (CheckKeyDowns(InputKeys.Q)) tileCreator.SetRotation(rot + 1); // <2>
			if (CheckKeyDowns(InputKeys.E)) tileCreator.SetRotation(rot - 1);

			if (CheckKeyDowns(InputKeys.Z)) tileCreator.CycleColor(false);
			if (CheckKeyDowns(InputKeys.X)) tileCreator.CycleColor(true);

			if (CheckKeyDowns(InputKeys.Click0)) addTile(); // <3>
		}

		Vector3 pos = anchorIcon.focus.ToUnitySpace(); // <4>
		pos.z = anchorIcon.transform.position.z;
		if (b2) {
			chkpntTool.transform.position = pos;

			if (CheckKeyDowns(InputKeys.Click0)) addSpecial(new ChkpntData(anchorIcon.focus, activeLayer));
		}
		if (b3) {
			warpTool.transform.position = pos;

			WarpData wd = new WarpData(false, true, new HexOrient(anchorIcon.focus, 0, activeLayer), activeLayer + 1);
			if (CheckKeyDowns(InputKeys.Click0)) addSpecial(wd);
		}

		if (!b2 && CheckKeyDowns(InputKeys.C)) setTool(chkpntTool); // <5>
		if (!b3 && CheckKeyDowns(InputKeys.V)) setTool(warpTool);
		bool bType = false;
		if (CheckKeyDowns(InputKeys.One)) { tileCreator.SelectType(0); bType = true; }
		if (CheckKeyDowns(InputKeys.Two)) { tileCreator.SelectType(1); bType = true; }
		if (CheckKeyDowns(InputKeys.Three)) { tileCreator.SelectType(2); bType = true; }
		if (CheckKeyDowns(InputKeys.Four)) { tileCreator.SelectType(3); bType = true; }
		if (CheckKeyDowns(InputKeys.Five)) { tileCreator.SelectType(4); bType = true; }
		if (CheckKeyDowns(InputKeys.Six)) { tileCreator.SelectType(5); bType = true; }
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
			if (CheckKeyDowns(InputKeys.Click0)) {
				if (si.tileData.HasValue) {
					addTile(); // <2>
					tileCreator.SetProperties(tile_buffer);
				}

				current_tool.SetActive(false); // <3>
				selected_item = null;
				return;
			}

			if (CheckKeyDowns(InputKeys.Delete)) { // <4>
				current_tool.SetActive(false);
				selected_item = null;
			}
		} else if (CheckKeyDowns(InputKeys.Click0)) { // <5>
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
		if (CheckKeyDowns(InputKeys.Click0)) { // <1>
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