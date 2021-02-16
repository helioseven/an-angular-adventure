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
	private Image object_display;
	private AspectRatioFitter object_display_ARF;
	private Text type_display;
	private Text color_display;
	private Text rotation_display;
	private Text locus_display;
	private Text special_label;
	private Text special_display;

	private int tile_type;
	private int tile_color;
	private int tile_special;
	private int tile_rotation;
	private HexLocus tile_position;

	private float[] aspect_ratios;
	private string[] type_strings;
	private string[] color_strings;

	void Awake ()
	{
		tile_type = 0;
		tile_color = 0;
		tile_special = 0;
		tile_rotation = 0;
		tile_position = new HexLocus();
		aspect_ratios = new float[] {1f, 2f, 2f, 1f, 1f, 2f};
		type_strings = new string[] {
			"Triangle",
			"Diamond",
			"Trapezoid",
			"Hexagon",
			"Square",
			"Wedge"
		};
		color_strings = new string[] {
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

		object_display = transform.GetChild(0).GetChild(0).GetComponent<Image>();
		object_display_ARF = object_display.GetComponent<AspectRatioFitter>();

		Transform t = transform.GetChild(1);
		type_display = t.GetChild(1).GetComponent<Text>();
		color_display = t.GetChild(3).GetComponent<Text>();
		rotation_display = t.GetChild(5).GetComponent<Text>();
		locus_display = t.GetChild(7).GetComponent<Text>();

		t = transform.GetChild(2);
		special_label = t.GetChild(0).GetComponent<Text>();
		special_display = t.GetChild(1).GetComponent<Text>();
	}

	void Update ()
	{
		bool b = false;

		if (tile_type != tc_ref.tileType) {
			tile_type = tc_ref.tileType;
			b = true;
		}
		if (tile_color != tc_ref.tileColor) {
			tile_color = tc_ref.tileColor;
			b = true;
		}
		if (tile_rotation != tc_ref.tileOrient.rotation) {
			tile_rotation = tc_ref.tileOrient.rotation;
			b = true;
		}
		if (tile_position != gm_ref.anchorIcon.anchor) {
			tile_position = gm_ref.anchorIcon.focus;
			b = true;
		}

		if (b) UpdateUI();
	}

	/* Public Functions */

	// updates the display image
	public void UpdateUI ()
	{
		Transform t = tc_ref.transform.GetChild(tile_type).GetChild(tile_color).GetChild(0);
		object_display.sprite = t.GetComponent<SpriteRenderer>().sprite;
		object_display_ARF.aspectRatio = aspect_ratios[tile_type];

		type_display.text = type_strings[tile_type];
		color_display.text = color_strings[tile_color];
		rotation_display.text = tile_rotation.ToString();
		locus_display.text = tile_position.PrettyPrint();

		bool b = false;
		special_display.text = tile_special.ToString();
		if (tile_color == 3) {
			special_label.text = "Switch Target:";
			b = true;
			return;
		}
		if (tile_color == 4) {
			special_label.text = "Gravity Target:";
			b = true;
			return;
		}

		special_label.text = "Special Value:";
		transform.GetChild(2).gameObject.SetActive(b);
	}
}
