using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using circleXsquares;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class EditGM
{
    /* Public Structs */

    // a struct that manages "selection" (what is active/inactive),
    // particularly when switching modes and/or tools
    public struct SelectedItem
    {
        public ChkpntData? chkpntData;
        public GameObject instance;
        public TileData? tileData;
        public WarpData? warpData;
        public VictoryData? victoryData;

        public SelectedItem(TileData inTile)
            : this(null, inTile) { }

        public SelectedItem(ChkpntData inChkpnt)
            : this(null, inChkpnt) { }

        public SelectedItem(WarpData inWarp)
            : this(null, inWarp) { }

        public SelectedItem(VictoryData inVictory)
            : this(null, inVictory) { }

        public SelectedItem(GameObject inInstance, TileData inTile)
        {
            instance = inInstance;
            tileData = inTile;
            chkpntData = null;
            warpData = null;
            victoryData = null;
        }

        public SelectedItem(GameObject inInstance, ChkpntData inChkpnt)
        {
            instance = inInstance;
            tileData = null;
            chkpntData = inChkpnt;
            warpData = null;
            victoryData = null;
        }

        public SelectedItem(GameObject inInstance, WarpData inWarp)
        {
            instance = inInstance;
            tileData = null;
            chkpntData = null;
            warpData = inWarp;
            victoryData = null;
        }

        public SelectedItem(GameObject inInstance, VictoryData inVictory)
        {
            instance = inInstance;
            tileData = null;
            chkpntData = null;
            warpData = null;
            victoryData = inVictory;
        }

        public static bool operator ==(SelectedItem si1, SelectedItem si2)
        {
            if (si1.instance != si2.instance)
                return false;
            if (si1.tileData != si2.tileData)
                return false;
            if (si1.chkpntData != si2.chkpntData)
                return false;
            if (si1.warpData != si2.warpData)
                return false;
            if (si1.victoryData != si2.victoryData)
                return false;
            return true;
        }

        public static SelectedItem noSelection = new SelectedItem();

        public static bool operator !=(SelectedItem si1, SelectedItem si2)
        {
            return !(si1 == si2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            SelectedItem? inSI = obj as SelectedItem?;
            if (!inSI.HasValue)
                return false;
            else
                return this == inSI.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /* Public Operations */

    // adds a single layer to the bottom of the level
    public void AddLayer()
    {
        addLayers(tileMap.transform.childCount);
    }

    // switches into createMode
    public void EnterCreate()
    {
        // if already in createMode, simply escape
        if (createMode)
            return;

        if (_selectedItem != SelectedItem.noSelection)
        {
            // if exiting editMode, add _selectedItem back to the level
            if (editMode)
                addSelectedItem();

            if (_selectedItem.tileData.HasValue)
            {
                // if _selectedItem is a tile, use its tileData to set tile tool
                tileCreator.SetProperties(_selectedItem.tileData.Value);
                setTool(EditTools.Tile);
            }
            // set tool to checkpoint or warp tool as appropriate
            if (_selectedItem.chkpntData.HasValue)
                setTool(EditTools.Checkpoint);
            if (_selectedItem.warpData.HasValue)
                setTool(EditTools.Warp);
            if (_selectedItem.victoryData.HasValue)
                setTool(EditTools.Victory);

            // null out SelectedItem's instance to instead refer to creation tool
            _selectedItem.instance = null;
        }
        else
        {
            // if no _selectedItem, default to tile tool
            TileData td = tileCreator.GetTileData();
            _selectedItem = new SelectedItem(td);
            setTool(EditTools.Tile);
        }

        _currentMode = EditorMode.Create;
    }

    // switches into editMode
    public void EnterEdit()
    {
        // if already in editMode, simply escape
        if (editMode)
            return;

        if (_selectedItem != SelectedItem.noSelection)
        {
            if (_selectedItem.instance)
            {
                // if an object is selected, destroy it and activate relevant tool
                removeSelectedItem();
                Destroy(_selectedItem.instance);
                _selectedItem.instance = null;
            }
            else
            {
                // otherwise, simply unselect _selectedItem
                _selectedItem = SelectedItem.noSelection;
            }
            if (_selectedItem.chkpntData.HasValue)
                setTool(EditTools.Checkpoint);
            if (_selectedItem.warpData.HasValue)
                setTool(EditTools.Warp);
            if (_selectedItem.victoryData.HasValue)
                setTool(EditTools.Victory);

            // regardless of item selected, unselect it
            removeSelectedItem();
            Destroy(_selectedItem.instance);
            _selectedItem.instance = null;
        }
        else
        {
            // if no _selectedItem, default to tile tool
            setTool(EditTools.Tile);
        }

        _currentMode = EditorMode.Edit;
    }

    // switches into paintMode
    public void EnterPaint()
    {
        // if already in paintMode, simply escape
        if (paintMode)
            return;

        if (_selectedItem != SelectedItem.noSelection)
        {
            if (_selectedItem.tileData.HasValue)
                // if _selectedItem is a tile, use its tileData to set tile tool
                tileCreator.SetProperties(_selectedItem.tileData.Value);
            if (editMode)
                // if in editMode, add _selectedItem back to the level
                addSelectedItem();
            else
                // if not in editMode, unselect _selectedItem
                _selectedItem = SelectedItem.noSelection;
        }

        // always enter paintMode with tile tool enabled
        setTool(EditTools.Tile);
        _currentMode = EditorMode.Paint;
    }

    // switches into selectMode
    public void EnterSelect()
    {
        // if already in selectMode, simply escape
        if (selectMode)
            return;

        if (editMode && _selectedItem != SelectedItem.noSelection)
            // if in editMode while an object is selected, place the object
            addSelectedItem();
        if (!_selectedItem.instance)
            // if no object is selected, unselect _selectedItem
            _selectedItem = SelectedItem.noSelection;

        // _currentTool should always be disabled in selectMode
        _currentTool.SetActive(false);
        _currentMode = EditorMode.Select;
    }

    // moves focus down to the next layer
    public void MoveDownLayer()
    {
        activateLayer(activeLayer + 1);
    }

    // moves focus up to the previous layer
    public void MoveUpLayer()
    {
        activateLayer(activeLayer - 1);
    }

    // removes a single layer from the bottom of the level
    public void RemoveLayer()
    {
        if (!removeLayer(tileMap.transform.childCount - 1, false))
        {
            // popup confirmation dialog
            removeLayer(tileMap.transform.childCount - 1, true);
        }
    }

    // (!!)(incomplete) deletes the current scene and loads the MainMenu scene
    public void ReturnToMainMenu()
    {
        // (!!) should prompt if unsaved
        SceneManager.LoadScene(0);
    }

    // In case we need it again...
    string SanitizeFilename(string input)
    {
        return new string(
            input.Where(c => !char.IsControl(c) && c != '\u200B' && c != '\uFEFF').ToArray()
        ).Trim();
    }

    // Save to disk in json
    public void SaveFile(string levelName)
    {
        string[] lines = levelData.Serialize();
        string levelsFolder = LevelStorage.LevelsFolder;

        if (!Directory.Exists(levelsFolder))
        {
            Directory.CreateDirectory(levelsFolder);
        }

        SupabaseLevelDTO level = new SupabaseLevelDTO { name = levelName, data = lines };
        string json = JsonUtility.ToJson(level, true); // true = pretty print

        string path = Path.Combine(levelsFolder, $"{levelName}.json");
        path = path.Replace("\\", "/");

        Debug.Log($"[SAVE] Saving to: {path}");
        File.WriteAllText(path, json);
    }

    public void TestLevel()
    {
        // first save a copy to disk
        SaveFile(levelName);

        // start the play scene and set the levelName to current levelName
        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();

        // overwrite the levelname with most recent
        levelInfo.name = levelName;
        // even if this level was loaded from supabase, its all local from here baby
        levelInfo.isLocal = true;
        // set the level info in the loader (this is the passoff)
        loader.levelInfo = levelInfo;

        // let the play loader know it's coming from edit mode
        loader.playModeContext = PlayGM.PlayModeContext.FromEditor;

        // fire the scene off
        SceneManager.LoadScene("Playing");
    }

    // sets level name property with passed string
    public void SetLevelName(string inName)
    {
        // level names are capped at 100 characters for now
        if (inName.Length <= 100)
        {
            _levelName = inName;
            levelInfo.name = inName;
        }
    }

    /* Private Operations */

    // returns a list of all HUD elements currently under the mouse
    private List<RaycastResult> raycastAllHUD()
    {
        PointerEventData ped = new PointerEventData(eventSystem);
        ped.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        return results;
    }
}
