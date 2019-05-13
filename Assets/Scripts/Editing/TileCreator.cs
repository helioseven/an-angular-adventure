using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

/* The TileCreator is how new tiles are added to the level in the editor. *
 * (*) all comments here are out of date
 *
 * The editor has two modes, "creation mode" and "editing mode";
 * when the editor is in creation mode, the genesis tile is used as a placeholder
 * for the new tile that is about to be created.
 * This works by having every possible shape/color combination of tile instantiated
 * in the same location, as children of the parent object owning this script,
 * and then turning off the renderers for all children except the working tile.
 *
 * In editing mode, the genesis tile is hijacked by whatever tile occupies the
 * buffer in EditGM.selectedTile (a private variable). EditGM handles the logic of
 * when to do this, TileCreator handles the logic of how to do it.
 */

public class TileCreator : MonoBehaviour {

	// tileRotation represents the number of turns from reference (0 degrees)
	public int tileRotation { get; private set; }
	// tileType represents the type of shape
	public int tileType { get; private set; }
	// tileColor represents the color of this tile
	public int tileColor { get; private set; }

	private SnapCursor anchor_ref;
	// tile_renderers manages the sprite renderers of the different tile colors
	private SpriteRenderer[,] tile_renderers;

	public void Start ()
	{
		anchor_ref = EditGM.instance.anchorIcon;
		tileRotation = 0;
		tileType = 0;
		tileColor = 0;

		int nTypes = transform.childCount;
		int nColors = transform.GetChild(0).childCount;
		tile_renderers = new SpriteRenderer[nTypes, nColors];

		for (int i = 0; i < nTypes; i++) {
			for (int j = 0; j < nColors; j++) {
				Transform t = transform.GetChild(i).GetChild(j);
				tile_renderers[i, j] = t.GetComponentInChildren<SpriteRenderer>(); // <1>
				tile_renderers[i, j].enabled = false;
			}
		}

		tile_renderers[tileType, tileColor].enabled = true; // <2>

		/*
		<1> gets the sprite renderer for each of the tile types and colors
		<2> turns all renderers off except the active tile
		*/
	}

	public void Update ()
	{
		Vector3 v3 = anchor_ref.focus.ToUnitySpace(); // <1>
		v3.z = anchor_ref.transform.position.z; // <2>
		transform.position = v3;

		/*
		<1> when active, the genesis_tile will follow the focus
		<2> get our Z-depth from the SnapCursor
		*/
	}

	public void SetActive (bool inActive)
	{
		gameObject.SetActive(inActive);
	}

	// turns the tile clockwise or counter-clockwise in 30 degree increments
	public void SetRotation (int inRotation)
	{
		tileRotation = (inRotation + 12) % 12;
		transform.eulerAngles = new Vector3(0, 0, 30 * tileRotation);
	}

	// disables and enables renderers based on passed type
	public void SelectType (int inType)
	{
		tile_renderers[tileType, tileColor].enabled = false;
		tileType = inType % tile_renderers.GetLength(0);
		tile_renderers[tileType, tileColor].enabled = true;
	}

	// disables and enables renderers based on color
	public void CycleColor (bool clockwise)
	{
		int cnt = tile_renderers.GetLength(1);
		tile_renderers[tileType, tileColor].enabled = false;
		int newColor = cnt + (clockwise ? tileColor + 1 : tileColor - 1); // <1>
		tileColor = newColor % cnt;
		tile_renderers[tileType, tileColor].enabled = true;

		/*
		<1> we add cnt so that modulus doesn't choke on a negative number
		*/
	}

	// sets type, color, and rotation by passed struct
	public void SetProperties (TileData inData)
	{
		tile_renderers[tileType, tileColor].enabled = false;
		tileType = inData.type;
		tileColor = inData.color;
		tile_renderers[tileType, tileColor].enabled = true;

		tileRotation = inData.orient.rotation;
		transform.eulerAngles = new Vector3(0, 0, 30 * tileRotation);
	}
 
	// returns a TileData representation of the genesisTile's current state
	public TileData GetTileData ()
	{
		return new TileData(tileType, tileColor, new HexOrient(anchor_ref.focus, tileRotation, EditGM.instance.activeLayer));
	}

	// returns a new tile copied from the tile in active use
	public GameObject GetActiveTile ()
	{
		GameObject go = tile_renderers[tileType, tileColor].transform.parent.gameObject;
		go = Instantiate(go, go.transform.position, go.transform.rotation) as GameObject;

		return go;
	}

	// returns an instantiated copy of a specified tile
	public GameObject NewTile (TileData inData)
	{
		GameObject go = tile_renderers[inData.type, inData.color].transform.parent.gameObject; // <1>
		Quaternion r = Quaternion.Euler(0, 0, 30 * inData.orient.rotation);
		Vector3 p = inData.orient.locus.ToUnitySpace();

		go = Instantiate(go, p, r) as GameObject;
		go.GetComponentInChildren<SpriteRenderer>().enabled = true; // <2>

		return go;

		/*
		<1> doesn't change the TileCreator itself, just uses its GameObjects to instantiate a copy as specified
		<2> make sure the renderer is on before handing the GameObject off
		*/
	}
}