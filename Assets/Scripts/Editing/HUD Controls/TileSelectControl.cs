using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileSelectControl : MonoBehaviour {

    // private variables
    private int _activeColor;
    private int _activeTile;
    private EditGM _gmRef;
    private bool _isActive;
    private TileCreator _tcRef;

    void Start ()
    {
        _gmRef = EditGM.instance;
        _tcRef = _gmRef.tileCreator;
        _isActive = true;
        _activeTile = 0;
        _activeColor = 0;
    }

    void Update ()
    {
        if (_activeTile != _tcRef.tileType) updateType();
        if (_activeColor != _tcRef.tileColor) updateColor();
        if (_isActive == _gmRef.editMode) updateActive();
    }

    /* Private Functions */

    // updates active state for current selector
    private void updateActive ()
    {
        _isActive = !_isActive;
        // the active selector is only turned on if _isActive
        transform.GetChild(_activeTile).GetComponent<Image>().enabled = _isActive;
    }

    // updates which selector is active
    private void updateType ()
    {
        // turn off the image renderer for the previous selector
        transform.GetChild(_activeTile).GetComponent<Image>().enabled = false;
        _activeTile = _tcRef.tileType;
        // turn on the image renderer for the newly active selector
        if (_isActive)
            transform.GetChild(_activeTile).GetComponent<Image>().enabled = true;
    }

    // updates the color of each selector's tile
    private void updateColor ()
    {
        int newColor = _tcRef.tileColor;
        foreach (Transform selector in transform) {
            // gets the appropriate transform in the tileCreator hierarchy
            Transform t = _tcRef.transform.GetChild(selector.GetSiblingIndex()).GetChild(newColor).GetChild(0);
            // gets the sprite from that that transform
            Sprite newSprite = t.GetComponent<SpriteRenderer>().sprite;
            // assigns that sprite to the appropriate selector
            selector.GetChild(0).GetChild(0).GetComponent<Image>().sprite = newSprite;
        }
    }
}
