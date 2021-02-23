using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

/* The TileCreator is how new tiles are added to the level in the editor.
 * It behaves differently depending on which editor mode is active.
 *
 * At its root, the TileCreator works by having every possible
 * shape/color combination of tile instantiated in the same location,
 * as children of the parent object (which owns this script),
 * and turning on/off the renderers for those children.
 */

public class TileCreator : MonoBehaviour {

	// public read-accessibility state variables
	public int tileType { get; private set; }
	public int tileColor { get; private set; }
	public int tileSpecial { get; private set; }
	public HexOrient tileOrient { get; private set; }

	// private variables
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
		tileSpecial = 0;
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
		HexLocus f = anchor_ref.focus; // <1>
		int r = tileOrient.rotation;
		int l = gm_ref.activeLayer;
		tileOrient = new HexOrient(f, r, l);

		Quaternion q;
		transform.position = tileOrient.ToUnitySpace(out q);
		transform.rotation = q;

		/*
		<1> when active, the TileCreator will follow the focus
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

	// sets tile's special value if valid color is in use
	public void SetSpecial (int inSpecial)
	{
		if (tileColor == 3) tileSpecial = inSpecial;
		if (tileColor == 4) tileSpecial = (inSpecial + 4) % 4;
	}

	// turns the transform in 30 degree increments
	public void SetRotation (int inRotation)
	{
		tileOrient = new HexOrient(tileOrient.locus, inRotation, tileOrient.layer);
		Update();
	}

	// translates and rotates the transform according to given orientation
	public void SetOrientation (HexOrient inOrient)
	{
		tileOrient = inOrient;
		Update();
	}

	// sets type, color, and rotation by passed struct
	public void SetProperties (TileData inData)
	{
		tile_renderers[tileType, tileColor].enabled = false;
		tileType = inData.type;
		tileColor = inData.color;
		tileSpecial = inData.special;
		tile_renderers[tileType, tileColor].enabled = true;
		SetRotation(inData.orient.rotation);
	}

	// returns a TileData representation of the genesisTile's current state
	public TileData GetTileData ()
	{
		return new TileData(tileType, tileColor, tileSpecial, tileOrient);
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
