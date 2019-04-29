using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class ObjectInfoControl : MonoBehaviour {

	//
	private EditGM gm_ref;
	private GenesisTile gt_ref;
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
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		gt_ref = gm_ref.genesisTile;
	}

	void Update ()
	{
		bool bType = tile_type != gt_ref.tileType;
		bool bColor = tile_color != gt_ref.tileColor;
		bool bRotation = tile_rotation != gt_ref.tileRotation;
		bool bPosition = tile_position != gm_ref.anchorIcon.anchor;

		setValues();

		if (bType || bColor) updateDisplay();
		if (bType || bColor || bRotation || bPosition) updateInfo();
	}

	/* Private Functions */

	//
	private void updateDisplay ()
	{
		Transform t = gt_ref.transform.GetChild(tile_type).GetChild(tile_color).GetChild(0);
		Image i = transform.GetChild(0).GetChild(0).GetComponent<Image>();
		i.sprite = t.GetComponent<SpriteRenderer>().sprite;
		i.GetComponent<AspectRatioFitter>().aspectRatio = aspect_ratios[tile_type];
	}

	//
	private void updateInfo ()
	{
		Transform t = transform.GetChild(1);

		t.GetChild(1).GetComponent<Text>().text = types_display[tile_type];
		t.GetChild(3).GetComponent<Text>().text = colors_display[tile_color];
		t.GetChild(5).GetComponent<Text>().text = tile_rotation.ToString();
		t.GetChild(7).GetComponent<Text>().text = printHexLocus(tile_position);
	}

	//
	private void setValues ()
	{
		tile_type = gt_ref.tileType;
		tile_color = gt_ref.tileColor;
		tile_rotation = gt_ref.tileRotation;
		tile_position = gm_ref.anchorIcon.anchor;
	}

	//
	private string printHexLocus (HexLocus inLocus)
	{
		string s = "";
		int[] h = new int[] {inLocus.a, inLocus.b, inLocus.c, inLocus.d, inLocus.e, inLocus.f};
		for (int i = 0; i < 6; i++) s += h.ToString() + ", ";
		return s.Substring(0, s.Length - 2);
	}
}