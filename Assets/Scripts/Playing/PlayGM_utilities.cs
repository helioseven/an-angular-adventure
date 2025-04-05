using System;
using circleXsquares;
using UnityEngine;

public partial class PlayGM
{
    /* Public Enums */

    public enum GravityDirection
    {
        Down = 0,
        Left,
        Up,
        Right,
    }

    /* Public Utilities */

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

    // calculation helper for downward impacts
    public float ImpactIntensityToVolume(Vector2 velocity, Vector2 gravity)
    {
        float volumeMultiplier = 0.007f;
        float minVolume = 0.001f;
        float maxVolume = 0.3f;

        // dot product calculates projection of velocity vector onto gravity vector
        float bounceForce = gravity.x * velocity.x + gravity.y * velocity.y;
        float intensity = Mathf.Abs(bounceForce * volumeMultiplier);
        float volume = Mathf.Clamp(intensity, minVolume, maxVolume);

        return volume;
    }

    // calculation helper for sliding impacts
    public float SlideIntensityToVolume(Vector2 velocity, Vector2 gravity)
    {
        float volumeMultiplier = 0.007f;
        float minVolume = 0.001f;
        float maxVolume = 0.3f;

        // dot product calculates projection of velocity vector onto slide vector
        // (perpendicular to gravity vector)
        float bounceForce = gravity.y * velocity.x + gravity.x * velocity.y;
        float intensity = Mathf.Abs(bounceForce * volumeMultiplier);
        float volume = Mathf.Clamp(intensity, minVolume, maxVolume);

        return volume;
    }

    /* Private Utilities */

    // calculates delta between each layer and desired active, sets accordingly
    private void activateLayer(int layerIndex)
    {
        // activeLayer is source of truth for active physics layer by index
        activeLayer = layerIndex;

        int targetLayer = LayerMask.NameToLayer(INT_TO_NAME[activeLayer]);

        player.gameObject.layer = targetLayer;

        foreach (Transform child in player.transform)
        {
            child.gameObject.layer = targetLayer;
        }

        // update opacity and physics for all tile layers
        foreach (Transform layer in tileMap.transform)
        {
            int layerNumber = layer.GetSiblingIndex();
            // delta is absolute distance between layers
            int distance = Math.Abs(layerNumber - activeLayer);
            // foreground layers are faded more
            if (activeLayer > layerNumber)
                distance += 2;
            setLayerOpacity(layer, distance);
        }

        // update opacity and physics for all checkpoints
        foreach (Transform checkpoint in checkpointMap.transform)
        {
            int layerNumber = checkpoint.gameObject.GetComponent<Checkpoint>().data.layer;
            int distance = Math.Abs(layerNumber - activeLayer);
            if (activeLayer > layerNumber)
                distance += 2;
            setCheckpointOpacity(checkpoint, distance);
        }

        // update opacity for all victories
        foreach (Transform victory in victoryMap.transform)
        {
            int layerNumber = victory.gameObject.GetComponent<Victory>().data.layer;
            int distance = Math.Abs(layerNumber - activeLayer);
            if (activeLayer > layerNumber)
                distance += 2;
            setVictoryOpacity(victory, distance);
        }

        // update physics for warps
        foreach (Transform warp in warpMap.transform)
        {
            Warp w = warp.gameObject.GetComponent<Warp>();
            bool isConnected = activeLayer == w.baseLayer || activeLayer == w.targetLayer;
            bool isFrontActive = activeLayer == w.baseLayer;
            setWarpOpacityAndPhysics(warp, isConnected, isFrontActive);
        }
    }

    // uses given levelData to build tileMap and place _playerStart
    private void buildLevel(LevelData inLevel)
    {
        // prefab references to tiles are arrayed for easy access
        GameObject[,] prefab_refs = new GameObject[Constants.NUM_SHAPES, Constants.NUM_COLORS];
        foreach (Transform tileType in tileCreator.transform)
        {
            foreach (Transform tile in tileType)
                prefab_refs[tileType.GetSiblingIndex(), tile.GetSiblingIndex()] = tile.gameObject;
        }

        // create default number of level layers
        for (int i = 0; i < Constants.DEFAULT_NUM_LAYERS; i++)
        {
            GameObject tileLayer = new GameObject();
            tileLayer.name = "Layer #" + i;
            tileLayer.transform.position = new Vector3(0f, 0f, 2f * i);
            tileLayer.transform.SetParent(tileMap.transform);
        }

        // used in loop below for lock rotation
        Vector3 rotation = Vector3.forward;

        // populate tile hierarchy
        foreach (TileData td in inLevel.tileSet)
        {
            GameObject pfRef = prefab_refs[td.type, td.color];
            Quaternion q;
            Vector3 v3 = td.orient.ToUnitySpace(out q);
            GameObject go = Instantiate(pfRef, v3, q) as GameObject;
            Tile t = go.GetComponent<Tile>();
            if (t)
                t.data = td;
            go.transform.SetParent(tileMap.transform.GetChild(td.orient.layer));
            go.layer = LayerMask.NameToLayer(INT_TO_NAME[td.orient.layer]);

            // keep icon if orange gravity or green key
            if (td.color == (int)TileColor.Orange || td.color == (int)TileColor.Green)
                continue;

            // Lose door icon if non doorId tile
            Transform icon = go.transform.GetChild(0).GetChild(0);

            // TODO: 0 should represent "not a door" (right now it doesn't - update in Tile_Green.cs)
            if (td.doorId == 0)
                icon.gameObject.SetActive(false);
            else
                icon.localRotation = Quaternion.Euler(rotation - go.transform.rotation.eulerAngles);
        }

        // populate checkpoint map
        foreach (CheckpointData cd in inLevel.chkpntSet)
        {
            Vector3 v3 = cd.locus.ToUnitySpace();
            v3.z = tileMap.transform.GetChild(cd.layer).position.z;
            GameObject go = Instantiate(chkpntRef, v3, Quaternion.identity) as GameObject;

            Checkpoint checkpointGO = go.GetComponent<Checkpoint>();
            if (checkpointGO)
            {
                checkpointGO.data = cd;
            }

            go.layer = LayerMask.NameToLayer(INT_TO_NAME[cd.layer]);
            go.transform.SetParent(checkpointMap.transform);
        }

        // populate victory map
        foreach (VictoryData vd in inLevel.victorySet)
        {
            Vector3 v3 = vd.locus.ToUnitySpace();
            v3.z = tileMap.transform.GetChild(vd.layer).position.z;
            GameObject go = Instantiate(victoryRef, v3, Quaternion.identity) as GameObject;

            Victory v = go.GetComponent<Victory>();
            if (v)
                v.data = vd;
            go.layer = LayerMask.NameToLayer(INT_TO_NAME[vd.layer]);
            go.transform.SetParent(victoryMap.transform);
        }

        // get starting checkpoing to set warp data physics layer
        CheckpointData start = inLevel.chkpntSet[0];

        // populate warp map
        foreach (WarpData wd in inLevel.warpSet)
        {
            Vector3 v3 = wd.locus.ToUnitySpace();
            v3.z = tileMap.transform.GetChild(wd.layer).position.z;
            GameObject go = Instantiate(warpRef, v3, Quaternion.identity) as GameObject;
            int baseLayer = wd.layer;
            int targetLayer = wd.targetLayer;
            Warp w = go.GetComponent<Warp>();
            if (w)
                w.data = wd;

            // Enable warp if on one of its layers
            if (baseLayer == start.layer || targetLayer == start.layer)
                go.layer = LayerMask.NameToLayer(INT_TO_NAME[start.layer]);
            else
                go.layer = LayerMask.NameToLayer("Inactive");
            go.transform.SetParent(warpMap.transform);
        }

        // set up player
        HexLocus hl = start.locus;
        player.gameObject.layer = LayerMask.NameToLayer(INT_TO_NAME[start.layer]);
        // player starts at the first checkpoint
        _playerStart = new HexOrient(hl, 0, start.layer);
    }

    // set opacity by given distance for each tile in given layer
    private void setLayerOpacity(Transform tileLayer, int distance)
    {
        // calculate opacity and physics layer for each tile layer
        float alpha = 1f;
        // active tile layer uses default values
        if (distance != 0)
            // otherwise alpha is calculated as (1/2)^distance
            alpha = (float)Math.Pow(0.5, (double)distance);
        Color color = new Color(1f, 1f, 1f, alpha);

        foreach (Transform tile in tileLayer)
        {
            tile.GetChild(0).GetComponent<SpriteRenderer>().color = color;

            // if there are grandchildren, dim them too
            if (tile.GetChild(0).childCount > 0)
            {
                GameObject go = tile.GetChild(0).GetChild(0).gameObject;
                go.GetComponent<SpriteRenderer>().color = color;
            }
        }
    }

    // set opacity and physics layer by given distance for given checkpoint
    private void setCheckpointOpacity(Transform checkpoint, int distance)
    {
        bool isActive = false;
        if (activeCheckpoint != null)
        {
            isActive = checkpoint == activeCheckpoint.transform;
        }
        // calculate opacity and physics layer for each checkpoint
        float alpha = 0.5f;
        // checkpoints on active game layer use default values
        if (distance != 0)
        {
            alpha -= Math.Abs(distance * 0.2f);
            alpha = Math.Max(alpha, 0.1f);
        }

        // overwrite if it's the active checkpoint
        if (isActive)
        {
            alpha = 1f;
        }

        // Set color via particle effect emitter
        Transform child = checkpoint.transform.Find("CheckpointShimmer");
        if (child != null)
        {
            ParticleSystem checkpointParticleShimmer = child.GetComponent<ParticleSystem>();
            // ✨ Trigger the particle burst
            Color bright = new Color(1f, 1f, 1f, alpha);
            var main = checkpointParticleShimmer.main;
            main.startColor = bright;
        }
    }

    // set opacity and physics layer by given distance for given victory
    private void setVictoryOpacity(Transform victory, int distance)
    {
        // calculate opacity and physics layer for each victory
        float alpha = 1f;
        // victorys on active game layer use default values
        if (distance != 0)
        {
            alpha -= Math.Abs(distance * 0.3f);
            alpha = Math.Max(alpha, 0.1f);
        }

        // Set color via particle effect emitter
        Transform child = victory.transform.Find("Particle System");
        if (child != null)
        {
            ParticleSystem victoryParticleSystem = child.GetComponent<ParticleSystem>();

            var colorOverLifetime = victoryParticleSystem.colorOverLifetime;

            Gradient currentGradient = colorOverLifetime.color.gradient;
            Gradient newGradient = new Gradient();

            // Copy the color keys (preserve the rainbow)
            GradientColorKey[] colorKeys = currentGradient.colorKeys;

            // Replace just the alpha keys
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(alpha, 0.0f),
                new GradientAlphaKey(alpha, 1.0f),
            };

            newGradient.SetKeys(colorKeys, alphaKeys);
            colorOverLifetime.color = newGradient;
        }
    }

    // set opacity and physics layer for warps
    private void setWarpOpacityAndPhysics(Transform warp, bool isConnected, bool isFrontActive)
    {
        ParticleSystem particleSystem = warp.GetComponentInChildren<ParticleSystem>();
        // warps have 3 levels of opacity - connected but not active layer, active layer, and non active layer
        if (isConnected)
        {
            // turn on the particle system
            if (particleSystem)
            {
                var main = particleSystem.main;
                main.startColor = new Color(1f, 1f, 1f, 1f);
            }

            // activate
            warp.gameObject.layer = LayerMask.NameToLayer(INT_TO_NAME[activeLayer]);
            Color bright = new Color(1f, 1f, 1f, 1f);
            Color mediumDim = new Color(1f, 1f, 1f, 0.3f);
            Transform child = warp.transform.Find(
                isFrontActive ? "WarpOverlay" : "WarpOverlayBack"
            );
            if (child)
            {
                // set to bright if it's the active warp on the active layer
                child.gameObject.GetComponent<SpriteRenderer>().material.color = bright;
            }
            child = warp.transform.Find(isFrontActive ? "WarpOverlayBack" : "WarpOverlay");
            if (child)
            {
                // set to medium dim if attached to the active layer via the dropdown but is not on the active layer
                child.gameObject.GetComponent<SpriteRenderer>().material.color = mediumDim;
            }
        }
        else
        {
            // dim the particle system when not connected to active layer
            if (particleSystem)
            {
                var main = particleSystem.main;
                main.startColor = new Color(1f, 1f, 1f, 0.1f);
            }

            // dim both the front and the back of the warp if not connected to active layer
            Color dim = new Color(1f, 1f, 1f, 0.05f);
            Transform child = warp.transform.Find("WarpOverlay");
            if (child)
            {
                child.gameObject.GetComponent<SpriteRenderer>().material.color = dim;
            }
            child = warp.transform.Find("WarpOverlayBack");
            if (child)
            {
                child.gameObject.GetComponent<SpriteRenderer>().material.color = dim;
            }
        }
    }
}
