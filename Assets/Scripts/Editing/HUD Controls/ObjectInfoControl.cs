using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class ObjectInfoControl : MonoBehaviour {

    // public references
    public EditGM gmRef;
    public TileCreator tcRef;
    public SpecialCreator ctRef;
    public SpecialCreator wtRef;

    // private variables
    private Text _colorDisplay;
    private Text _locusDisplay;
    private Image _objectDisplay;
    private AspectRatioFitter _objectDisplayARF;
    private Text _rotationDisplay;
    private Text _specialLabel;
    private Text _specialDisplay;
    private Text _typeDisplay;

    private bool _isAnySelected;
    private int _objRotation;
    private HexLocus _objPosition;
    private int _tileColor;
    private int _tileSpecial;
    private int _tileType;

    private float[] _aspectRatios;
    private string[] _colorStrings;
    private string[] _typeStrings;

    void Awake ()
    {
        _isAnySelected = false;
        _tileType = 0;
        _tileColor = 0;
        _tileSpecial = 0;
        _objRotation = 0;
        _objPosition = new HexLocus();
        _aspectRatios = new float[] {1f, 2f, 2f, 1f, 1f, 2f};
        _typeStrings = new string[] {
            "Triangle",
            "Diamond",
            "Trapezoid",
            "Hexagon",
            "Square",
            "Wedge"
        };
        _colorStrings = new string[] {
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
        _objectDisplay = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        _objectDisplayARF = _objectDisplay.GetComponent<AspectRatioFitter>();

        Transform t = transform.GetChild(1);
        _typeDisplay = t.GetChild(1).GetComponent<Text>();
        _colorDisplay = t.GetChild(3).GetComponent<Text>();
        _rotationDisplay = t.GetChild(5).GetComponent<Text>();
        _locusDisplay = t.GetChild(7).GetComponent<Text>();

        t = transform.GetChild(2);
        _specialLabel = t.GetChild(0).GetComponent<Text>();
        _specialDisplay = t.GetChild(1).GetComponent<Text>();
    }

    void Update ()
    {
        bool b = false;
        InfoPack ip = getUpdatedInfo();

        if (_tileType != ip.type) {
            _tileType = ip.type;
            b = true;
        }
        if (_tileColor != ip.color) {
            _tileColor = ip.color;
            b = true;
        }
        if (_objRotation != ip.rot) {
            _objRotation = ip.rot;
            b = true;
        }
        if (_objPosition != ip.locus) {
            _objPosition = ip.locus;
            b = true;
        }

        if (b)
            UpdateUI();
    }

    /* Public Functions */

    // updates the display image
    public void UpdateUI ()
    {
        // set sprite and aspect ratio for object image, activate if appropriate
        Transform t = tcRef.transform.GetChild(_tileType).GetChild(_tileColor).GetChild(0);
        _objectDisplay.sprite = t.GetComponent<SpriteRenderer>().sprite;
        _objectDisplayARF.aspectRatio = _aspectRatios[_tileType];
        _objectDisplay.gameObject.SetActive(_isAnySelected);

        // set text strings as appropriate
        bool b = !_isAnySelected || _tileType == -1;
        string s = b ? "[N/A]" : _typeStrings[_tileType];
        _typeDisplay.text = s;
        b = !_isAnySelected || _tileColor == -1;
        s = b ? "[N/A]" : _colorStrings[_tileColor];
        _colorDisplay.text = s;
        b = !_isAnySelected || _objRotation == -1;
        s = b ? "[N/A]" : _objRotation.ToString();
        _rotationDisplay.text = s;
        s = !_isAnySelected ? "[N/A]" : _objPosition.PrettyPrint();
        _locusDisplay.text = s;

        // set special dropdown values, activate if appropriate
        string sp = "Special Value:";
        b = false;
        if (_tileColor == 3) {
            sp = "Switch Target:";
            b = true;
        }
        if (_tileColor == 4) {
            sp = "Gravity Target:";
            b = true;
        }
        _specialLabel.text = sp;
        _specialDisplay.text = _tileSpecial.ToString();
        transform.GetChild(2).gameObject.SetActive(b);
    }

    /* Private Structs */

    // a struct for reporting what current information should be
    private struct InfoPack {
        public int type;
        public int color;
        public int rot;
        public HexLocus locus;

        public InfoPack (int inType, int inColor, int inRot, HexLocus inLocus)
        {
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
        EditGM.SelectedItem si = gmRef.selectedItem;
        _isAnySelected = si != new EditGM.SelectedItem();
        bool instance_null = si.instance == null;
        int updt_type = 0;
        int updt_color = 0;
        int updt_rot = 0;
        HexLocus updt_locus = new HexLocus();

        if (_isAnySelected) {
            // if instance is null, gather info from currently active tool
            if (instance_null) {
                if (si.tileData.HasValue) {
                    updt_type = tcRef.tileType;
                    updt_color = tcRef.tileColor;
                    updt_rot = tcRef.tileOrient.rotation;
                    updt_locus = tcRef.tileOrient.locus;
                }
                if (si.chkpntData.HasValue) {
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = -1;
                    updt_locus = ctRef.specOrient.locus;
                }
                if (si.warpData.HasValue) {
                    updt_type = -1;
                    updt_color = -1;
                    updt_rot = wtRef.specOrient.rotation;
                    updt_locus = wtRef.specOrient.locus;
                }
            // if instance is non-null, gather info directly from object data
            } else {
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
    }
}
