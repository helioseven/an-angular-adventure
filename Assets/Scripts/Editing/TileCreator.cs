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

	// public read-accessibility state variables
	public int tileType { get; private set; }
	public int tileColor { get; private set; }
	public HexOrient tileOrient { get; private set; }

	private EditGM gm_ref;
	private SnapCursor anchor_ref;
	// tile_renderers manages the sprite renderers of the different tile colors
	private SpriteRenderer[,] tile_renderers;

	void Start ()
	{
		gm_ref = EditGM.instance;
		anchor_ref = gm_ref.anchorIcon;
		tileType = 0;
		tileColor = 0;
		tileOrient = new HexOrient(new HexLocus(), 0, 0);

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

	void Update ()
	{
		tileOrient = new HexOrient(anchor_ref.focus, tileOrient.rotation, gm_ref.activeLayer);

		Vector3 v3 = tileOrient.locus.ToUnitySpace(); // <1>
		v3.z = gm_ref.GetLayerDepth(tileOrient.layer); // <2>
		transform.position = v3;

		/*
		<1> when active, the genesis_tile will follow the focus
		<2> get our Z-depth from the SnapCursor
		*/
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

	// turns the transform in 30 degree increments
	public void SetRotation (int inRotation)
	{
		tileOrient = new HexOrient(tileOrient.locus, (inRotation + 12) % 12, tileOrient.layer);
		transform.eulerAngles = new Vector3(0, 0, 30 * tileOrient.rotation);
	}

	// translates and rotates the transform according to given orientation
	public void SetOrientation (HexOrient inOrient)
	{
		tileOrient = inOrient;

		Vector3 v3 = tileOrient.locus.ToUnitySpace();
		v3.z = gm_ref.GetLayerDepth(tileOrient.layer);
		transform.position = v3;
		SetRotation(tileOrient.rotation);
	}

	// sets type, color, and rotation by passed struct
	public void SetProperties (TileData inData)
	{
		tile_renderers[tileType, tileColor].enabled = false;
		tileType = inData.type;
		tileColor = inData.color;
		tile_renderers[tileType, tileColor].enabled = true;

		SetOrientation(inData.orient);
	}

	// returns a TileData representation of the genesisTile's current state
	public TileData GetTileData ()
	{
		return new TileData(tileType, tileColor, tileOrient);
	}

	// returns a new tile copied from the tile in active use
	public GameObject GetActiveCopy ()
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
		p.z = gm_ref.GetLayerDepth();

		go = Instantiate(go, p, r) as GameObject;
		go.GetComponentInChildren<SpriteRenderer>().enabled = true; // <2>

		return go;

		/*
		<1> doesn't change the TileCreator itself, just uses its GameObjects to instantiate a copy as specified
		<2> make sure the renderer is on before handing the GameObject off
		*/
	}
}