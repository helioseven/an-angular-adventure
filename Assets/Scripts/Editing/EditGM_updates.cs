using circleXsquares;
using UnityEngine;

public partial class EditGM
{
    /* Update() Sub-Routines */

    // updates getInputs and getInputDowns with appropriate InputKeys
    private void updateInputs()
    {
        // get inputs from InputManager
        bool[] b =
        {
            Input.GetButton("Jump"),
            Input.GetButton("Palette"),
            Input.GetButton("Delete"),
            Input.GetButton("Mouse ButtonLeft"),
            Input.GetButton("Mouse ButtonRight"),
            Input.GetButton("CheckpointTool"),
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
            Input.GetButton("VictoryTool"),
        };

        int k = 0;
        InputKeys now = InputKeys.None;
        // enum bit flags are assigned by powers of 2
        for (int i = 1; i <= 0x800000; i = i * 2)
        {
            InputKeys ik = (InputKeys)i;
            // CheckInput relies on last frame data before its been updated
            if (b[k++] && !CheckInput(ik))
                now = now | (InputKeys)i;
        }
        // assign public member for inputdown flags
        getInputDowns = now;

        // then same as above for regular input flags
        k = 0;
        now = InputKeys.None;
        for (int i = 1; i <= 0x400000; i = i * 2)
            if (b[k++])
                now = now | (InputKeys)i;
        getInputs = now;

        // get raycast results for this frame's mouse position
        _currentHUDhover = raycastAllHUD();
    }

    // makes changes associated with anchorIcon and layer changes
    private void updateLevel()
    {
        // right-click will update snap cursor location
        if (CheckInputDown(InputKeys.ClickAlt))
            anchorIcon.FindNewAnchor();
        // F and R will change active layer
        if (CheckInputDown(InputKeys.Out))
            MoveUpLayer();
        if (CheckInputDown(InputKeys.In))
            MoveDownLayer();
    }

    // updates UI Overlay and Palette panels
    private void updateUI()
    {
        if (_inputMode)
            return;

        bool isHUD = CheckInputDown(InputKeys.HUD);
        bool isPal = CheckInput(InputKeys.Palette);

        // UI is toggled whenever spacebar is pressed
        if (isHUD)
            hudPanel.SetActive(!hudPanel.activeSelf);
        // check for HUD hover only when HUD panel is active, otherwise false
        hoveringHUD = hudPanel.activeSelf ? checkHUDHover() : false;

        // palette is toggled on whenever tab key is held down
        if (paletteMode != isPal)
        {
            paletteMode = isPal;
            palettePanel.TogglePalette();
        }

        if (hoveringHUD || paletteMode)
        {
            // whenever palette activates, _currentCreatorToolGameObject is turned off
            _currentCreatorToolGameObject.SetActive(false);
        }
        else
        {
            // when palette deactivates, determine desired _currentCreatorToolGameObject activity
            // and turn _currentCreatorToolGameObject back on if necessary
            _currentCreatorToolGameObject.SetActive(isEditorInCreateMode);
        }
    }

    // makes changes associated with being in isEditorInCreateMode
    private void updateCreate()
    {
        if (_suppressClickThisFrame)
        {
            _suppressClickThisFrame = false;
            return;
        }
        else
        {
            // process input for tool and update active tool accordingly
            UpdateTool();
        }

        // C, V, and B activate the checkpoint, victory, and warp tools, respectively
        if (CheckInputDown(InputKeys.Checkpoint))
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Checkpoint);
            soundManager.Play("checkpoint");
        }
        if (CheckInputDown(InputKeys.Victory))
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Victory);
            soundManager.Play("victory");
        }
        if (CheckInputDown(InputKeys.Warp))
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Warp);
            soundManager.Play("warp");
        }

        // if numeric key was pressed, set tileCreator as tool
        InputKeys nums = InputKeys.One;
        nums |= InputKeys.Two;
        nums |= InputKeys.Three;
        nums |= InputKeys.Four;
        nums |= InputKeys.Five;
        nums |= InputKeys.Six;
        nums &= getInputDowns;
        if (nums != InputKeys.None)
        {
            _currentCreatorToolGameObject.SetActive(false);
            updateTileProperties();
            setTool(EditCreatorTool.Tile);
        }

        // whichever tool is being used should always be active
        _currentCreatorToolGameObject.SetActive(true);
    }

    public void SetSelectedItemSpecial(string s)
    {
        if (_selectedItem.tileData.HasValue)
        {
            TileData tileDataModified = new TileData(
                _selectedItem.tileData.Value.type,
                _selectedItem.tileData.Value.color,
                int.Parse(s),
                _selectedItem.tileData.Value.orient,
                _selectedItem.tileData.Value.doorId
            );
            removeTile(_selectedItem.instance);
            Destroy(_selectedItem.instance);
            GameObject newTile = addTile(tileDataModified);
            _selectedItem.instance = newTile;
        }
    }

    public void SetSelectedItemDoorId(string s)
    {
        if (_selectedItem.tileData.HasValue)
        {
            TileData tileDataModified = new TileData(
                _selectedItem.tileData.Value.type,
                _selectedItem.tileData.Value.color,
                _selectedItem.tileData.Value.special,
                _selectedItem.tileData.Value.orient,
                int.Parse(s)
            );
            removeTile(_selectedItem.instance);
            Destroy(_selectedItem.instance);
            GameObject newTile = addTile(tileDataModified);
            _selectedItem.instance = newTile;
        }
    }

    // makes changes associated with being in isEditorInEditMode
    private void updateEdit()
    {
        bool noItemCurrentlySelected =
            !_selectedItem.instance || _selectedItem == SelectedItem.noSelection;
        bool isDoubleClick = false;
        GameObject go;
        TileData td;

        // first, handle the case where an item is currently selected
        if (!noItemCurrentlySelected)
        {
            if (CheckInputDown(InputKeys.ClickMain))
            {
                // check if you clicked the same thing
                Collider2D c2d = GetObjectClicked();

                // if nothing is clicked on
                if (!c2d)
                {
                    _selectedItem = SelectedItem.noSelection;
                    return;
                }
                // otherwise if the same item is clicked on
                else if (_selectedItem.instance == c2d.gameObject)
                {
                    isDoubleClick = true;
                }
                // single click on new item
                else if (!isDoubleClick)
                {
                    // Debug.Log(
                    //     " ----SINGLE CLICK HANDLER---- a different item has been selected"
                    // );
                    go = c2d.gameObject;
                    if (IsMappedTile(go, out td))
                        _selectedItem = new SelectedItem(go, td);
                    CheckpointData cd;
                    if (IsMappedCheckpoint(go, out cd))
                        _selectedItem = new SelectedItem(go, cd);
                    WarpData wd;
                    if (IsMappedWarp(go, out wd))
                        _selectedItem = new SelectedItem(go, wd);
                    VictoryData vd;
                    if (IsMappedVictory(go, out vd))
                        _selectedItem = new SelectedItem(go, vd);
                }

                // +++ double click handler +++
                if (isDoubleClick)
                {
                    // Debug.Log("++++ DOUBLE CLICK HANDLER+++++");

                    go = c2d.gameObject;
                    if (IsMappedTile(go, out td))
                    {
                        removeTile(go);
                        Destroy(go);
                    }
                    else
                    {
                        removeSpecial(go);
                        Destroy(go);
                    }

                    // Kick it over to create mode
                    EnterCreate();
                }
            }

            // Delete key will destroy instance and forget _selectedItem
            if (CheckInputDown(InputKeys.Delete))
            {
                soundManager.Play("delete");

                _currentCreatorToolGameObject.SetActive(false);
                Destroy(_selectedItem.instance);
                _selectedItem = SelectedItem.noSelection;
            }
        }
        // single click handler (no previously selected item)
        if (CheckInputDown(InputKeys.ClickMain) && noItemCurrentlySelected && !isDoubleClick)
        {
            // Debug.Log(
            //     "[updateEdit] SINGLE CLICK noItemCurrentlySelected: " + noItemCurrentlySelected
            // );
            Collider2D c2d = GetObjectClicked();
            if (!c2d)
            {
                // left-click selects a item, if click misses then break
                _selectedItem = SelectedItem.noSelection;
                return;
            }

            go = c2d.gameObject;
            // check if clicked object is a mapped tile
            if (IsMappedTile(go, out td))
            {
                // if clicked tile isn't a part of activeLayer, ignore it
                if (td.orient.layer != activeLayer)
                    return;
                _selectedItem = new SelectedItem(go, td);
            }
            else
            {
                // if special is clicked, same as above with extra checks
                CheckpointData cd;
                WarpData wd;
                VictoryData vd;
                if (IsMappedCheckpoint(go, out cd))
                {
                    _selectedItem = new SelectedItem(go, cd);
                }
                if (IsMappedWarp(go, out wd))
                {
                    _selectedItem = new SelectedItem(go, wd);
                }
                if (IsMappedVictory(go, out vd))
                {
                    _selectedItem = new SelectedItem(go, vd);
                }
            }
        }
    }

    // make changes associated with being in paintMode
    private void updatePaint()
    {
        // stub
    }

    // makes changes associated with being in selectMode
    private void updateSelect()
    {
        // in select mode, clicking is the only function
        if (CheckInputDown(InputKeys.ClickMain))
        {
            // first find out what (if anything) was clicked on
            Collider2D c2d = GetObjectClicked();
            GameObject si = _selectedItem.instance;

            // Debug.Log("[updates] [updateSelect] c2d.gameObject.name: " + c2d.gameObject.name);

            if (!c2d || (si && (si == c2d.gameObject)))
            {
                // if nothing or selected tile is clicked on, deselect and return
                _selectedItem = SelectedItem.noSelection;
                return;
            }
            else
            {
                // otherwise select according to what was clicked on
                GameObject go = c2d.gameObject;
                TileData td;
                if (IsMappedTile(go, out td))
                    _selectedItem = new SelectedItem(go, td);
                CheckpointData cd;
                if (IsMappedCheckpoint(go, out cd))
                    _selectedItem = new SelectedItem(go, cd);
                WarpData wd;
                if (IsMappedWarp(go, out wd))
                    _selectedItem = new SelectedItem(go, wd);
                VictoryData vd;
                if (IsMappedVictory(go, out vd))
                    _selectedItem = new SelectedItem(go, vd);
            }
        }
    }

    // handles input that modifies the tile creator tool
    private void updateTileProperties()
    {
        // update tile rotation
        int rot = tileCreator.tileOrient.rotation;
        int oldRot = rot;
        if (CheckInputDown(InputKeys.CCW))
            rot++;
        if (CheckInputDown(InputKeys.CW))
            rot--;
        if (rot != oldRot)
        {
            // play a sound for rotation
            soundManager.Play("bounce");

            tileCreator.SetRotation(rot);
        }

        // update tile color
        if (CheckInputDown(InputKeys.ColorCCW))
        {
            // play a sound for color Change
            soundManager.Play("bounce");

            tileCreator.CycleColor(false);
        }
        if (CheckInputDown(InputKeys.ColorCW))
        {
            // play a sound for color Change
            soundManager.Play("bounce");

            tileCreator.CycleColor(true);
        }

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

        if (
            CheckInputDown(InputKeys.One)
            || CheckInputDown(InputKeys.Two)
            || CheckInputDown(InputKeys.Three)
            || CheckInputDown(InputKeys.Four)
            || CheckInputDown(InputKeys.Five)
            || CheckInputDown(InputKeys.Six)
        )
        {
            // Play the sound for any tile type selected
            soundManager.Play("bounce");
        }
    }

    // handles input relating to the current tool
    private void UpdateTool()
    {
        bool isMainClick = CheckInputDown(InputKeys.ClickMain);
        switch (_currentCreatorTool)
        {
            // when using tile tool, always update tile creator properties first
            case EditCreatorTool.Tile:
                updateTileProperties();
                // if main click, add relevant tool's item to the level
                if (isMainClick)
                {
                    // play random drawing sound
                    int variant = UnityEngine.Random.Range(1, 10);
                    soundManager.Play($"drawing-{variant}");

                    GameObject go = addTile();
                    _selectedItem = new SelectedItem(go, _tileLookup[go]);
                }
                break;
            case EditCreatorTool.Checkpoint:
                // if main click, add relevant tool's item to the level
                if (isMainClick)
                {
                    // play warp sound
                    soundManager.Play("checkpoint");
                    CheckpointData cd = new CheckpointData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(cd);
                    _selectedItem = new SelectedItem(go, cd);
                }
                break;
            case EditCreatorTool.Warp:
                // if main click, add relevant tool's item to the level
                if (isMainClick)
                {
                    // play warp sound
                    soundManager.Play("warp");

                    WarpData wd = new WarpData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(wd);
                    _selectedItem = new SelectedItem(go, wd);
                }
                break;
            case EditCreatorTool.Victory:
                // if main click, add relevant tool's item to the level
                if (isMainClick)
                {
                    // play victory sound
                    soundManager.Play("victory");
                    VictoryData victoryData = new VictoryData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(victoryData);
                    _selectedItem = new SelectedItem(go, victoryData);
                }
                break;
            default:
                break;
        }
    }
}
