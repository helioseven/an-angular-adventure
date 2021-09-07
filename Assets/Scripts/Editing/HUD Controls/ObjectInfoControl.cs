using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class ObjectInfoControl : MonoBehaviour {

    // public references
    public EditGM gm_ref;
    public TileCreator tc_ref;
    public SpecialCreator ct_ref;
    public SpecialCreator wt_ref;

    // private variables
    private Image object_display;
    private AspectRatioFitter object_display_ARF;
    private Text type_display;
    private Text color_display;
    private Text rotation_display;
    private Text locus_display;
    private Text special_label;
    private Text special_display;

    private bool is_any_selected;
    private int tile_type;
    private int tile_color;
    private int tile_special;
    private int obj_rotation;
    private HexLocus obj_position;

    private float[] aspect_ratios;
    private string[] type_strings;
    private string[] color_strings;

    void Awake ()
    {
        is_any_selected = false;
        tile_type = 0;
        tile_color = 0;
        tile_special = 0;
        obj_rotation = 0;
        obj_position = new HexLocus();
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
        InfoPack ip = getUpdatedInfo();

        if (tile_type != ip.type) {
            tile_type = ip.type;
            b = true;
        }
        if (tile_color != ip.color) {
            tile_color = ip.color;
            b = true;
        }
        if (obj_rotation != ip.rot) {
            obj_rotation = ip.rot;
            b = true;
        }
        if (obj_position != ip.locus) {
            obj_position = ip.locus;
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
        object_display.gameObject.SetActive(is_any_selected); // <1>

        bool b = !is_any_selected || tile_type == -1;
        string s = b ? "[N/A]" : type_strings[tile_type];
        type_display.text = s;
        b = !is_any_selected || tile_color == -1;
        s = b ? "[N/A]" : color_strings[tile_color];
        color_display.text = s;
        b = !is_any_selected || obj_rotation == -1;
        s = b ? "[N/A]" : obj_rotation.ToString();
        rotation_display.text = s;
        s = !is_any_selected ? "[N/A]" : obj_position.PrettyPrint();
        locus_display.text = s; // <2>

        string sp = "Special Value:";
        b = false;
        if (tile_color == 3) {
            sp = "Switch Target:";
            b = true;
        }
        if (tile_color == 4) {
            sp = "Gravity Target:";
            b = true;
        }
        special_label.text = sp;
        special_display.text = tile_special.ToString();
        transform.GetChild(2).gameObject.SetActive(b); // <3>

        /*
        <1> set sprite and aspect ratio for object image, activate if appropriate
        <2> set text strings as appropriate
        <3> set special dropdown values, activate if appropriate
        */
    }

    /* Private Structs */

    // a struct for reporting what current information should be
    private struct InfoPack {
        public int type;
        public int color;
        public int rot;
        public HexLocus locus;

        public InfoPack (int inType, int inColor, int inRot, HexLocus inLocus) {
            type = inType;
            color = inColor;
            rot = inRot;
            locus = inLocus;
        }
    }

    /* Private Functions */

    // gets information from appropriate sources to fill InfoPack
    private InfoPack getUpdatedInfo()
    {
        EditGM.SelectedItem si = gm_ref.selectedItem;
        is_any_selected = si != new EditGM.SelectedItem();
        bool instance_null = si.instance == null;
        int updt_type = 0;
        int updt_color = 0;
        int updt_rot = 0;
        HexLocus updt_locus = new HexLocus();

        if (is_any_selected) {
            if (instance_null) { // <1>
                if (si.tileData.HasValue) {
                    updt_type = tc_ref.tileType;
                    updt_color = tc_ref.tileColor;
                    updt_rot = tc_ref.tileOrient.rotation;
                    updt_locus = tc_ref.tileOrient.locus;
                }
                if (si.chkpntData.HasValue) {
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = -1;
                    updt_locus = ct_ref.specOrient.locus;
                }
                if (si.warpData.HasValue) {
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = wt_ref.specOrient.rotation;
                    updt_locus = wt_ref.specOrient.locus;
                }
            } else { // <2>
                if (si.tileData.HasValue) {
                    TileData td = si.tileData.Value;
                    updt_type = td.type;
                    updt_color = td.color;
                    updt_rot = td.orient.rotation;
                    updt_locus = td.orient.locus;
                }
                if (si.chkpntData.HasValue) {
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = -1;
                    updt_locus = si.chkpntData.Value.locus;
                }
                if (si.warpData.HasValue) {
                    WarpData wd = si.warpData.Value;
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = wd.orient.rotation;
                    updt_locus = wd.orient.locus;
                }
            }
        }

        return new InfoPack(updt_type, updt_color, updt_rot, updt_locus);

        /*
        <1> if instance is null, gather info from currently active tool
        <2> if instance is non-null, gather info directly from object data
        */
    }
}
