using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM {

    /* Public Enums */

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
        if (b)
            // if invalid layer index is given, fail quietly
            return;
        else
            // otherwise update activeLayer and continue
            activeLayer = inLayer;

        // ordinal distance from activeLayer is calculated, opacity set accordingly
        foreach (Transform layer in tileMap.transform) {
            int layerNumber = layer.GetSiblingIndex();
            int distance = Math.Abs(layerNumber - activeLayer);
            // dim layers in front of active layer by an extra amount
            if (activeLayer > layerNumber)
                distance += 2;
            setLayerOpacity(layer, distance);
        }

        // update opacity for all checkpoints
        foreach (Transform checkpoint in chkpntMap.transform) {
            ChkpntData cd;
            bool ok = IsMappedChkpnt(checkpoint.gameObject, out cd);
            int layerNumber = INACTIVE_LAYER;
            if (ok)
                layerNumber = cd.layer;
            int distance = Math.Abs(layerNumber - activeLayer);
            if (activeLayer > layerNumber)
                distance += 2;
            setCheckpointOpacity(checkpoint, distance);
        }

        // add active layer depth and move the snap cursor to the new location
        Vector3 v3 = anchorIcon.transform.position;
        v3.z = GetLayerDepth();
        anchorIcon.transform.position = v3;
    }

    // simply adds layers to the level until there are enough layers to account for the given layer
    private void addLayers(int inLayer)
    {
        // if there are already more layers than the passed index, simply return
        if (inLayer < tileMap.transform.childCount)
            return;
        // otherwise, create layers until the passed index is reached
        for (int i = tileMap.transform.childCount; i <= inLayer; i++) {
            GameObject tileLayer = new GameObject("Layer #" + i.ToString());
            tileLayer.transform.position = new Vector3(0f, 0f, i * 2f);
            tileLayer.transform.SetParent(tileMap.transform);
        }
    }

    // used when leaving editMode, places _selectedItem where it indicates it belongs
    private void addSelectedItem ()
    {
        // if nothing is selected, escape
        if (_selectedItem == new SelectedItem())
            return;
        // for each item type, use item data to restore item
        if (_selectedItem.tileData.HasValue) {
            TileData td = _selectedItem.tileData.Value;
            _selectedItem.instance = addTile(td);
        } else if (_selectedItem.chkpntData.HasValue) {
            ChkpntData cd = _selectedItem.chkpntData.Value;
            _selectedItem.instance = addSpecial(cd);
        } else if (_selectedItem.warpData.HasValue) {
            WarpData wd = _selectedItem.warpData.Value;
            _selectedItem.instance = addSpecial(wd);
        }
    }

    // adds a passed ChkpntData to the level and returns a reference
    private GameObject addSpecial (ChkpntData inChkpnt)
    {
        // first, the given ChkpntData is added to levelData
        levelData.chkpntSet.Add(inChkpnt);

        // corresponding checkpoint object is added to chkpntMap
        Vector3 v3 = inChkpnt.locus.ToUnitySpace();
        v3.z = GetLayerDepth(inChkpnt.layer);
        GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
        go.GetComponent<SpecialCreator>().enabled = false;
        go.transform.SetParent(chkpntMap.transform);

        // resulting gameObject is added to lookup dictionary and returned
        _chkpntLookup[go] = inChkpnt;
        return go;
    }

    // adds a passed WarpData to the level and returns a reference
    private GameObject addSpecial (WarpData inWarp)
    {
        // first, the given ChkpntData is added to levelData
        levelData.warpSet.Add(inWarp);

        // corresponding checkpoint object is added to chkpntMap
        Vector3 v3 = inWarp.orient.locus.ToUnitySpace();
        v3.z = GetLayerDepth(inWarp.orient.layer);
        GameObject go = Instantiate(warpTool, v3, Quaternion.identity) as GameObject;
        go.GetComponent<SpecialCreator>().enabled = false;
        go.transform.SetParent(warpMap.transform); // <2>

        // resulting gameObject is added to lookup dictionary and returned
        _warpLookup[go] = inWarp; // <3>
        return go;
    }

    // adds a default tile to the level and returns a reference
    private GameObject addTile ()
    {
        // uses tileCreator state for parameterless tile addition
        TileData td = tileCreator.GetTileData();
        return addTile(td);
    }

    // adds a passed tileData to the level and returns a reference
    private GameObject addTile (TileData inTile)
    {
        // first, the given TileData is added to levelData
        levelData.tileSet.Add(inTile);

        // then new tile object is created and added to tileMap
        GameObject go = tileCreator.NewTile(inTile);
        Transform tl = tileMap.transform.GetChild(inTile.orient.layer);
        go.transform.SetParent(tl);

        // add tile's gameObject to the tile lookup and return it
        _tileLookup[go] = inTile;
        return go;
    }

    // instantiates GameObjects and builds lookup dictionaries based on the given LevelData
    private void buildLevel (LevelData inLevel)
    {
        // first, prefab references are arrayed for indexed access
        GameObject[,] prefab_refs = new GameObject[6, 8];
        foreach (Transform tileGroup in tileCreator.transform)
            foreach (Transform tile in tileGroup) {
                int tgi = tileGroup.GetSiblingIndex();
                int ti = tile.GetSiblingIndex();
                prefab_refs[tgi, ti] = tile.gameObject;
            }

        // build each tile in the level
        foreach (TileData td in inLevel.tileSet) {
            // make sure there are enough layers for the new tile
            addLayers(td.orient.layer);
            Transform tileLayer = tileMap.transform.GetChild(td.orient.layer);
            GameObject pfRef = prefab_refs[td.type, td.color];
            Quaternion q;
            Vector3 v3 = td.orient.ToUnitySpace(out q);
            GameObject go = Instantiate(pfRef, v3, q) as GameObject;
            go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
            go.transform.SetParent(tileLayer);
            // once tile is built, add (GameObject,TileData) pair to _tileLookup
            _tileLookup.Add(go, td);
        }

        // build each checkpoint in the level
        foreach (ChkpntData cd in inLevel.chkpntSet) {
            // make sure there are enough layers for the new checkpoint
            addLayers(cd.layer);
            Vector3 v3 = cd.locus.ToUnitySpace();
            // checkpoints' z positions are assigned by corresponding tileMap layer
            v3.z = GetLayerDepth(cd.layer);
            GameObject go = Instantiate(chkpntTool, v3, Quaternion.identity) as GameObject;
            go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
            go.transform.SetParent(chkpntMap.transform);
            go.SetActive(true);
            go.GetComponent<SpecialCreator>().enabled = false;
            // add the GameObject,ChkpntData pair to _chkpntLookup
            _chkpntLookup.Add(go, cd);
        }

        // build each warp in the level
        foreach (WarpData wd in inLevel.warpSet) {
            // make sure there are enough layers for the new warp
            addLayers(wd.orient.layer);
            Quaternion q;
            Vector3 v3 = wd.orient.ToUnitySpace(out q);
            GameObject go = Instantiate(warpTool, v3, q) as GameObject;
            go.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
            go.transform.SetParent(warpMap.transform);
            go.SetActive(true);
            go.GetComponent<SpecialCreator>().enabled = false;
            // add the GameObject,WarpData pair to _warpLookup
            _warpLookup.Add(go, wd);
        }
    }

    // used when entering editMode with an item selected, which removes it
    private void removeSelectedItem ()
    {
        if (_selectedItem.tileData.HasValue) {
            removeTile(_selectedItem.instance);
            // if _selectedItem is a tile, use tileData to set tileCreator
            tileCreator.SetProperties(_selectedItem.tileData.Value);
            // remove _selectedItem from level and set tile
            setTool(EditTools.Tile);
        } else if (_selectedItem.chkpntData.HasValue) {
            removeSpecial(_selectedItem.instance);
            // remove _selectedItem from level and set chkpnt
            setTool(EditTools.Chkpnt);
        } else if (_selectedItem.warpData.HasValue) {
            removeSpecial(_selectedItem.instance);
            // remove _selectedItem from level and set warp
            setTool(EditTools.Warp);
        }
    }

    // removes a given special from the level
    private void removeSpecial (GameObject inSpecial)
    {
        // check to see whether the given item is a checkpoint
        ChkpntData cData;
        WarpData wData;
        if (IsMappedChkpnt(inSpecial, out cData)) {
            _selectedItem = new SelectedItem(inSpecial, cData);
            setTool(EditTools.Chkpnt);

            //set _selectedItem and tool then remove item from level and lookup
            levelData.chkpntSet.Remove(cData);
            _chkpntLookup.Remove(inSpecial);
        // check to see whether the given item is a warp
        } else if (IsMappedWarp(inSpecial, out wData)) {
            _selectedItem = new SelectedItem(inSpecial, wData);
            setTool(EditTools.Warp);

            // set _selectedItem and tool then remove item from level and lookup
            levelData.warpSet.Remove(wData);
            _warpLookup.Remove(inSpecial);
        // if neither, simply return
        } else
            return;

        // if either, destroy the passed object
        Destroy(inSpecial);
    }

    // removes a given tile from the level
    private void removeTile (GameObject inTile)
    {
        // lookup the item's TileData
        TileData tData;
        bool b = IsMappedTile(inTile, out tData);
        // if the passed GameObject is not part of tileMap, we escape
        if (!b)
            return;

        // otherwise remove tile from the level and _tileLookup
        levelData.tileSet.Remove(tData);
        _tileLookup.Remove(inTile);
    }

    // sets the opacity of all tiles within a layer using ordinal distance from activeLayer
    private void setLayerOpacity (Transform tileLayer, int distance)
    {
        // opacity and layer are calculated for non-active layers
        float alpha = 1f;
        int layer = DEFAULT_LAYER;
        if (distance != 0) {
            // alpha is calculated as (1/2)^distance from active layer
            alpha = (float)Math.Pow(0.5, (double)distance);
            layer = INACTIVE_LAYER;
        }
        Color color = new Color(1f, 1f, 1f, alpha);

        // each tile's sprite is colored appropriately
        foreach (Transform tile in tileLayer) {
            tile.gameObject.layer = layer;
            tile.GetChild(0).GetComponent<SpriteRenderer>().color = color;
        }
    }

    // set opacity and physics by given distance for given checkpoint
    private void setCheckpointOpacity (Transform checkpoint, int distance)
    {
        // opacity and layer are calculated for non-active layers
        float alpha = 1f;
        int layer = DEFAULT_LAYER;
        if (distance != 0) {
            // alpha is calculated as (1/2)^distance from active layer
            alpha = (float)Math.Pow(0.5, (double)distance);
            layer = INACTIVE_LAYER;
        }
        Color color = new Color(1f, 1f, 1f, alpha);

        // each chkpnt's sprite is colored appropriately
        checkpoint.gameObject.layer = layer;
        checkpoint.GetChild(0).GetComponent<SpriteRenderer>().color = color;
    }

    // sets the currently active tool
    private void setTool (EditTools inTool)
    {
        switch (inTool) {
            case EditTools.Tile:
                _currentTool = tileCreator.gameObject;
                break;
            case EditTools.Chkpnt:
                _currentTool = chkpntTool;
                break;
            case EditTools.Warp:
                _currentTool = warpTool;
                break;
            case EditTools.Eraser:
                // missing implementation
                _currentTool = null;
                break;
            default:
                break;
        }

        _toolMode = inTool;
    }

    /* Public Utilities */

    // simply returns whether the given keys were being held during this frame
    public bool CheckInput (InputKeys inKeys)
    {
        return (getInputs & inKeys) == inKeys;
    }

    // simply returns whether the given keys were pressed on this frame
    public bool CheckInputDown (InputKeys inKeys)
    {
        return (getInputDowns & inKeys) == inKeys;
    }

    // simply returns the z value of the current layer's transform
    public float GetLayerDepth ()
    {
        return GetLayerDepth(activeLayer);
    }

    // simply returns the z value of the given layer's transform
    public float GetLayerDepth (int inLayer)
    {
        return tileMap.transform.GetChild(inLayer).position.z;
    }

    // returns first collider hit on active layer under click
    public Collider2D GetObjectClicked ()
    {
        // use plane at active layer depth to calculate mouse intersection
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.forward, -2f * activeLayer);
        // get point of intersection with active layer plane
        float f;
        p.Raycast(r, out f);
        // cast an orthogonal ray through layer at intersection point
        Vector3 v3 = r.GetPoint(f);
        v3.z -= 1f;
        r = new Ray(v3, Vector3.forward);
        return Physics2D.GetRayIntersection(r, 2f).collider;
    }

    // if passed object is a tile, supplies corresponding TileData
    public bool IsMappedTile (GameObject inTile, out TileData outData)
    {
        outData = new TileData();
        // if tile isn't mapped, output default values and return false
        if (!inTile || !_tileLookup.ContainsKey(inTile)) {
            return false;
        // if it is, output the TileData and return true
        } else {
            outData = _tileLookup[inTile];
            return true;
        }
    }

    // if passed object is a checkpoint, supplies corresponding ChkpntData
    public bool IsMappedChkpnt (GameObject inChkpnt, out ChkpntData outData)
    {
        outData = new ChkpntData();
        if (!inChkpnt || !_chkpntLookup.ContainsKey(inChkpnt)) {
            return false;
        // if it is, output the ChkpntData and return true
        } else {
            outData = _chkpntLookup[inChkpnt];
            return true;
        }
    }

    // if passed object is a checkpoint, supplies corresponding WarpData
    public bool IsMappedWarp (GameObject inWarp, out WarpData outData)
    {
        outData = new WarpData();
        // if warp isn't mapped, output default values and return false
        if (!inWarp || !_warpLookup.ContainsKey(inWarp)) {
            return false;
        // if it is, output the WarpData and return true
        } else {
            outData = _warpLookup[inWarp];
            return true;
        }
    }
}
