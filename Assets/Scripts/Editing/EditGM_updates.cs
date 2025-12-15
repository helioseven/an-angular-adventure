using circleXsquares;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class EditGM
{
    // Makes changes associated with anchorIcon and layer changes
    private void updateLevel()
    {
        var edit = InputManager.Instance.Controls.Edit;

        // --- Right-click or alternate click updates snap cursor location ---
        if (edit.ClickAlt.WasPressedThisFrame())
            anchorIcon.FindNewAnchor();

        // --- Layer change inputs ---
        if (edit.LayerUp.WasPressedThisFrame())
            MoveUpLayer();

        if (edit.LayerDown.WasPressedThisFrame())
            MoveDownLayer();
    }

    // updates UI Overlay and Palette panels
    private void updateUI()
    {
        if (_inputMode)
            return;

        var edit = InputManager.Instance.Controls.Edit;

        // --- HUD toggle (was spacebar before) ---
        if (edit.Palette.WasPressedThisFrame())
            hudPanel.SetActive(!hudPanel.activeSelf);

        // --- HUD hover logic ---
        hoveringHUD = hudPanel.activeSelf ? checkHUDHover() : false;

        // --- Show/hide current creator tool based on palette/HUD state ---
        if (hoveringHUD || paletteMode)
        {
            // whenever palette activates, _currentCreatorToolGameObject is turned off
            _currentCreatorToolGameObject.SetActive(false);
        }
        else
        {
            // when palette deactivates, determine desired _currentCreatorToolGameObject activity
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
        var edit = InputManager.Instance.Controls.Edit;

        // --- C, V, and B activate checkpoint, victory, and warp tools ---
        if (edit.CheckpointTool.WasPressedThisFrame())
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Checkpoint);
            soundManager.Play("checkpoint");
        }

        if (edit.VictoryTool.WasPressedThisFrame())
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Victory);
            soundManager.Play("victory");
        }

        if (edit.WarpTool.WasPressedThisFrame())
        {
            _currentCreatorToolGameObject.SetActive(false);
            setTool(EditCreatorTool.Warp);
            soundManager.Play("warp");
        }

        // --- Number keys 1–6 activate Tile tool and update tile properties ---
        if (
            edit.Triangle.WasPressedThisFrame()
            || edit.Diamond.WasPressedThisFrame()
            || edit.Trapezoid.WasPressedThisFrame()
            || edit.Hexagon.WasPressedThisFrame()
            || edit.Square.WasPressedThisFrame()
            || edit.Wedge.WasPressedThisFrame()
        )
        {
            _currentCreatorToolGameObject.SetActive(false);
            updateTileProperties();
            setTool(EditCreatorTool.Tile);
        }

        // whichever tool is being used should always be active
        _currentCreatorToolGameObject.SetActive(true);
    }

    // Makes changes associated with being in isEditorInEditMode
    private void updateEdit()
    {
        if (ShouldBlockWorldClick())
            return; // skip world interaction this frame

        var edit = InputManager.Instance.Controls.Edit;

        bool noItemCurrentlySelected =
            !_selectedItem.instance || _selectedItem == SelectedItem.noSelection;
        bool isDoubleClick = false;
        GameObject go;
        TileData td;

        // --- Primary click  ---
        bool clickMain = edit.Click.WasPressedThisFrame();
        // --- Delete key ---
        bool deletePressed = edit.Delete.WasPressedThisFrame();

        // first, handle the case where an item is currently selected
        if (!noItemCurrentlySelected)
        {
            if (clickMain)
            {
                Collider2D c2d = GetObjectClicked();

                if (!c2d)
                {
                    _selectedItem = SelectedItem.noSelection;
                    return;
                }
                else if (_selectedItem.instance == c2d.gameObject)
                {
                    isDoubleClick = true;
                }
                else if (!isDoubleClick)
                {
                    go = c2d.gameObject;
                    if (IsMappedTile(go, out td))
                        _selectedItem = new SelectedItem(go, td);

                    if (IsMappedCheckpoint(go, out CheckpointData cd))
                        _selectedItem = new SelectedItem(go, cd);

                    if (IsMappedWarp(go, out WarpData wd))
                        _selectedItem = new SelectedItem(go, wd);

                    if (IsMappedVictory(go, out VictoryData vd))
                        _selectedItem = new SelectedItem(go, vd);
                }

                if (isDoubleClick)
                {
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

                    // kick it over to create mode
                    EnterCreate();
                }
            }

            // Delete key destroys instance and clears selection
            if (deletePressed)
            {
                soundManager.Play("delete");
                removeSelectedItem();
                Destroy(_selectedItem.instance);
                _selectedItem = SelectedItem.noSelection;
            }
        }

        // single click handler (no previously selected item)
        if (clickMain && noItemCurrentlySelected && !isDoubleClick)
        {
            Collider2D c2d = GetObjectClicked();
            if (!c2d)
            {
                _selectedItem = SelectedItem.noSelection;
                return;
            }

            go = c2d.gameObject;
            if (IsMappedTile(go, out td))
            {
                if (td.orient.layer != activeLayer)
                    return;
                _selectedItem = new SelectedItem(go, td);
            }
            else
            {
                if (IsMappedCheckpoint(go, out CheckpointData cd))
                    _selectedItem = new SelectedItem(go, cd);
                if (IsMappedWarp(go, out WarpData wd))
                    _selectedItem = new SelectedItem(go, wd);
                if (IsMappedVictory(go, out VictoryData vd))
                    _selectedItem = new SelectedItem(go, vd);
            }
        }
    }

    // make changes associated with being in paintMode
    private void updatePaint()
    {
        // stub
    }

    // Makes changes associated with being in selectMode
    private void updateSelect()
    {
        if (ShouldBlockWorldClick())
            return; // skip world interaction this frame

        var edit = InputManager.Instance.Controls.Edit;

        // In select mode, clicking is the only function
        if (edit.Click.WasPressedThisFrame())
        {
            // First find out what (if anything) was clicked on
            Collider2D c2d = GetObjectClicked();
            GameObject si = _selectedItem.instance;

            if (!c2d || (si && si == c2d.gameObject))
            {
                // If nothing or the same tile is clicked, deselect and return
                _selectedItem = SelectedItem.noSelection;
                return;
            }

            // Otherwise select according to what was clicked on
            GameObject go = c2d.gameObject;
            if (IsMappedTile(go, out TileData td))
                _selectedItem = new SelectedItem(go, td);
            if (IsMappedCheckpoint(go, out CheckpointData cd))
                _selectedItem = new SelectedItem(go, cd);
            if (IsMappedWarp(go, out WarpData wd))
                _selectedItem = new SelectedItem(go, wd);
            if (IsMappedVictory(go, out VictoryData vd))
                _selectedItem = new SelectedItem(go, vd);
        }
    }

    // Handles input that modifies the tile creator tool
    private void updateTileProperties()
    {
        var edit = InputManager.Instance.Controls.Edit;

        // --- Update tile rotation ---
        int rot = tileCreator.tileOrient.rotation;
        int oldRot = rot;

        if (edit.RotateLeft.WasPressedThisFrame())
            rot++;
        if (edit.RotateRight.WasPressedThisFrame())
            rot--;

        if (rot != oldRot)
        {
            soundManager.Play("bounce");
            tileCreator.SetRotation(rot);
        }

        // --- Update tile color ---
        if (edit.CycleColorPrev.WasPressedThisFrame())
        {
            soundManager.Play("bounce");
            tileCreator.CycleColor(false);
        }

        if (edit.CycleColorNext.WasPressedThisFrame())
        {
            soundManager.Play("bounce");
            tileCreator.CycleColor(true);
        }

        // --- Update tile type (1–6) ---
        if (edit.Triangle.WasPressedThisFrame())
            tileCreator.SelectType(0);
        if (edit.Diamond.WasPressedThisFrame())
            tileCreator.SelectType(1);
        if (edit.Trapezoid.WasPressedThisFrame())
            tileCreator.SelectType(2);
        if (edit.Hexagon.WasPressedThisFrame())
            tileCreator.SelectType(3);
        if (edit.Square.WasPressedThisFrame())
            tileCreator.SelectType(4);
        if (edit.Wedge.WasPressedThisFrame())
            tileCreator.SelectType(5);

        if (
            edit.Triangle.WasPressedThisFrame()
            || edit.Diamond.WasPressedThisFrame()
            || edit.Trapezoid.WasPressedThisFrame()
            || edit.Hexagon.WasPressedThisFrame()
            || edit.Square.WasPressedThisFrame()
            || edit.Wedge.WasPressedThisFrame()
        )
        {
            soundManager.Play("bounce");
        }
    }

    // Handles input relating to the current tool
    private void UpdateTool()
    {
        if (ShouldBlockWorldClick())
            return; // skip world interaction this frame

        var edit = InputManager.Instance.Controls.Edit;
        bool isMainClick = edit.Click.WasPressedThisFrame();

        switch (_currentCreatorTool)
        {
            // --- Tile Tool ---
            case EditCreatorTool.Tile:
                updateTileProperties();
                if (isMainClick)
                {
                    int variant = UnityEngine.Random.Range(3, 10);
                    soundManager.Play($"drawing-{variant}");

                    GameObject go = addTile();
                    _selectedItem = new SelectedItem(go, _tileLookup[go]);
                }
                break;

            // --- Checkpoint Tool ---
            case EditCreatorTool.Checkpoint:
                if (isMainClick)
                {
                    soundManager.Play("checkpoint");
                    CheckpointData cd = new CheckpointData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(cd);
                    _selectedItem = new SelectedItem(go, cd);
                }
                break;

            // --- Warp Tool ---
            case EditCreatorTool.Warp:
                if (isMainClick)
                {
                    soundManager.Play("warp");
                    WarpData wd = new WarpData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(wd);
                    _selectedItem = new SelectedItem(go, wd);
                }
                break;

            // --- Victory Tool ---
            case EditCreatorTool.Victory:
                if (isMainClick)
                {
                    soundManager.Play("victory");
                    VictoryData vd = new VictoryData(anchorIcon.focus, activeLayer);
                    GameObject go = addSpecial(vd);
                    _selectedItem = new SelectedItem(go, vd);
                }
                break;

            default:
                break;
        }
    }
}
