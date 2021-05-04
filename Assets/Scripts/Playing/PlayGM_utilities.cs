using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM {

	/* Enums */

	public enum GravityDirection
	{
		Down = 0,
		Left,
		Up,
		Right
	}

	/* Public Utilities */

	// simply returns the z value of the current layer's transform
	public float GetLayerDepth ()
	{ return GetLayerDepth(activeLayer); }

	// simply returns the z value of the given layer's transform
	public float GetLayerDepth (int inLayer)
	{ return tileMap.transform.GetChild(inLayer).position.z; }

	// ImpactIntensityToVolume - Downward impact calculation helper
	public float ImpactIntensityToVolume (Vector2 velocity, Vector2 gravity)
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

	// SlideIntensityToVolume - Downward impact calculation helper
	public float SlideIntensityToVolume (Vector2 velocity, Vector2 gravity)
	{
		float volumeMultiplier = 0.007f;
		float minVolume = 0.001f;
		float maxVolume = 0.3f;
		// dot product calculates projection of velocity vector onto slide vector (perpendicular to gravity vector)
		float bounceForce = gravity.y * velocity.x + gravity.x * velocity.y;
		float intensity = Mathf.Abs(bounceForce * volumeMultiplier);
		float volume = Mathf.Clamp(intensity, minVolume, maxVolume);
		return volume;
	}

	/* Private Utilities */

	// calculates delta between each layer and desired active, sets accordingly
	private void activateLayer (int layerIndex)
	{
		activeLayer = layerIndex;

		// update opacity and physics for all tile layers
		foreach (Transform layer in tileMap.transform) { // <1>
			int layerNumber = layer.GetSiblingIndex();
			int distance = Math.Abs(layerNumber - activeLayer); // <2>
			if (activeLayer > layerNumber) distance += 2; // <3>
			setLayerOpacityAndPhysics(layer, distance);
		}

		// update opacity and physics for all checkpoints
		foreach (Transform checkpoint in chkpntMap.transform) {
			int layerNumber = checkpoint.gameObject.GetComponent<Checkpoint>().data.layer;
			int distance = Math.Abs(layerNumber - activeLayer);
			if (activeLayer > layerNumber) distance += 2;
			setCheckpointOpacityAndPhysics(checkpoint, distance);
		}

		// update physics for warps
		foreach (Transform warp in warpMap.transform) {
			Warp w = warp.gameObject.GetComponent<Warp>();
			bool isActive = activeLayer == w.baseLayer || activeLayer == w.targetLayer;
			setWarpOpacityAndPhysics(warp, isActive);
		}

		/*
		<1> cycle through all layers and calls setLayerOpacity appropriately
		<2> delta is absolute distance between layers
		<3> foreground layers are faded more
		*/
	}

	// uses given levelData to build tileMap and place player_start
	private void buildLevel (LevelData inLevel)
	{
		GameObject[,] prefab_refs = new GameObject[6, 8]; // <1>
		foreach (Transform tileType in tileCreator.transform)
			foreach (Transform tile in tileType)
				prefab_refs[tileType.GetSiblingIndex(), tile.GetSiblingIndex()] = tile.gameObject;

		for (int i = 0; i < DEFAULT_NUM_LAYERS; i++) { // <2>
			GameObject tileLayer = new GameObject();
			tileLayer.name = "Layer #" + i;
			tileLayer.transform.position = new Vector3(0f, 0f, 2f * i);
			tileLayer.transform.SetParent(tileMap.transform);
		}

		foreach (TileData td in inLevel.tileSet) { // <3>
			GameObject pfRef = prefab_refs[td.type, td.color];
			Quaternion q;
			Vector3 v3 = td.orient.ToUnitySpace(out q);
			GameObject go = Instantiate(pfRef, v3, q) as GameObject;
			Tile t = go.GetComponent<Tile>();
			if (t) t.data = td;
			go.transform.SetParent(tileMap.transform.GetChild(td.orient.layer));
		}

		foreach (ChkpntData cd in inLevel.chkpntSet) { // <4>
			Vector3 v3 = cd.locus.ToUnitySpace();
			v3.z = tileMap.transform.GetChild(cd.layer).position.z;
			GameObject go = Instantiate(chkpntRef, v3, Quaternion.identity) as GameObject;
			Checkpoint c = go.GetComponent<Checkpoint>();
			if (c) c.data = cd;
			go.layer = cd.layer == 0 ? 10 : 9;
			go.transform.SetParent(chkpntMap.transform);
		}

		foreach (WarpData wd in inLevel.warpSet) { // <5>
			Quaternion q;
			Vector3 v3 = wd.orient.ToUnitySpace(out q);
			GameObject go = Instantiate(warpRef, v3, q) as GameObject;
			int baseLayer = wd.orient.layer;
			int targetLayer = wd.targetLayer;
			bool isBase = baseLayer == 0;
			bool isTarget = targetLayer == 0;
			Warp w = go.GetComponent<Warp>();
			if (w) w.data = wd;
			go.layer = isBase || isTarget ? 10 : 9;
			go.transform.SetParent(warpMap.transform);
		}

		ChkpntData start = inLevel.chkpntSet[0];
		HexLocus hl = start.locus;
		player_start = new HexOrient(hl, 0, start.layer); // <6>

		/*
		<1> prefab references to tiles are arrayed for easy access
		<2> create level layers (hard-coded amount for now)
		<3> populate tile hierarchy
		<4> populate checkpoint map
		<5> populate warp map
		<6> player starts at the first checkpoint
		*/
	}

	// set opacity and physics by given distance for each tile in given layer
	private void setLayerOpacityAndPhysics (Transform tileLayer, int distance)
	{
		float alpha = 1f;
		int layer = DEFAULT_LAYER;
		if (distance != 0) { // <1>
			alpha = (float)Math.Pow(0.5, (double)distance); // <2>
			layer = INACTIVE_LAYER;
		}
		Color color = new Color(1f, 1f, 1f, alpha);

		foreach (Transform tile in tileLayer) {
			tile.gameObject.layer = layer;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = color;

			// if there are grandchildren, dim them too
			if (tile.GetChild(0).childCount > 0) {
				tile.GetChild(0).GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = color;
			}
		}

		/*
		<1> active layer gets default values, otherwise opacity and layer are calculated
		<2> alpha is calculated as (1/2)^distance
		*/
	}

	// set opacity and physics by given distance for given checkpoint
	private void setCheckpointOpacityAndPhysics (Transform checkpoint, int distance)
	{
		float alpha = 1f;
		int layer = DEFAULT_LAYER;
		if (distance != 0) { // <1>
			alpha = (float)Math.Pow(0.5, (double)distance); // <2>
			layer = INACTIVE_LAYER;
		}
		Color color = new Color(1f, 1f, 1f, alpha);

		checkpoint.gameObject.layer = layer;
		checkpoint.GetChild(0).GetComponent<SpriteRenderer>().color = color;

		/*
		<1> active layer gets default values, otherwise opacity and layer are calculated
		<2> alpha is calculated as (1/2)^distance
		*/
	}

	// set physics collisions for warps
	private void setWarpOpacityAndPhysics (Transform warp, bool isActive) {
		int layer = DEFAULT_LAYER;
		if (!isActive) { // <1>
			layer = INACTIVE_LAYER;
		}

		warp.gameObject.layer = layer; // <2>

		/*
		<1> active layer gets default (active) values, otherwise disable
		<2> set physics layer
		*/
	}
}
