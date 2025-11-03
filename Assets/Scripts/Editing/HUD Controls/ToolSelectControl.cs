using circleXsquares;
using UnityEngine;
using UnityEngine.UI;
using static EditGM;

public class ToolSelectControl : MonoBehaviour
{
    // private variables
    private int _activeColor;
    private int _highlightedTool;
    private int _activeTile;
    private EditGM _gmRef;
    private EditCreatorTool _gmTool;
    private bool _isActive;
    private TileCreator _tcRef;

    void Start()
    {
        _gmRef = EditGM.instance;
        _tcRef = _gmRef.tileCreator;

        _gmTool = EditCreatorTool.Tile;
        _isActive = true;
        _highlightedTool = 0;
        _activeColor = 0;
    }

    void Update()
    {
        // first update currently active tool
        _gmTool = _gmRef.currentCreatorTool;

        if (_gmTool == EditCreatorTool.Tile)
        {
            // if the current tool is TileCreator, selected should == tileType
            if (_highlightedTool != _tcRef.tileType)
                updateHighlightedTool(_tcRef.tileType);
            if (_activeColor != _tcRef.tileColor)
                updateTileColors();
        }
        else if (_gmTool == EditCreatorTool.Eraser)
        {
            // stub, eraser not implemented yet
        }
        else
        {
            updateSpecialTool();
        }

        // after all other state variables updated, activate or not
        if (_isActive == _gmRef.isEditorInEditMode)
            updateActive();
    }

    /* Private Functions */

    // updates active state for current selected
    private void updateActive()
    {
        _isActive = !_isActive;
        // the active selected is only turned on if _isActive
        transform.GetChild(_activeTile).GetComponent<Image>().enabled = _isActive;
    }

    // updates active state for old and new selected
    private void updateHighlightedTool(int inSelected)
    {
        // turn off the image renderer for the previous selected
        transform.GetChild(_highlightedTool).GetComponent<Image>().enabled = false;
        // turn on the image renderer for the now selected
        transform.GetChild(inSelected).GetComponent<Image>().enabled = true;
        // update selected
        _highlightedTool = inSelected;
    }

    // updates selected based on which currently active tool
    private void updateSpecialTool()
    {
        if (_gmTool == EditCreatorTool.Checkpoint)
            updateHighlightedTool(6);
        else if (_gmTool == EditCreatorTool.Victory)
            updateHighlightedTool(7);
        else if (_gmTool == EditCreatorTool.Warp)
            updateHighlightedTool(8);
    }

    // updates the color of each selected's tile
    private void updateTileColors()
    {
        _activeColor = _tcRef.tileColor;

        for (int i = 0; i < Constants.NUM_SHAPES; i++)
        {
            Transform selected = transform.GetChild(i);
            Transform t = _tcRef.transform.GetChild(i).GetChild(_activeColor).GetChild(0);

            Sprite newSprite = t.GetComponent<SpriteRenderer>().sprite;
            selected.GetChild(0).GetChild(0).GetComponent<Image>().sprite = newSprite;
        }
    }
}
