using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM {

	/* Private Utilities */

	// calculates delta between each layer and desired active, sets accordingly
	private void activateLayer (int layerIndex)
	{
		foreach (Transform tileLayer in tileMap.transform) { // <1>
			int d = tileLayer.GetSiblingIndex();
			d = Math.Abs(d - layerIndex); // <2>
			setLayerOpacity(tileLayer, d);
		}

		/*
		<1> cycle through all layers and calls setLayerOpacity appropriately
		<2> delta is simply absolute distance between layers
		*/
	}

	// uses given levelData to build tileMap and place playerStart
	private void buildLevel (LevelData inLevel)
	{
		playerStart = new HexOrient(new HexLocus(0, 0, 0, 0, 0, -8), 0, 0); // <1>

		GameObject[,] prefab_refs = new GameObject[6, 8]; // <1>
		foreach (Transform tileType in tileCreator.transform)
			foreach (Transform tile in tileType)
				prefab_refs[tileType.GetSiblingIndex(), tile.GetSiblingIndex()] = tile.gameObject;

		for (int i = 0; i < 2; i++) { // <2>
			GameObject tileLayer = new GameObject();
			tileLayer.name = "Layer #" + i;
			tileLayer.transform.position = new Vector3(0f, 0f, 2f * i);
			tileLayer.transform.SetParent(tileMap.transform);
		}

		foreach (TileData td in inLevel.tileSet) { // <3>
			GameObject pfRef = prefab_refs[td.type, td.color];
			Quaternion q = Quaternion.Euler(0, 0, 30 * td.orient.rotation);
			Vector3 v3 = td.orient.locus.ToUnitySpace();
			// next two steps should be obsolete w/ HexOrient.ToUnitySpace()
			int tdLayer = td.orient.layer;
			v3.z = 2f * tdLayer;
			GameObject go = Instantiate(pfRef, v3, q) as GameObject;
			go.transform.SetParent(tileMap.transform.GetChild(tdLayer));
		}

		foreach (ChkpntData cd in inLevel.chkpntSet) { // <4>
			Vector3 v3 = cd.locus.ToUnitySpace();
			v3.z = tileMap.transform.GetChild(cd.layer).position.z;
			GameObject go = Instantiate(chkpntRef, v3, Quaternion.identity) as GameObject;
			go.transform.SetParent(chkpntMap.transform);
		}

		foreach (WarpData wd in inLevel.warpSet) { // <5>
			Vector3 v3 = wd.orient.locus.ToUnitySpace();
			v3.z = tileMap.transform.GetChild(wd.orient.layer).position.z;
			Quaternion q = Quaternion.Euler(0, 0, 30 * wd.orient.rotation);
			GameObject go = Instantiate(warpRef, v3, q) as GameObject;
			go.transform.SetParent(warpMap.transform);
		}

		/*
		<1> hard-coded player start for now (!!) needs to change
		<1> prefab references to tiles are arrayed for easy access
		<2> create level layers (hard-coded amount for now)
		<3> populate tile hierarchy
		<4> populate checkpoint map
		<5> populate warp map
		*/
	}

	// set opacity and physics by given distance for each tile in given layer
	private void setLayerOpacity (Transform tileLayer, int distance)
	{
		float a = 1f; // <1>
		int l = 0; // <2>
		if (distance != 0) {
			a = 1f / (distance + 1f); // <3>
			l = 9;
		}
		Color c = new Color(1f, 1f, 1f, a); // <4>

		foreach (Transform tile in tileLayer) { // <5>
			tile.gameObject.layer = l;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
		}

		/*
		<1> a represents an alpha value
		<2> the physics layer we will be setting, 0 if active or 9 otherwise
		<3> opacity is calculated by 1/(x+1), falling off with distance
		<4> color is generated from opacity calculation
		<5> each tile in the layer is assigned new physics layer and opacity
		*/
	}
}