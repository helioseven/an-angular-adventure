﻿using System;
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
    private bool _isAnySelected;
    private bool _isInstanceNull;
    private InfoPack _lastFrameInfoPack;
    private Text _locusDisplay;
    private Image _objectDisplay;
    private AspectRatioFitter _objectDisplayARF;
    private Text _rotationDisplay;
    private Text _specialLabel;
    private Text _specialDisplay;
    private Text _typeDisplay;

    private readonly float[] _aspectRatios = new float[] {1f, 2f, 2f, 1f, 1f, 2f};
    private readonly string[] _colorStrings = new string[] {
        "Black",
        "Blue",
        "Brown",
        "Green",
        "Orange",
        "Purple",
        "Red",
        "White"
    };
    private readonly string[] _typeStrings = new string[] {
        "Triangle",
        "Diamond",
        "Trapezoid",
        "Hexagon",
        "Square",
        "Wedge",
        "Warp",
        "Checkpoint"
    };

    void Awake ()
    {
        _isAnySelected = false;
        _lastFrameInfoPack = new InfoPack();

        transform.GetChild(2).gameObject.SetActive(false);
    }

    void Start ()
    {
        gmRef = EditGM.instance;
        tcRef = gmRef.tileCreator;
        ctRef = gmRef.chkpntTool.GetComponent<SpecialCreator>();
        wtRef = gmRef.warpTool.GetComponent<SpecialCreator>();

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
        InfoPack ip = getUpdatedInfo();

        if (_lastFrameInfoPack != ip)
            updateUI(ip);

        _lastFrameInfoPack = ip;
    }

    /* Private Structs */

    // a struct for reporting what current display information should be
    private struct InfoPack {
        public int type;
        public int color;
        public int spec;
        public int rot;
        public HexLocus locus;

        public InfoPack (int inType, int inColor, int inSpec, int inRot, HexLocus inLocus)
        {
            type = inType;
            color = inColor;
            spec = inSpec;
            rot = inRot;
            locus = inLocus;
        }

        public static bool operator ==(InfoPack ip1, InfoPack ip2)
        {
            if (ip1.type != ip2.type) return false;
            if (ip1.color != ip2.color) return false;
            if (ip1.spec != ip2.spec) return false;
            if (ip1.rot != ip2.rot) return false;
            if (ip1.locus != ip2.locus) return false;
            return true;
        }

        public static bool operator !=(InfoPack ip1, InfoPack ip2) { return !(ip1 == ip2); }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            InfoPack? inIP = obj as InfoPack?;
            if (!inIP.HasValue)
                return false;
            else
                return this == inIP.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /* Private Functions */

    // gets information from appropriate sources to fill InfoPack
    private InfoPack getUpdatedInfo()
    {
        EditGM.SelectedItem si = gmRef.selectedItem;
        _isAnySelected = si != EditGM.SelectedItem.noSelection;
        _isInstanceNull = si.instance == null;
        int updt_type = 0;
        int updt_color = 0;
        int updt_spec = 0;
        int updt_rot = 0;
        HexLocus updt_locus = new HexLocus();

        if (_isAnySelected) {
            if (si.tileData.HasValue) {
                if (_isInstanceNull) {
                    // if instance is null, gather info from currently active tool
                    updt_type = tcRef.tileType;
                    updt_color = tcRef.tileColor;
                    updt_spec = tcRef.tileSpecial;
                    updt_rot = tcRef.tileOrient.rotation;
                    updt_locus = tcRef.tileOrient.locus;
                } else {
                    // if instance is non-null, gather info from object data
                    TileData td = si.tileData.Value;
                    updt_type = td.type;
                    updt_color = td.color;
                    updt_spec = td.special;
                    updt_rot = td.orient.rotation;
                    updt_locus = td.orient.locus;
                }
            }
            if (si.chkpntData.HasValue) {
                updt_type = -1;
                updt_color = -1;
                updt_spec = -1;
                updt_rot = -1;
                if (_isInstanceNull)
                    // if instance is null, gather info from currently active tool
                    updt_locus = ctRef.specOrient.locus;
                else
                    // if instance is non-null, gather info from object data
                    updt_locus = si.chkpntData.Value.locus;
            }
            if (si.warpData.HasValue) {
                WarpData wd = si.warpData.Value;
                updt_type = -2;
                updt_color = -1;
                updt_spec = -1;
                if (_isInstanceNull) {
                    // if instance is null, gather info from currently active tool
                    updt_rot = wtRef.specOrient.rotation;
                    updt_locus = wtRef.specOrient.locus;
                } else {
                    // if instance is non-null, gather info from object data
                    updt_rot = wd.orient.rotation;
                    updt_locus = wd.orient.locus;
                }
            }
        }

        return new InfoPack(updt_type, updt_color, updt_spec, updt_rot, updt_locus);
    }

    // updates the display image
    private void updateUI (InfoPack inIP)
    {
        if (_isAnySelected) {
            // set sprite source transform and aspect ratio for object image
            Transform t = tcRef.transform;
            if (inIP.type >= 0) {
                t = tcRef.transform.GetChild(inIP.type);
                t = t.GetChild(inIP.color).GetChild(0);
                _objectDisplayARF.aspectRatio = _aspectRatios[inIP.type];
            }
            if (inIP.type == -1) {
                t = ctRef.transform.GetChild(0);
                _objectDisplayARF.aspectRatio = 1f;
            }
            if (inIP.type == -2) {
                t = wtRef.transform.GetChild(1);
                _objectDisplayARF.aspectRatio = 1f;
            }

            // check for sprite, activate display as appropriate
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr) {
                _objectDisplay.sprite = sr.sprite;
                _objectDisplay.gameObject.SetActive(true);
            } else
                _objectDisplay.gameObject.SetActive(false);
        } else
            _objectDisplay.gameObject.SetActive(false);

        // set text strings as appropriate
        bool b = !_isAnySelected || inIP.type < 0;
        string s = b ? "[N/A]" : _typeStrings[inIP.type];
        _typeDisplay.text = s;
        b = !_isAnySelected || inIP.color < 0;
        s = b ? "[N/A]" : _colorStrings[inIP.color];
        _colorDisplay.text = s;
        b = !_isAnySelected || inIP.rot < 0;
        s = b ? "[N/A]" : inIP.rot.ToString();
        _rotationDisplay.text = s;
        s = !_isAnySelected ? "[N/A]" : inIP.locus.PrettyPrint();
        _locusDisplay.text = s;

        // set special dropdown values, activate if appropriate
        string sp = "Special Value:";
        b = false;
        if (inIP.color == 3) {
            sp = "Switch Target:";
            b = true;
        }
        if (inIP.color == 4) {
            sp = "Gravity Target:";
            b = true;
        }
        _specialLabel.text = sp;
        _specialDisplay.text = inIP.spec.ToString();
        transform.GetChild(2).gameObject.SetActive(b);
    }
}
