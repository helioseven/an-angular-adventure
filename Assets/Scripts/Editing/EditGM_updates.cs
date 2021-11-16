using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

    /* Update() Sub-Routines */

    // updates getInputs and getInputDowns with appropriate InputKeys
    private void updateInputs ()
    {
        // get inputs from InputManager
        bool[] b = {
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
        // enum bit flags are assigned by powers of 2
        for (int i = 1; i <= 0x400000; i = i * 2) {
            InputKeys ik = (InputKeys) i;
            // CheckInput relies on last frame data before its been updated
            if (b[k++] && !CheckInput(ik)) now = now | (InputKeys) i;
        }
        // assign public member for inputdown flags
        getInputDowns = now;

        // then same as above for regular input flags
        k = 0;
        now = InputKeys.None;
        for (int i = 1; i <= 0x400000; i = i * 2)
            if (b[k++]) now = now | (InputKeys) i;
        getInputs = now;
    }

    // makes changes associated with anchorIcon and layer changes
    private void updateLevel ()
    {
        // right-click will update snap cursor location
        if (CheckInputDown(InputKeys.ClickAlt))
            anchorIcon.FindNewAnchor();
        // F and R will change active layer
        if (CheckInputDown(InputKeys.Out))
            activateLayer(activeLayer - 1);
        if (CheckInputDown(InputKeys.In))
            activateLayer(activeLayer + 1);
    }

    // updates UI Overlay and Palette panels
    private void updateUI ()
    {
        bool isHUD = CheckInputDown(InputKeys.HUD);
        bool isPal = CheckInput(InputKeys.Palette);

        // UI is toggled whenever spacebar is pressed
        if (isHUD)
            hudPanel.SetActive(!hudPanel.activeSelf);

        // palette is toggled on whenever tab key is held down
        if (paletteMode != isPal) {
            paletteMode = isPal;
            palettePanel.TogglePalette();

            if (paletteMode) {
                // whenever palette activates, _currentTool is turned off
                _currentTool.SetActive(false);
            } else {
                // when palette deactivates, determine desired _currentTool activity
                bool b = false;
                if (createMode)
                    b = true;
                if (editMode && _selectedItem != SelectedItem.noSelection)
                    b = true;
                if (paintMode)
                    b = true;

                // turn _currentTool back on if necessary
                if (b)
                    _currentTool.SetActive(true);
            }
        }
    }

    // makes changes associated with being in createMode
    private void updateCreate ()
    {
        // break if eraser is active, because it shouldn't be (for now)
        if (_toolMode == EditTools.Eraser)
            return;

        // process input for tool and update active tool accordingly
        updateTool();

        // C and V activate the checkpoint and warp tools, respectively
        if (CheckInputDown(InputKeys.Chkpnt)) {
            _currentTool.SetActive(false);
            setTool(EditTools.Chkpnt);
        }
        if (CheckInputDown(InputKeys.Warp)) {
            _currentTool.SetActive(false);
            setTool(EditTools.Warp);
        }

        // if numeric key was pressed, set tileCreator as tool
        InputKeys nums = InputKeys.One;
        nums |= InputKeys.Two;
        nums |= InputKeys.Three;
        nums |= InputKeys.Four;
        nums |= InputKeys.Five;
        nums |= InputKeys.Six;
        nums &= getInputDowns;
        if (nums != InputKeys.None) {
            _currentTool.SetActive(false);
            updateTileProperties();
            setTool(EditTools.Tile);
        }

        // whichever tool is being used should always be active
        _currentTool.SetActive(true);
    }

    // makes changes associated with being in editMode
    private void updateEdit ()
    {
        // first, handle the case where an item is currently selected
        if (_selectedItem != SelectedItem.noSelection) {
            if (_toolMode == EditTools.Eraser)
                // break if eraser is active, because it shouldn't be (for now)
                return;

            // make updates associated with active tool according to input
            updateTool();

            if (CheckInputDown(InputKeys.ClickMain)) {
                // if tool has been used, clean up
                _currentTool.SetActive(false);
                _selectedItem = SelectedItem.noSelection;
                return;
            }

            // Delete key will destroy instance and forget _selectedItem
            if (CheckInputDown(InputKeys.Delete)) {
                _currentTool.SetActive(false);
                Destroy(_selectedItem.instance);
                _selectedItem = SelectedItem.noSelection;
            }
        } else if (CheckInputDown(InputKeys.ClickMain)) {
            // next, handle the case where there is no selected tile
            Collider2D c2d = GetObjectClicked();
            if (!c2d) {
                // left-click selects a tile, if click misses then break
                _selectedItem = SelectedItem.noSelection;
                return;
            }

            GameObject go = c2d.gameObject;
            TileData td;
            // check if clicked object is a mapped tile
            if (IsMappedTile(go, out td)) {
                // if clicked tile isn't a part of activeLayer, ignore it
                if (td.orient.layer != activeLayer)
                    return;
                _selectedItem = new SelectedItem(null, td);
                // use tileData to populate selected item and tool properties
                tileCreator.SetProperties(td);
                setTool(EditTools.Tile);

                // when done using data, remove and destroy GameObject
                removeTile(go);
                Destroy(go);
            } else {
                // if special is clicked, same as above with extra checks
                ChkpntData cd;
                WarpData wd;
                if (IsMappedChkpnt(go, out cd)) {
                    _selectedItem = new SelectedItem(null, cd);
                    setTool(EditTools.Chkpnt);
                }
                if (IsMappedWarp(go, out wd)) {
                    _selectedItem = new SelectedItem(null, wd);
                    _warpTool.SetOrientation(wd.orient);
                    setTool(EditTools.Warp);
                }
                removeSpecial(go);
                Destroy(go);
            }
            _currentTool.SetActive(true);
        }
    }

    // make changes associated with being in paintMode
    private void updatePaint()
    {
        // stub
    }

    // makes changes associated with being in selectMode
    private void updateSelect ()
    {
        // in select mode, clicking is the only function
        if (CheckInputDown(InputKeys.ClickMain)) {
            // first find out what (if anything) was clicked on
            Collider2D c2d = GetObjectClicked();
            GameObject si = _selectedItem.instance;
            if (!c2d || (si && (si == c2d.gameObject))) {
                // if nothing or selected tile is clicked on, deselect and return
                _selectedItem = SelectedItem.noSelection;
                return;
            } else {
                // otherwise select according to what was clicked on
                GameObject go = c2d.gameObject;
                TileData td;
                if (IsMappedTile(go, out td))
                    _selectedItem = new SelectedItem(go, td);
                ChkpntData cd;
                if (IsMappedChkpnt(go, out cd))
                    _selectedItem = new SelectedItem(go, cd);
                WarpData wd;
                if (IsMappedWarp(go, out wd))
                    _selectedItem = new SelectedItem(go, wd);
            }
        }
    }

    // handles input that modifies the tile creator tool
    private void updateTileProperties ()
    {
        // update tile rotation
        int rot = tileCreator.tileOrient.rotation;
        int oldRot = rot;
        if (CheckInputDown(InputKeys.CCW))
            rot++;
        if (CheckInputDown(InputKeys.CW))
            rot--;
        if (rot != oldRot)
            tileCreator.SetRotation(rot);

        // update tile color
        if (CheckInputDown(InputKeys.ColorCCW))
            tileCreator.CycleColor(false);
        if (CheckInputDown(InputKeys.ColorCW))
            tileCreator.CycleColor(true);

        // update tile type
        if (CheckInputDown(InputKeys.One))
            tileCreator.SelectType(0);
        if (CheckInputDown(InputKeys.Two))
            tileCreator.SelectType(1);
        if (CheckInputDown(InputKeys.Three))
            tileCreator.SelectType(2);
        if (CheckInputDown(InputKeys.Four))
            tileCreator.SelectType(3);
        if (CheckInputDown(InputKeys.Five))
            tileCreator.SelectType(4);
        if (CheckInputDown(InputKeys.Six))
            tileCreator.SelectType(5);
    }

    // handles input relating to the current tool
    private void updateTool ()
    {
        bool chkclck = CheckInputDown(InputKeys.ClickMain);
        switch (_toolMode) {
            // when using tile tool, always update tile creator properties first
            case EditTools.Tile:
                updateTileProperties();
                // if main click, add relevant tool's item to the level
                if (chkclck) {
                    GameObject go = addTile();
                    _selectedItem = new SelectedItem(go, _tileLookup[go]);
                }
                break;
            case EditTools.Chkpnt:
                // if main click, add relevant tool's item to the level
                if (chkclck) {
                    ChkpntData cd = new ChkpntData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(cd);
                    _selectedItem = new SelectedItem(go, cd);
                }
                break;
            case EditTools.Warp:
                // set rotation of the warp tool, if necessary
                int rot = _warpTool.specOrient.rotation;
                int oldRot = rot;
                if (CheckInputDown(InputKeys.CCW))
                    rot++;
                if (CheckInputDown(InputKeys.CW))
                    rot--;
                if (rot != oldRot)
                    _warpTool.SetRotation(rot);

                // if main click, add relevant tool's item to the level
                if (chkclck) {
                    HexOrient ho = new HexOrient(anchorIcon.focus, rot, activeLayer);
                    WarpData wd = new WarpData(false, true, ho, activeLayer + 1);
                    GameObject go = addSpecial(wd);
                    _selectedItem = new SelectedItem(go, wd);
                }
                break;
            default:
                break;
        }
    }
}
