using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

public class EditingTile : MonoBehaviour {

	// tileType must be declared by the tile prefab
	public int tileType;

	// tRotation represents the number of turns from reference (0 degrees)
	private int tRotation;
	// tColor represents the color of this tile
	private int tColor;
	// cColors manages the sprite renderers of the different tile colors
	private SpriteRenderer[] cColors;

	public int tileRotation {
		get { return tRotation; }
	}
	public int tileColor {
		get { return tColor; }
	}

	public void Awake () {
		tRotation = 0;
		tColor = 0;
		int clrs = transform.childCount;
		cColors = new SpriteRenderer[clrs];

		for (int i = 0; i < clrs; i++) {
			cColors[i] = transform.GetChild(i).gameObject.GetComponent<SpriteRenderer>();
			cColors[i].enabled = false;
		}

		cColors[tColor].enabled = true;
	}

	// rotate turns the tile clockwise or counter-clockwise in 30 degree increments
	public void rotate (bool clockwise) {
		tRotation += clockwise ? -1 : 1;
		tRotation = (tRotation + 12) % 12;
		transform.eulerAngles = new Vector3(0, 0, 30 * tRotation);
	}

	// cycleColor disables and enables child color prefabs
	public void cycleColor () {
		cColors[tColor].enabled = false;
		tColor = (tColor + 1) % cColors.Length;
		cColors[tColor].enabled = true;
	}

	// serialize turns this tile's attributes into strings separated by spaces
	public string serialize (hexLocus hl) {
		string s = tileType.ToString();
		s += " " + tColor.ToString();
		s += " " + hl.iA.ToString();
		s += " " + hl.iB.ToString();
		s += " " + hl.iC.ToString();
		s += " " + hl.iD.ToString();
		s += " " + hl.iE.ToString();
		s += " " + hl.iF.ToString();
		s += " " + tRotation.ToString();
		return s;
	}
}