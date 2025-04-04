using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using circleXsquares;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectInfoControl : MonoBehaviour
{
    /* Private References */
    private EditGM _editGM;
    private TileCreator _tileCreator;
    private SpecialCreator _checkpointCreator;
    private SpecialCreator _warpCreator;
    private SpecialCreator _victoryCreator;
    private TMP_Text _colorDisplay;
    private InfoPack _lastFrameInfoPack;
    private TMP_Text _locusDisplay;
    private Image _objectDisplay;
    private AspectRatioFitter _objectDisplayARF;
    private TMP_Text _rotationDisplay;
    private TMP_Text _specialLabel;
    private TMP_Text _specialDisplay;
    private TMP_Text _doorIdLabel;
    private TMP_Text _doorIdDisplay;
    private TMP_Text _typeDisplay;
    private TMP_Text _combinedNameDisplay;

    /* Private Variables */

    private bool _isAnyItemSelected;
    private bool _isInstanceNull;

    private readonly float[] _aspectRatios = new float[] { 1f, 2f, 2f, 1f, 1f, 2f };
    private readonly string[] _colorStrings = new string[]
    {
        "Black",
        "Blue",
        "Brown",
        "Green",
        "Orange",
        "Purple",
        "Red",
        "White",
    };
    private readonly string[] _typeStrings = new string[]
    {
        "Triangle",
        "Diamond",
        "Trapezoid",
        "Hexagon",
        "Square",
        "Wedge",
        "Checkpoint",
        "Warp",
    };

    void Awake()
    {
        _isAnyItemSelected = false;
        _lastFrameInfoPack = new InfoPack();

        transform.GetChild(2).gameObject.SetActive(false);
    }

    void Start()
    {
        _editGM = EditGM.instance;
        _tileCreator = _editGM.tileCreator;
        _checkpointCreator = _editGM.checkpointTool.GetComponent<SpecialCreator>();
        _warpCreator = _editGM.warpTool.GetComponent<SpecialCreator>();
        _victoryCreator = _editGM.victoryTool.GetComponent<SpecialCreator>();

        _objectDisplay = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        _objectDisplayARF = _objectDisplay.GetComponent<AspectRatioFitter>();

        _combinedNameDisplay = transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();

        Transform t = transform.GetChild(1);
        _typeDisplay = t.GetChild(1).GetComponent<TMP_Text>();
        _colorDisplay = t.GetChild(3).GetComponent<TMP_Text>();
        _rotationDisplay = t.GetChild(5).GetComponent<TMP_Text>();
        _locusDisplay = t.GetChild(7).GetComponent<TMP_Text>();

        t = transform.GetChild(2);
        _specialLabel = t.GetChild(0).GetComponent<TMP_Text>();
        _specialDisplay = t.GetChild(1).GetComponent<TMP_Text>();

        t = transform.GetChild(3);
        _doorIdLabel = t.GetChild(0).GetComponent<TMP_Text>();
        _doorIdDisplay = t.GetChild(1).GetComponent<TMP_Text>();
    }

    void Update()
    {
        InfoPack infoPack = getUpdatedInfo();

        if (_lastFrameInfoPack != infoPack)
            updateUI(infoPack);

        _lastFrameInfoPack = infoPack;
    }

    /* Private Structs */

    // a struct for reporting what current display information should be
    private struct InfoPack
    {
        public int type;
        public int color;
        public int spec;
        public int rot;
        public HexLocus locus;
        public int doorId;

        public InfoPack(
            int inType,
            int inColor,
            int inSpec,
            int inRot,
            HexLocus inLocus,
            int inDoorId
        )
        {
            type = inType;
            color = inColor;
            spec = inSpec;
            rot = inRot;
            locus = inLocus;
            doorId = inDoorId;
        }

        public static bool operator ==(InfoPack ip1, InfoPack ip2)
        {
            return (
                (ip1.type == ip2.type)
                && (ip1.color == ip2.color)
                && (ip1.spec == ip2.spec)
                && (ip1.rot == ip2.rot)
                && (ip1.locus == ip2.locus)
                && (ip1.doorId == ip2.doorId)
            );
        }

        public static bool operator !=(InfoPack ip1, InfoPack ip2)
        {
            return !(ip1 == ip2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            InfoPack? infoPack = obj as InfoPack?;
            if (!infoPack.HasValue)
                return false;
            else
                return this == infoPack.Value;
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
        EditGM.SelectedItem selectedItem = _editGM.selectedItem;
        _isAnyItemSelected = selectedItem != EditGM.SelectedItem.noSelection;
        _isInstanceNull = selectedItem.instance == null;
        int type = 0;
        int color = 0;
        int spec = 0;
        int rot = 0;
        HexLocus locus = new HexLocus();
        int doorId = 0;

        // TODO: rewrite this whole function to use _currentTool based on creation vs seletion mode

        if (_isAnyItemSelected)
        {
            if (selectedItem.tileData.HasValue)
            {
                if (_isInstanceNull)
                {
                    // if instance is null, gather info from currently active tool
                    type = _tileCreator.tileType;
                    color = _tileCreator.tileColor;
                    spec = _tileCreator.tileSpecial;
                    rot = _tileCreator.tileOrient.rotation;
                    locus = _tileCreator.tileOrient.locus;
                    doorId = _tileCreator.tileDoorId;
                }
                else
                {
                    // if instance is non-null, gather info from object data
                    TileData td = selectedItem.tileData.Value;
                    type = td.type;
                    color = td.color;
                    spec = td.special;
                    rot = td.orient.rotation;
                    locus = td.orient.locus;
                    doorId = td.doorId;
                }
            }
            if (selectedItem.CheckpointData.HasValue)
            {
                type = 6;
                color = -1;
                spec = -1;
                rot = -1;
                doorId = 0;
                if (_isInstanceNull)
                    // if instance is null, gather info from currently active tool
                    locus = _checkpointCreator.specOrient.locus;
                else
                    // if instance is non-null, gather info from object data
                    locus = selectedItem.CheckpointData.Value.locus;
            }
            if (selectedItem.warpData.HasValue)
            {
                WarpData wd = selectedItem.warpData.Value;
                type = 7;
                color = -1;
                spec = -1;
                rot = 0;
                doorId = 0;
                if (_isInstanceNull)
                {
                    // if instance is null, gather info from currently active tool
                    rot = _warpCreator.specOrient.rotation;
                    locus = _warpCreator.specOrient.locus;
                }
                else
                {
                    // if instance is non-null, gather info from object data
                    locus = wd.locus;
                }
            }
            if (selectedItem.victoryData.HasValue)
            {
                VictoryData vd = selectedItem.victoryData.Value;
                type = 8;
                color = -1;
                spec = -1;
                doorId = 0;

                if (_isInstanceNull)
                    // if instance is null, gather info from currently active tool
                    locus = _victoryCreator.specOrient.locus;
                else
                    // if instance is non-null, gather info from object data
                    locus = vd.locus;
            }
        }

        // replace based on create tool (possibly a hack?)
        InfoPack infoPack = new InfoPack(type, color, spec, rot, locus, doorId);
        InfoPack infoPackReplaced = ReplaceInfoToDesiredItem(infoPack);

        return infoPackReplaced;
    }

    private InfoPack ReplaceInfoToDesiredItem(InfoPack infoPack)
    {
        EditGM.SelectedItem selectedItem = _editGM.selectedItem;
        _isAnyItemSelected = selectedItem != EditGM.SelectedItem.noSelection;
        _isInstanceNull = selectedItem.instance == null;
        int type = infoPack.type;
        int color = infoPack.color;
        int spec = infoPack.spec;
        int rot = infoPack.rot;
        HexLocus locus = infoPack.locus;
        int doorId = infoPack.doorId;
        if (_tileCreator.isActiveAndEnabled)
        {
            type = _tileCreator.tileType;
            color = _tileCreator.tileColor;
            spec = _tileCreator.tileSpecial;
            rot = _tileCreator.tileOrient.rotation;
            locus = _tileCreator.tileOrient.locus;
            doorId = _tileCreator.tileDoorId;
        }
        if (_checkpointCreator.isActiveAndEnabled)
        {
            type = 6;
        }
        if (_warpCreator.isActiveAndEnabled)
        {
            type = 7;
        }
        if (_victoryCreator.isActiveAndEnabled)
        {
            type = 8;
        }

        return new InfoPack(type, color, spec, rot, locus, doorId);
    }

    // updates the display image
    private void updateUI(InfoPack infoPack)
    {
        if (_isAnyItemSelected && infoPack.type >= 0)
        {
            // set sprite source transform and aspect ratio for object image
            Transform t = _tileCreator.transform;

            // Tiles
            if (infoPack.type <= 5)
            {
                t = _tileCreator.transform.GetChild(infoPack.type);
                t = t.GetChild(infoPack.color).GetChild(0);
                _objectDisplayARF.aspectRatio = _aspectRatios[infoPack.type];
            }

            // Specials
            if (infoPack.type == 6)
            {
                t = _checkpointCreator.transform.GetChild(0);
                _objectDisplayARF.aspectRatio = 1f;
            }
            if (infoPack.type == 7)
            {
                t = _warpCreator.transform.GetChild(1);
                _objectDisplayARF.aspectRatio = 1f;
            }
            if (infoPack.type == 8)
            {
                t = _victoryCreator.transform.GetChild(0);
                _objectDisplayARF.aspectRatio = 1f;
            }

            // check for sprite, activate display as appropriate
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr)
            {
                _objectDisplay.sprite = sr.sprite;
                _objectDisplay.gameObject.SetActive(true);
            }
            else
                _objectDisplay.gameObject.SetActive(false);
        }
        else
            _objectDisplay.gameObject.SetActive(false);

        /* Object Panel Text Setting */
        // General approach - if we have a selected tile and it has the associated property - set the text in the object viewing panel
        // Combined Name (use standard for tile types (0-5) and special switch for 6, 7, 8 (checkpoint, warp, and victory))
        if (_isAnyItemSelected && infoPack.type >= 0 && infoPack.color >= 0 && infoPack.type < 6)
            _combinedNameDisplay.text =
                _colorStrings[infoPack.color] + " " + _typeStrings[infoPack.type];
        if (infoPack.type >= 6)
        {
            Debug.Log("infoPack.type: " + infoPack.type);
            // special switch
            switch (infoPack.type)
            {
                case 6:
                    _combinedNameDisplay.text = "Checkpoint";
                    break;
                case 7:
                    _combinedNameDisplay.text = "Warp";
                    break;
                case 8:
                    _combinedNameDisplay.text = "Victory";
                    break;
                default:
                    _combinedNameDisplay.text = "Special Type";
                    break;
            }
        }

        // Type (Not displayed at the moment)
        if (_isAnyItemSelected && infoPack.type >= 0 && infoPack.type < 6)
            _typeDisplay.text = _typeStrings[infoPack.type];
        else
            _typeDisplay.text = "[N/A]";

        // Color (Not displayed at the moment)
        if (_isAnyItemSelected && infoPack.color >= 0)
            _colorDisplay.text = _colorStrings[infoPack.color];
        else
            _colorDisplay.text = "[N/A]";

        // Rotation
        if (_isAnyItemSelected && infoPack.rot >= 0)
            _rotationDisplay.text = infoPack.rot.ToString();
        else
            _rotationDisplay.text = "[N/A]";

        // Locus
        if (_isAnyItemSelected)
            _locusDisplay.text = infoPack.locus.PrettyPrint();
        else
            _locusDisplay.text = "[N/A]";

        // set special dropdown values, activate if appropriate
        // Green - Special Text
        if (infoPack.color == 3)
        {
            _specialLabel.text = "Key Id:";
            _specialDisplay.text = infoPack.spec.ToString();
        }

        // Orange - Special Text
        if (infoPack.color == 4)
        {
            _specialLabel.text = "Gravity Direction:";
            _specialDisplay.text = infoPack.spec.ToString();
        }

        // Door ID
        _doorIdDisplay.text = infoPack.doorId.ToString();

        // Only show special attributes for green and orange tiles
        transform.GetChild(2).gameObject.SetActive(infoPack.color == 3 || infoPack.color == 4);
    }
}
