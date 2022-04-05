using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using circleXsquares;

public class LevelInfoControl : MonoBehaviour {

    /* Private Constants */

    private const int ANCHR_CID = 9;
    private const int ATTR_CID = 1;
    private const int CURR_LAYER_CID = 1;
    private const int LAYER_TILES_CID = 5;
    private const int NAME_CID = 0;
    private const int TOTAL_LAYER_CID = 3;
    private const int TOTAL_TILES_CID = 7;

    /* Private References */

    private Text _anchorDisplay;
    private Text _currentLayerDisplay;
    private EditGM _gmRef;
    private Text _layerTilesDisplay;
    private Text _nameDisplay;
    private LevelNameField _nameField;
    private Transform _tmRef;
    private Text _totalLayersDisplay;
    private Text _totalTilesDisplay;

    /* Private Variables */

    private int _activeLayer;
    private HexLocus _anchorLocus;
    private int _layerCount;
    private int _layerTiles;
    private string _levelName;
    private int _levelTiles;

    void Awake ()
    {
        _levelName = "";
        _activeLayer = 0;
        _layerCount = 1;
        _layerTiles = 0;
        _levelTiles = 0;
        _anchorLocus = new HexLocus();
    }

    void Start ()
    {
        _gmRef = EditGM.instance;
        _tmRef = _gmRef.tileMap.transform;

        Transform t = transform.GetChild(NAME_CID);
        _nameDisplay = t.GetComponent<Text>();
        _nameField = t.GetComponent<LevelNameField>();
        t = transform.GetChild(ATTR_CID);
        _currentLayerDisplay = t.GetChild(CURR_LAYER_CID).GetComponent<Text>();
        _totalLayersDisplay = t.GetChild(TOTAL_LAYER_CID).GetComponent<Text>();
        _layerTilesDisplay = t.GetChild(LAYER_TILES_CID).GetComponent<Text>();
        _totalTilesDisplay = t.GetChild(TOTAL_TILES_CID).GetComponent<Text>();
        _anchorDisplay = t.GetChild(ANCHR_CID).GetComponent<Text>();
    }

    void Update ()
    {
        if (checkAllFields()) updateUI();
    }

    /* Public Functions */

    // updates the text variables inside the relevant UI sub-elements
    public void updateUI ()
    {
        _nameDisplay.text = _levelName;

        _currentLayerDisplay.text = (_activeLayer + 1).ToString();
        _totalLayersDisplay.text = _layerCount.ToString();
        _layerTilesDisplay.text = _layerTiles.ToString();
        _totalTilesDisplay.text = _levelTiles.ToString();
        _anchorDisplay.text = _anchorLocus.PrettyPrint();
    }

    /* Private Functions */

    // checks all panel fields for any changes and returns true if found
    private bool checkAllFields ()
    {
        bool b = false;

        // text is hidden while input prompt is active by replacement with ""
        string s = _nameField.isActive ? "" : _gmRef.levelName;
        if (_levelName != s) {
            _levelName = s;
            b = true;
        }
        int al = _gmRef.activeLayer;
        if (_activeLayer != al) {
            _activeLayer = al;
            b = true;
        }
        if (_layerCount != _tmRef.childCount) {
            _layerCount = _tmRef.childCount;
            b = true;
        }
        if (_layerTiles != _tmRef.GetChild(al).childCount) {
            _layerTiles = _tmRef.GetChild(al).childCount;
            b = true;
        }
        if (_levelTiles != getTileCount()) {
            _levelTiles = getTileCount();
            b = true;
        }
        if (_anchorLocus != _gmRef.anchorIcon.anchor) {
            _anchorLocus = _gmRef.anchorIcon.anchor;
            b = true;
        }

        return b;
    }

    // gets a count of all tiles currently in the level
    private int getTileCount ()
    {
        int count = 0;
        foreach (Transform layer in _tmRef)
            count += layer.childCount;
        return count;
    }
}
