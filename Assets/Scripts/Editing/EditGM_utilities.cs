using System;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class EditGM
{
    /* Public Enums */

    // EditorMode establishes the different modes the editor can be in
    public enum EditorMode
    {
        Select,
        Edit,
        Create,
        Paint,
    }

    // EditCreatorTool establishes the different tools usable in the editor
    public enum EditCreatorTool
    {
        Tile,
        Checkpoint,
        Warp,
        Victory,
        Eraser,
    }

    /* Private Utilities */

    private void NotifyKeyDoorMappingChanged()
    {
        KeyDoorMappingChanged?.Invoke();
    }

    private void NotifyKeyDoorVisibilityChanged()
    {
        KeyDoorVisibilityChanged?.Invoke(_keyDoorLinksVisible);
    }

    private void HandleKeyDoorLinkHotkey()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            _keyDoorLinksVisible = !_keyDoorLinksVisible;
            NotifyKeyDoorVisibilityChanged();
        }
    }

    // cycles through all layers, calculates distance, and sets opacity accordingly
    private void activateLayer(int inLayer)
    {
        bool b = (inLayer < 0) || (inLayer >= Constants.DEFAULT_NUM_LAYERS);
        if (b)
            // if invalid layer index is given, fail quietly
            return;
        else
            // otherwise update activeLayer and continue
            activeLayer = inLayer;

        // ordinal distance from activeLayer is calculated, opacity set accordingly
        foreach (Transform layer in tileMap.transform)
        {
            int layerNumber = layer.GetSiblingIndex();
            int distance = Math.Abs(layerNumber - activeLayer);
            // dim layers in front of active layer by an extra amount
            if (activeLayer > layerNumber)
                distance += 2;
            setTileLayerOpacity(layer, distance);
        }

        // update opacity for all checkpoints
        foreach (Transform checkpoint in checkpointMap.transform)
        {
            CheckpointData cd;
            int layerNumber = INACTIVE_LAYER;
            if (IsMappedCheckpoint(checkpoint.gameObject, out cd))
                layerNumber = cd.layer;
            else
                continue;
            int distance = Math.Abs(layerNumber - activeLayer);
            // dim checkpoints in front of active layer by an extra amount
            if (activeLayer > layerNumber)
                distance += 2;
            setSpecialOpacity(checkpoint, distance);
        }

        // update opacity for all warps
        foreach (Transform warp in warpMap.transform)
        {
            WarpData wd;
            int layerNumber = INACTIVE_LAYER;
            if (IsMappedWarp(warp.gameObject, out wd))
                layerNumber = wd.layer;
            else
                continue;
            // warps do additional logic to figure out which
            // end of the warp is closer to the active layer
            int d1 = Math.Abs(layerNumber - activeLayer);
            int d2 = Math.Abs(wd.targetLayer - activeLayer);
            int distance = d1 <= d2 ? d1 : d2;
            // dim warps fully in front of active layer by an extra amount
            if ((activeLayer > layerNumber) && (activeLayer > wd.targetLayer))
                distance += 2;
            setSpecialOpacity(warp, distance);
        }

        // update opacity for all victories
        foreach (Transform victory in victoryMap.transform)
        {
            VictoryData vd;
            int layerNumber = INACTIVE_LAYER;
            if (IsMappedVictory(victory.gameObject, out vd))
                layerNumber = vd.layer;
            else
                continue;
            int distance = Math.Abs(layerNumber - activeLayer);
            // dim victories in front of active layer by an extra amount
            if (activeLayer > layerNumber)
                distance += 2;
            setSpecialOpacity(victory, distance);
        }

        // add active layer depth and move the snap cursor to the new location
        Vector3 v3 = anchorIcon.transform.position;
        v3.z = GetLayerDepth();
        anchorIcon.transform.position = v3;
    }

    // adds layers to the level until given total count is met
    private void addLayers(int inLayer)
    {
        // if there are already more layers than the passed index, simply return
        if (inLayer < tileMap.transform.childCount)
            return;

        // otherwise, create layers until the passed index is reached
        for (int i = tileMap.transform.childCount; i <= inLayer; i++)
        {
            GameObject tileLayer = new GameObject("Layer #" + i.ToString());
            tileLayer.transform.position = new Vector3(0f, 0f, i * 2f);
            tileLayer.transform.SetParent(tileMap.transform);
        }
    }

    // used when leaving isEditorInEditMode, places _selectedItem where it indicates it belongs
    private void addSelectedItem()
    {
        // if nothing is selected, escape
        if (_selectedItem == SelectedItem.noSelection)
            return;
        // for each item type, use item data to restore item
        if (_selectedItem.tileData.HasValue)
        {
            TileData td = _selectedItem.tileData.Value;
            _selectedItem.instance = addTile(td);
        }
        else if (_selectedItem.checkpointData.HasValue)
        {
            CheckpointData cd = _selectedItem.checkpointData.Value;
            _selectedItem.instance = addSpecial(cd);
        }
        else if (_selectedItem.victoryData.HasValue)
        {
            VictoryData victoryData = _selectedItem.victoryData.Value;
            _selectedItem.instance = addSpecial(victoryData);
        }
        else if (_selectedItem.warpData.HasValue)
        {
            WarpData wd = _selectedItem.warpData.Value;
            _selectedItem.instance = addSpecial(wd);
        }
    }

    // adds a passed CheckpointData to the level and returns a reference
    private GameObject addSpecial(CheckpointData inChkpnt)
    {
        // first, the given CheckpointData is added to levelData
        levelData.chkpntSet.Add(inChkpnt);

        // corresponding checkpoint object is added to checkpointMap
        Vector3 v3 = inChkpnt.locus.ToUnitySpace();
        v3.z = GetLayerDepth(inChkpnt.layer);
        GameObject go = Instantiate(checkpointTool, v3, Quaternion.identity) as GameObject;
        go.GetComponent<SpecialCreator>().enabled = false;
        go.transform.SetParent(checkpointMap.transform);

        // resulting gameObject is added to lookup dictionary and returned
        _checkpointLookup[go] = inChkpnt;
        return go;
    }

    // adds a passed WarpData to the level and returns a reference
    private GameObject addSpecial(WarpData inWarp)
    {
        // first, the given WarpData is added to levelData
        levelData.warpSet.Add(inWarp);

        // corresponding checkpoint object is added to chkpntMap
        Vector3 v3 = inWarp.locus.ToUnitySpace();
        v3.z = GetLayerDepth(inWarp.layer);
        GameObject go = Instantiate(warpTool, v3, Quaternion.identity) as GameObject;
        go.GetComponent<SpecialCreator>().enabled = false;
        go.transform.SetParent(warpMap.transform); // <2>

        // resulting gameObject is added to lookup dictionary and returned
        _warpLookup[go] = inWarp; // <3>
        return go;
    }

    // adds a passed VictoryData to the level and returns a reference
    private GameObject addSpecial(VictoryData inVictory)
    {
        // first, the given VictoryData is added to levelData
        levelData.victorySet.Add(inVictory);

        // corresponding victory object is added to victoryMap
        Vector3 v3 = inVictory.locus.ToUnitySpace();
        v3.z = GetLayerDepth(inVictory.layer);
        GameObject go = Instantiate(victoryTool, v3, Quaternion.identity) as GameObject;
        go.GetComponent<SpecialCreator>().enabled = false;
        go.transform.SetParent(victoryMap.transform);

        // resulting gameObject is added to lookup dictionary and returned
        _victoryLookup[go] = inVictory;
        return go;
    }

    // adds a default tile to the level and returns a reference
    private GameObject addTile()
    {
        // uses tileCreator state for parameterless tile addition
        TileData td = tileCreator.GetTileData();
        return addTile(td);
    }

    // adds a passed tileData to the level and returns a reference
    private GameObject addTile(TileData inTile)
    {
        bool mappingChanged = false;
        // first, the given TileData is added to levelData
        levelData.tileSet.Add(inTile);

        // then new tile object is created and added to tileMap
        GameObject go = tileCreator.NewTile(inTile);
        Transform tl = tileMap.transform.GetChild(inTile.orient.layer);
        go.transform.SetParent(tl);

        // add tile's gameObject to the tile lookup
        _tileLookup[go] = inTile;

        // if any tile has a valid door value
        if (inTile.doorID != 0)
        {
            // add tile to the door map
            _doorTileMap.Add(go, inTile.doorID);

            // set lock icon's rotation and then activate it
            Transform lockIcon = go.transform.GetChild(LOCK_CHILD_INDEX);
            lockIcon.rotation = Quaternion.identity;
            lockIcon.gameObject.SetActive(true);

            // trigger appropriate tile's script logic
            foreach (KeyValuePair<GameObject, int> kvp in _greenTileMap)
            {
                if (kvp.Value == inTile.doorID)
                    kvp.Key.GetComponent<TileEditGreen>().DrawLinesToAllTargets();
            }

            // flag event notification
            mappingChanged = true;
        }

        // if the tile is green and has a valid key value
        if (inTile.color == TileColor.Green && inTile.special != 0)
        {
            // add tile to the green tile map
            _greenTileMap.Add(go, inTile.special);

            // set key icon's rotation and then activate it
            Transform keyIcon = go.transform.GetChild(ARROW_OR_KEY_CHILD_INDEX);
            keyIcon.rotation = Quaternion.identity;
            keyIcon.gameObject.SetActive(true);

            // trigger appropriate script logics
            go.GetComponent<TileEditGreen>().DrawLinesToAllTargets();

            // flag event notification
            mappingChanged = true;
        }

        // if the tile is orange
        if (inTile.color == TileColor.Orange)
        {
            // set arrow icon direction
            go.GetComponent<TileEditOrange>()
                .SetGravityDirection((GravityDirection)inTile.special);
            // set arrow icon to active
            go.transform.GetChild(ARROW_OR_KEY_CHILD_INDEX).gameObject.SetActive(true);
        }

        // handle event notifications
        if (mappingChanged)
            NotifyKeyDoorMappingChanged();

        // finally, return the gameObject
        return go;
    }

    // instantiates GameObjects and builds lookup dictionaries based on the given LevelData
    private void buildLevel(LevelData inLevel)
    {
        // first, prefab references are arrayed for indexed access
        GameObject[,] prefab_refs = new GameObject[Constants.NUM_SHAPES, Constants.NUM_COLORS];
        foreach (Transform tileGroup in tileCreator.transform)
        {
            foreach (Transform tile in tileGroup)
            {
                int tgi = tileGroup.GetSiblingIndex();
                int ti = tile.GetSiblingIndex();
                prefab_refs[tgi, ti] = tile.gameObject;
            }
        }

        // create default number of level layers
        for (int i = 0; i < Constants.DEFAULT_NUM_LAYERS; i++)
        {
            GameObject tileLayer = new GameObject();
            tileLayer.name = "Layer #" + i;
            tileLayer.transform.position = new Vector3(0f, 0f, 2f * i);
            tileLayer.transform.SetParent(tileMap.transform);
        }

        bool mappingChanged = false;
        // build each tile in the level
        foreach (TileData td in inLevel.tileSet)
        {
            // make sure there are enough layers for the new tile
            addLayers(td.orient.layer);
            Transform tileLayer = tileMap.transform.GetChild(td.orient.layer);
            GameObject pfRef = prefab_refs[(int)td.type, (int)td.color];
            Quaternion q;
            Vector3 v3 = td.orient.ToUnitySpace(out q);
            GameObject go = Instantiate(pfRef, v3, q) as GameObject;
            go.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
            go.transform.SetParent(tileLayer);
            // once tile is built, add (GameObject,TileData) pair to _tileLookup
            _tileLookup.Add(go, td);

            // do something with the lock icon, depending on whether tile has a valid doorID
            Transform lockIcon = go.transform.GetChild(LOCK_CHILD_INDEX);

            // if the tile has a valid door value
            if (td.doorID != 0)
            {
                // orient lock icon appropriately
                lockIcon.rotation = Quaternion.identity;

                // add it to the door tile map
                _doorTileMap.Add(go, td.doorID);

                // trigger appropriate script logics
                foreach (KeyValuePair<GameObject, int> kvp in _greenTileMap)
                {
                    if (kvp.Value == td.doorID)
                        kvp.Key.GetComponent<TileEditGreen>().DrawLinesToAllTargets();
                }

                // flag event notification
                mappingChanged = true;
            }
            else
            {
                // otherwise, simply deactivate the lock icon
                lockIcon.gameObject.SetActive(false);
            }

            // if the tile is green and has a valid key value
            if (td.color == TileColor.Green && td.special != 0)
            {
                // add it to the green tile map
                _greenTileMap.Add(go, td.special);

                // trigger script logic
                go.GetComponent<TileEditGreen>().DrawLinesToAllTargets();

                // flag event notification
                mappingChanged = true;
            }

            // if tile is green or orange
            if (td.color == TileColor.Green || td.color == TileColor.Orange)
            {
                // first, get a reference to the arrow/key icon
                Transform specIcon = go.transform.GetChild(ARROW_OR_KEY_CHILD_INDEX);

                // if the tile is green, but no valid key value
                if (td.color == TileColor.Green && td.special == 0)
                    // simply deactivate the key icon
                    specIcon.gameObject.SetActive(false);
                // if the tile is orange
                else if (td.color == TileColor.Orange)
                    // set the arrow icon's direction
                    go.GetComponent<TileEditOrange>()
                        .SetGravityDirection((GravityDirection)td.special);
                else
                    // if the tile isn't orange, the key icon defaults to identity rotation
                    specIcon.rotation = Quaternion.identity;
            }
        }

        if (mappingChanged)
            NotifyKeyDoorMappingChanged();

        // build each checkpoint in the level
        foreach (CheckpointData cd in inLevel.chkpntSet)
        {
            // make sure there are enough layers for the new checkpoint
            addLayers(cd.layer);
            Vector3 v3 = cd.locus.ToUnitySpace();
            // checkpoints' z positions are assigned by corresponding tileMap layer
            v3.z = GetLayerDepth(cd.layer);
            GameObject go = Instantiate(checkpointTool, v3, Quaternion.identity);
            go.transform.SetParent(checkpointMap.transform);
            go.SetActive(true);
            go.GetComponent<SpecialCreator>().enabled = false;
            // add the (GameObject,CheckpointData) pair to _checkpointLookup
            _checkpointLookup.Add(go, cd);
        }

        // build each warp in the level
        foreach (WarpData wd in inLevel.warpSet)
        {
            // make sure there are enough layers for the new warp
            addLayers(wd.targetLayer); // targetLayer is layer + 1
            Vector3 v3 = wd.locus.ToUnitySpace();
            v3.z = GetLayerDepth(wd.layer);
            GameObject go = Instantiate(warpTool, v3, Quaternion.identity);
            go.transform.SetParent(warpMap.transform);
            go.SetActive(true);
            go.GetComponent<SpecialCreator>().enabled = false;
            // add the (GameObject,WarpData) pair to _warpLookup
            _warpLookup.Add(go, wd);
        }

        // build each victory in the level
        foreach (VictoryData victoryData in inLevel.victorySet)
        {
            // make sure there are enough layers for the new warp
            addLayers(victoryData.layer);
            Vector3 v3 = victoryData.locus.ToUnitySpace();
            v3.z = GetLayerDepth(victoryData.layer);
            GameObject go = Instantiate(victoryTool, v3, Quaternion.identity);
            go.transform.SetParent(victoryMap.transform);
            go.SetActive(true);
            go.GetComponent<SpecialCreator>().enabled = false;
            // add the GameObject,VictoryData pair to _victoryLookup
            _victoryLookup.Add(go, victoryData);
        }
    }

    // returns true if the mouse is hovering over any HUD element
    private bool checkHUDHover()
    {
        return _currentHUDhover.Count > 0;
    }

    // removes given layer from level, if layer has no tiles or isConfirmed
    private bool removeLayer(int inLayer, bool isConfirmed)
    {
        // if there are already more layers than the passed index, simply return
        if (inLayer < 0 || inLayer >= tileMap.transform.childCount)
            return false;

        Transform t = tileMap.transform.GetChild(inLayer);
        if (t.childCount > 0 && !isConfirmed)
            return false;

        Destroy(t.gameObject);

        // rename layers from the passed index onwards as appropriate
        for (int i = inLayer + 1; i < tileMap.transform.childCount; i++)
        {
            Transform tileLayer = tileMap.transform.GetChild(i);
            tileLayer.gameObject.name = "Layer #" + (i - 1).ToString();
            tileLayer.position = new Vector3(0f, 0f, (i - 1) * 2f);
        }

        return true;
    }

    // used when entering isEditorInEditMode with an item selected, which removes it
    private void removeSelectedItem()
    {
        if (_selectedItem.instance == null)
            return;
        if (_selectedItem.tileData.HasValue)
        {
            removeTile(_selectedItem.instance);
            // if _selectedItem is a tile, use tileData to set tileCreator
            tileCreator.SetProperties(_selectedItem.tileData.Value);
            // remove _selectedItem from level and set tile
            setTool(EditCreatorTool.Tile);
        }
        else if (_selectedItem.checkpointData.HasValue)
        {
            removeSpecial(_selectedItem.instance);
            // remove _selectedItem from level and set checkpoint
            setTool(EditCreatorTool.Checkpoint);
        }
        else if (_selectedItem.victoryData.HasValue)
        {
            removeSpecial(_selectedItem.instance);
            // remove _selectedItem from level and set victory
            setTool(EditCreatorTool.Victory);
        }
        else if (_selectedItem.warpData.HasValue)
        {
            removeSpecial(_selectedItem.instance);
            // remove _selectedItem from level and set warp
            setTool(EditCreatorTool.Warp);
        }
    }

    // removes a given special from the level
    private void removeSpecial(GameObject inSpecial)
    {
        CheckpointData cData;
        WarpData wData;
        VictoryData vData;
        if (IsMappedCheckpoint(inSpecial, out cData))
        {
            // if the given item is a checkpoint
            _selectedItem = new SelectedItem(inSpecial, cData);
            setTool(EditCreatorTool.Checkpoint);

            // set _selectedItem and tool then remove item from level and lookup
            levelData.chkpntSet.Remove(cData);
            _checkpointLookup.Remove(inSpecial);
        }
        else if (IsMappedWarp(inSpecial, out wData))
        {
            // if the given item is a warp
            _selectedItem = new SelectedItem(inSpecial, wData);
            setTool(EditCreatorTool.Warp);

            // set _selectedItem and tool then remove item from level and lookup
            levelData.warpSet.Remove(wData);
            _warpLookup.Remove(inSpecial);
        }
        else if (IsMappedVictory(inSpecial, out vData))
        {
            // must be victory, eh?
            _selectedItem = new SelectedItem(inSpecial, vData);
            setTool(EditCreatorTool.Victory);

            //set _selectedItem and tool then remove item from level and lookup
            levelData.victorySet.Remove(vData);
            _victoryLookup.Remove(inSpecial);
        }
        else
        {
            return;
        }

        // if either, destroy the passed object
        Destroy(inSpecial);
    }

    // removes a given tile from the level
    private void removeTile(GameObject inTile)
    {
        bool mappingChanged = false;
        // lookup the item's TileData
        TileData tData;
        bool b = IsMappedTile(inTile, out tData);
        // if the passed GameObject is not part of tileMap, we escape
        if (!b)
            return;

        // otherwise remove tile from the level and _tileLookup
        levelData.tileSet.Remove(tData);
        _tileLookup.Remove(inTile);
        // remove from green tile map if necessary
        if (_greenTileMap.ContainsKey(inTile))
        {
            _greenTileMap.Remove(inTile);
            mappingChanged = true;
        }
        // remove from the door tile map if necessary
        if (_doorTileMap.ContainsKey(inTile))
        {
            _doorTileMap.Remove(inTile);
            foreach (KeyValuePair<GameObject, int> kvp in _greenTileMap)
            {
                if (kvp.Value == tData.doorID)
                    kvp.Key.GetComponent<TileEditGreen>().DrawLinesToAllTargets();
            }
            mappingChanged = true;
        }

        if (mappingChanged)
            NotifyKeyDoorMappingChanged();

        Destroy(inTile);
    }

    // sets the opacity of all tiles within a layer using ordinal distance from activeLayer
    private void setTileLayerOpacity(Transform tileLayer, int distance)
    {
        // opacity and layer are calculated for non-active layers
        float alpha = 1f;
        int layer = DEFAULT_LAYER;
        if (distance != 0)
        {
            // alpha is calculated as (1/2)^distance from active layer
            alpha = (float)Math.Pow(0.5, (double)distance);
            layer = INACTIVE_LAYER;
        }
        Color color = new Color(1f, 1f, 1f, alpha);

        // each tile's sprite is colored appropriately
        foreach (Transform tile in tileLayer)
        {
            tile.gameObject.layer = layer;
            tile.GetChild(0).GetComponent<SpriteRenderer>().color = color;
        }
    }

    // set opacity and physics by given distance for given special
    private void setSpecialOpacity(Transform special, int distance)
    {
        // opacity and layer are calculated for non-active layers
        float alpha = 1f;
        int layer = DEFAULT_LAYER;
        if (distance != 0)
        {
            // alpha is calculated as (1/2)^distance from active layer
            alpha = (float)Math.Pow(0.5, (double)distance);
            layer = INACTIVE_LAYER;
        }

        // first, just set the gameObject's layer
        special.gameObject.layer = layer;

        // then, each special's animation is colored appropriately
        CheckpointData cd;
        if (IsMappedCheckpoint(special.gameObject, out cd))
        {
            // copies a whole module to modify one value
            ParticleSystem.ColorOverLifetimeModule colm = special
                .GetChild(1)
                .GetComponent<ParticleSystem>()
                .colorOverLifetime;
            Gradient oldGradient = colm.color.gradient;
            Gradient newGradient = new Gradient();
            GradientColorKey[] colorKeys = oldGradient.colorKeys;
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[oldGradient.alphaKeys.Length];

            for (int i = 0; i < oldGradient.alphaKeys.Length; i++)
            {
                alphaKeys[i].time = oldGradient.alphaKeys[i].time;
                // we just modify the alpha of the first key, since the
                // checkpoint particles already fade to alpha=0 by time=1
                alphaKeys[i].alpha = i == 0 ? alpha : oldGradient.alphaKeys[i].alpha;
            }

            newGradient.SetKeys(colorKeys, alphaKeys);
            colm.color = new ParticleSystem.MinMaxGradient(newGradient);

            return;
        }

        WarpData wd;
        if (IsMappedWarp(special.gameObject, out wd))
        {
            // first, warps have two sprites to dim
            Color color = new Color(1f, 1f, 1f, alpha);

            special.GetChild(1).GetComponent<SpriteRenderer>().color = color;
            special.GetChild(2).GetComponent<SpriteRenderer>().color = color;

            // then, warps' particles are easier to dim, because
            // they don't have an existing opacity gradient that
            // otherwise needs to be preserved
            ParticleSystem.ColorOverLifetimeModule colm = special
                .GetChild(3)
                .GetComponent<ParticleSystem>()
                .colorOverLifetime;
            colm.enabled = true;
            colm.color = new ParticleSystem.MinMaxGradient(color);

            return;
        }

        VictoryData vd;
        if (IsMappedVictory(special.gameObject, out vd))
        {
            // victories work very similarly to checkpoints
            ParticleSystem.ColorOverLifetimeModule colm = special
                .GetChild(1)
                .GetComponent<ParticleSystem>()
                .colorOverLifetime;
            Gradient oldGradient = colm.color.gradient;
            Gradient newGradient = new Gradient();
            GradientColorKey[] colorKeys = oldGradient.colorKeys;
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[oldGradient.alphaKeys.Length];

            for (int i = 0; i < oldGradient.alphaKeys.Length; i++)
            {
                alphaKeys[i].time = oldGradient.alphaKeys[i].time;
                // victory particles also eventually fade like checkpoint
                // particles, but have more intervening keys; we only
                // preserve the (zero) alpha of the last key
                alphaKeys[i].alpha =
                    i < oldGradient.alphaKeys.Length ? alpha : oldGradient.alphaKeys[i].alpha;
            }

            newGradient.SetKeys(colorKeys, alphaKeys);
            colm.color = new ParticleSystem.MinMaxGradient(newGradient);

            return;
        }
    }

    // sets the currently active tool
    private void setTool(EditCreatorTool inTool)
    {
        switch (inTool)
        {
            case EditCreatorTool.Tile:
                _currentCreatorToolGameObject = tileCreator.gameObject;
                break;
            case EditCreatorTool.Checkpoint:
                _currentCreatorToolGameObject = checkpointTool;
                break;
            case EditCreatorTool.Warp:
                _currentCreatorToolGameObject = warpTool;
                break;
            case EditCreatorTool.Victory:
                _currentCreatorToolGameObject = victoryTool;
                break;
            case EditCreatorTool.Eraser:
                // missing implementation
                _currentCreatorToolGameObject = null;
                break;
            default:
                break;
        }

        _currentCreatorTool = inTool;
    }

    /* Public Utilities */

    public static string CleanAutosaveName(string levelName)
    {
        const string suffix = " (autosave)";
        if (levelName.EndsWith(suffix))
        {
            return levelName.Substring(0, levelName.Length - suffix.Length);
        }
        return levelName;
    }

    // returns the key value associated with a green tile, if one is known
    public int GetGreenTileKeyID(GameObject inTile)
    {
        return _greenTileMap.ContainsKey(inTile) ? _greenTileMap[inTile] : 0;
    }

    // returns the z value of the current layer's transform
    public float GetLayerDepth()
    {
        return GetLayerDepth(activeLayer);
    }

    // returns the z value of the given layer's transform
    public float GetLayerDepth(int inLayer)
    {
        return tileMap.transform.GetChild(inLayer).position.z;
    }

    public Collider2D GetObjectClicked()
    {
        // get mouse position from new Input System
        Vector2 screenPos = Mouse.current.position.ReadValue();

        // convert screen position to ray
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // calculate intersection with plane at active layer
        Plane plane = new Plane(Vector3.forward, -2f * activeLayer);
        if (!plane.Raycast(ray, out float distance))
            return null;

        // get world point of intersection
        Vector3 intersection = ray.GetPoint(distance);

        // cast an orthogonal ray through the layer at intersection point
        intersection.z -= 1f;
        Ray layerRay = new Ray(intersection, Vector3.forward);

        // perform 2D physics raycast
        RaycastHit2D hit = Physics2D.GetRayIntersection(layerRay, 2f);
        return hit.collider;
    }

    // returns a hashset of door tiles based on a passed ID
    public void GetDoorSet(int doorID, out HashSet<GameObject> knownDoors)
    {
        knownDoors = new HashSet<GameObject>();

        foreach (GameObject go in _doorTileMap.Keys)
        {
            if (_doorTileMap[go] == doorID)
                knownDoors.Add(go);
        }
    }

    // returns true if the given element is mouse hovered
    public bool IsLevelNameFieldHovered(LevelNameField element)
    {
        bool b = false;
        foreach (RaycastResult result in _currentHUDhover)
        {
            if (result.gameObject == element.gameObject)
                b = true;
            break;
        }
        return b;
    }

    // if passed object is a tile, supplies corresponding TileData
    public bool IsMappedTile(GameObject inTile, out TileData outData)
    {
        outData = new TileData();
        // if tile isn't mapped, output default values and return false
        if (!inTile || !_tileLookup.ContainsKey(inTile))
        {
            return false;
            // if it is, output the TileData and return true
        }
        else
        {
            outData = _tileLookup[inTile];
            return true;
        }
    }

    // if passed object is a checkpoint, supplies corresponding CheckpointData
    public bool IsMappedCheckpoint(GameObject inChkpnt, out CheckpointData outData)
    {
        outData = new CheckpointData();
        if (!inChkpnt || !_checkpointLookup.ContainsKey(inChkpnt))
        {
            return false;
            // if it is, output the CheckpointData and return true
        }
        else
        {
            outData = _checkpointLookup[inChkpnt];
            return true;
        }
    }

    // if passed object is a warp, supplies corresponding WarpData
    public bool IsMappedWarp(GameObject inWarp, out WarpData outData)
    {
        outData = new WarpData();
        // if warp isn't mapped, output default values and return false
        if (!inWarp || !_warpLookup.ContainsKey(inWarp))
        {
            return false;
            // if it is, output the WarpData and return true
        }
        else
        {
            outData = _warpLookup[inWarp];
            return true;
        }
    }

    // if passed object is a victory, supplies corresponding VictoryData
    public bool IsMappedVictory(GameObject inVictory, out VictoryData outData)
    {
        outData = new VictoryData();
        // if victory isn't mapped, output default values and return false
        if (!inVictory || !_victoryLookup.ContainsKey(inVictory))
        {
            return false;
            // if it is, output the VictoryData and return true
        }
        else
        {
            outData = _victoryLookup[inVictory];
            return true;
        }
    }

    public void SetSelectedItemSpecial(string s)
    {
        int newId = int.Parse(s);

        if (_selectedItem.tileData.HasValue)
        {
            TileData td = _selectedItem.tileData.Value;
            TileData tileDataModified = new TileData(
                td.type,
                td.color,
                newId,
                td.orient,
                td.doorID
            );

            if (_selectedItem.instance)
            {
                removeTile(_selectedItem.instance);
                GameObject newTile = addTile(tileDataModified);
                _selectedItem = new SelectedItem(newTile, tileDataModified);
            }
            else
                _selectedItem = new SelectedItem(tileDataModified);
        }
    }

    public void SetSelectedItemDoorID(string s)
    {
        int newId = int.Parse(s);

        if (_selectedItem.tileData.HasValue)
        {
            TileData td = _selectedItem.tileData.Value;
            TileData tileDataModified = new TileData(
                td.type,
                td.color,
                td.special,
                td.orient,
                newId
            );

            if (_selectedItem.instance)
            {
                removeTile(_selectedItem.instance);
                GameObject newTile = addTile(tileDataModified);
                _selectedItem = new SelectedItem(newTile, tileDataModified);
            }
            else
                _selectedItem = new SelectedItem(tileDataModified);
        }
    }

    public bool ShouldBlockWorldClick()
    {
        // palette open, typing into an input, or pointer over UI element
        return paletteMode
            || hoveringHUD
            || inputMode
            || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
    }
}
