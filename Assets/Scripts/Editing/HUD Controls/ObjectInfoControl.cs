using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class ObjectInfoControl : MonoBehaviour {

	// private variables
	private EditGM gm_ref;
	private TileCreator tc_ref;
	private int tile_type;
	private int tile_color;
	private int tile_rotation;
	private HexLocus tile_position;
	private float[] aspect_ratios;
	private string[] types_display;
	private string[] colors_display;

	void Awake ()
	{
		tile_type = 0;
		tile_color = 0;
		tile_rotation = 0;
		tile_position = new HexLocus();
		aspect_ratios = new float[] {1f, 2f, 2f, 1f, 1f, 2f};
		types_display = new string[] {
			"Triangle",
			"Diamond",
			"Trapezoid",
			"Hexagon",
			"Square",
			"Wedge"
		};
		colors_display = new string[] {
			"Black",
			"Blue",
			"Brown",
			"Green",
			"Orange",
			"Purple",
			"Red",
			"White"
		};

		transform.GetChild(2).gameObject.SetActive(false);
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		tc_ref = gm_ref.tileCreator;
	}

	void Update ()
	{
		bool bType = tile_type != tc_ref.tileType;
		bool bColor = tile_color != tc_ref.tileColor;
		bool bRotation = tile_rotation != tc_ref.tileOrient.rotation;
		bool bPosition = tile_position != gm_ref.anchorIcon.anchor;

		setValues();

		if (bType || bColor) updateDisplay();
		if (bType || bColor || bRotation || bPosition) updateInfo();
	}

	/* Private Functions */

	// updates the display image
	private void updateDisplay ()
	{
		Transform t = tc_ref.transform.GetChild(tile_type).GetChild(tile_color).GetChild(0);
		Image i = transform.GetChild(0).GetChild(0).GetComponent<Image>();
		i.sprite = t.GetComponent<SpriteRenderer>().sprite;
		i.GetComponent<AspectRatioFitter>().aspectRatio = aspect_ratios[tile_type];
	}

	// updates the standard attributes panel
	private void updateInfo ()
	{
		Transform t = transform.GetChild(1);

		t.GetChild(1).GetComponent<Text>().text = types_display[tile_type];
		t.GetChild(3).GetComponent<Text>().text = colors_display[tile_color];
		t.GetChild(5).GetComponent<Text>().text = tile_rotation.ToString();
		t.GetChild(7).GetComponent<Text>().text = tile_position.PrettyPrint();
	}

	// updates variables from references and returns true if any change
	private void setValues ()
	{
		tile_type = tc_ref.tileType;
		tile_color = tc_ref.tileColor;
		tile_rotation = tc_ref.tileOrient.rotation;
		tile_position = gm_ref.anchorIcon.anchor;
	}
}
