using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

/* The "genesis tile" is how new tiles are added to the level in the editor.
 *
 * The editor has two modes, "creation mode" and "editing mode";
 * when the editor is in creation mode, the genesis tile is used as a placeholder
 * for the new tile that is about to be created.
 * This works by having every possible shape/color combination of tile instantiated
 * in the same location, as children of the parent object owning this script,
 * and then turning off the renderers for all children except the working tile.
 *
 * In edit mode, we simply disable the genesis tile and all of it's children.
 */

public class GenesisTile : MonoBehaviour {

	// tileRenderers manages the sprite renderers of the different tile colors
	private SpriteRenderer[,] tileRenderers;

	// tileRotation represents the number of turns from reference (0 degrees)
	public int tileRotation { get; private set; }
	// tileType represents the type of shape
	public int tileType { get; private set; }
	// tileColor represents the color of this tile
	public int tileColor { get; private set; }

	public void Awake ()
	{
		tileRotation = 0;
		tileType = 0;
		tileColor = 0;

		int nTypes = transform.childCount;
		int nColors = transform.GetChild(0).childCount;
		tileRenderers = new SpriteRenderer[nTypes, nColors];

		// gets the sprite renderer for each of the tile types and colors
		for (int i = 0; i < nTypes; i++) {
			for (int j = 0; j < nColors; j++) {
				Transform t = transform.GetChild(i).GetChild(j);
				tileRenderers[i, j] = t.GetComponentInChildren<SpriteRenderer>();
				tileRenderers[i, j].enabled = false;
			}
		}

		tileRenderers[tileType, tileColor].enabled = true;
	}

	// turns the tile clockwise or counter-clockwise in 30 degree increments
	public void rotate (bool clockwise)
	{
		tileRotation += clockwise ? -1 : 1;
		tileRotation = (tileRotation + 12) % 12;
		transform.eulerAngles = new Vector3(0, 0, 30 * tileRotation);
	}

	// disables and enables renderers based on passed type
	public void selectType (int inType)
	{
		tileRenderers[tileType, tileColor].enabled = false;
		tileType = inType % tileRenderers.GetLength(0);
		tileRenderers[tileType, tileColor].enabled = true;
	}

	// disables and enables renderers based on color
	public void cycleColor ()
	{
		tileRenderers[tileType, tileColor].enabled = false;
		tileColor = (tileColor + 1) % tileRenderers.GetLength(1);
		tileRenderers[tileType, tileColor].enabled = true;
	}

	// sets type, color, and rotation by passed struct
	public void setProperties (tileData inData)
	{
		tileRenderers[tileType, tileColor].enabled = false;
		tileType = inData.type;
		tileColor = inData.color;
		tileRenderers[tileType, tileColor].enabled = true;

		tileRotation = inData.rotation;
		transform.eulerAngles = new Vector3(0, 0, 30 * tileRotation);
	}

	// returns a new tile copied from the tile in active use
	public GameObject getActiveTile ()
	{
		// instantiates a copy of the currently active tile and returns it
		GameObject go = tileRenderers[tileType, tileColor].transform.parent.gameObject;
		go = Instantiate(go, go.transform.position, go.transform.rotation) as GameObject;

		return go;
	}

	// returns an instantiated copy of a specified tile
	public GameObject newTile (tileData inData)
	{
		// doesn't make any changes to the GenesisTile itself,
		// just grabs the necessary GameObject and instantiates a copy as specified
		GameObject go = tileRenderers[inData.type, inData.color].transform.parent.gameObject;
		Quaternion r = Quaternion.Euler(0, 0, 30 * inData.rotation);
		Vector3 p = inData.locus.toUnitySpace();

		go = Instantiate(go, p, r) as GameObject;
		// make sure the renderer is on
		go.GetComponentInChildren<SpriteRenderer>().enabled = true;

		return go;
	}
}