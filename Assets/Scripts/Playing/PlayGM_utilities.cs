using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM {

	/* CONSTANTS */

	// layer names
	private const int INACTIVE_LAYER = 9;
	private const int DEFAULT_LAYER = 0;

	// default number of layers to load from file
	private const int DEFAULT_NUM_LAYERS = 5;

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

	/* Private Utilities */

	// calculates delta between each layer and desired active, sets accordingly
	private void activateLayer (int layerIndex)
	{
		activeLayer = layerIndex;

		foreach (Transform layer in tileMap.transform) { // <1>
			int layerNumber = layer.GetSiblingIndex();
			int distance = Math.Abs(layerNumber - activeLayer); // <2>
			if (activeLayer > layerNumber) distance += 2; // <3>
			setLayerOpacity(layer, distance);
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
			go.layer = cd.layer == 0 ? 10 : 9;
			go.transform.SetParent(chkpntMap.transform);
		}

		foreach (WarpData wd in inLevel.warpSet) { // <5>
			Quaternion q;
			Vector3 v3 = wd.orient.ToUnitySpace(out q);
			GameObject go = Instantiate(warpRef, v3, q) as GameObject;
			int lBase = wd.orient.layer;
			int lTarget = wd.targetLayer;
			bool isBase = lBase == 0;
			bool isTarget = lTarget == 0;
			Warp w = go.GetComponent<Warp>();
			w.baseLayer = lBase;
			w.targetLayer = lTarget;
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
	private void setLayerOpacity (Transform tileLayer, int distance)
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
		}

		/*
		<1> active layer gets default values, otherwise opacity and layer are calculated
		<2> alpha is calculated as (1/2)^distance
		*/
	}
}
