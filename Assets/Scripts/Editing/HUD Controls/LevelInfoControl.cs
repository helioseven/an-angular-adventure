using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class LevelInfoControl : MonoBehaviour {

    // private constants
    private const int NAME_CID = 0;
    private const int ATTR_CID = 1;
    private const int LAYER_CID = 1;
    private const int TILES_CID = 3;
    private const int ANCHR_CID = 5;

    // private references
    private EditGM gm_ref;
    private Transform tm_ref;
    private Text name_display;
    private EditableField name_field;
    private Text layers_display;
    private Text tiles_display;
    private Text anchor_display;

    // private variables
    private string level_name;
    private int active_layer;
    private int layer_count;
    private int layer_tiles;
    private int level_tiles;
    private HexLocus anchor_locus;

    void Awake ()
    {
        level_name = "";
        active_layer = 0;
        layer_count = 1;
        layer_tiles = 0;
        level_tiles = 0;
        anchor_locus = new HexLocus();
    }

    void Start ()
    {
        gm_ref = EditGM.instance;
        tm_ref = gm_ref.tileMap.transform;

        Transform t = transform.GetChild(NAME_CID);
        name_display = t.GetComponent<Text>();
        name_field = t.GetComponent<EditableField>();
        t = transform.GetChild(ATTR_CID);
        layers_display = t.GetChild(LAYER_CID).GetComponent<Text>();
        tiles_display = t.GetChild(TILES_CID).GetComponent<Text>();
        anchor_display = t.GetChild(ANCHR_CID).GetComponent<Text>();
    }

    void Update ()
    {
        if (checkAllFields()) updateUI();
    }

    /* Public Functions */

    // updates the text variables inside the relevant UI sub-elements
    public void updateUI ()
    {
        name_display.text = level_name;

        string s = (active_layer + 1).ToString() + " / " + layer_count.ToString();
        layers_display.text = s;

        s = layer_tiles.ToString() + " (" + level_tiles.ToString() + ")";
        tiles_display.text = s;

        anchor_display.text = anchor_locus.PrettyPrint();
    }

    /* Private Functions */

    // checks all panel fields for any changes and returns true if found
    private bool checkAllFields ()
    {
        bool b = false;

        string s = name_field.isActive ? "" : gm_ref.levelName; // <1>
        if (level_name != s) {
            level_name = s;
            b = true;
        }
        int al = gm_ref.activeLayer;
        if (active_layer != al) {
            active_layer = al;
            b = true;
        }
        if (layer_count != tm_ref.childCount) {
            layer_count = tm_ref.childCount;
            b = true;
        }
        if (layer_tiles != tm_ref.GetChild(al).childCount) {
            layer_tiles = tm_ref.GetChild(al).childCount;
            b = true;
        }
        if (level_tiles != getTileCount()) {
            level_tiles = getTileCount();
            b = true;
        }
        if (anchor_locus != gm_ref.anchorIcon.anchor) {
            anchor_locus = gm_ref.anchorIcon.anchor;
            b = true;
        }

        return b;

        /*
        <1> text is hidden while input prompt is active by replacement with ""
        */
    }

    // gets a count of all tiles currently in the level
    private int getTileCount ()
    {
        int count = 0;
        foreach (Transform layer in tm_ref) count += layer.childCount;
        return count;
    }
}
