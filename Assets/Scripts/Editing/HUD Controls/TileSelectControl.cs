using circleXsquares;
using UnityEngine;
using UnityEngine.UI;
using EditCreatorTool = EditGM.EditCreatorTool;

public class TileSelectControl : MonoBehaviour
{
    private const string HighlightObjectName = "BG";
    private const string PreviewButtonObjectName = "Button";
    private const string PreviewImageObjectName = "Image";

    // private variables
    private int _activeColor;
    private int _activeSelected;
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
        _activeSelected = 0;
        _activeColor = 0;

        InitializeSelectorButtons();
    }

    void Update()
    {
        // first update currently active tool
        _gmTool = _gmRef.currentCreatorTool;

        if (_gmTool == EditCreatorTool.Tile)
        {
            // if the current tool is TileCreator, selected should == tileType
            if (_activeSelected != _tcRef.tileType)
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
        SetHighlightEnabled(_activeSelected, _isActive);
    }

    // updates active state for old and new selected
    private void updateHighlightedTool(int inSelected)
    {
        // turn off the image renderer for the previous selected
        SetHighlightEnabled(_activeSelected, false);
        // turn on the image renderer for the now selected
        SetHighlightEnabled(inSelected, true);
        // update selected
        _activeSelected = inSelected;
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

        int selectorCount = Mathf.Min(
            Constants.NUM_SHAPES,
            transform.childCount,
            _tcRef.transform.childCount
        );
        for (int i = 0; i < selectorCount; i++)
        {
            Transform selected = transform.GetChild(i);
            Transform tileType = _tcRef.transform.GetChild(i);
            if (_activeColor < 0 || _activeColor >= tileType.childCount)
                continue;

            Transform spriteRoot = tileType.GetChild(_activeColor);
            if (spriteRoot.childCount == 0)
                continue;

            SpriteRenderer spriteRenderer = spriteRoot.GetChild(0).GetComponent<SpriteRenderer>();
            Image previewImage = GetSelectorPreviewImage(selected);
            if (spriteRenderer == null || previewImage == null)
                continue;

            previewImage.sprite = spriteRenderer.sprite;
        }
    }

    private void InitializeSelectorButtons()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int selectorIndex = i;
            GameObject selector = transform.GetChild(i).gameObject;
            WireButton(selector, selectorIndex);

            Button[] childButtons = selector.GetComponentsInChildren<Button>(true);
            for (int j = 0; j < childButtons.Length; j++)
            {
                if (childButtons[j] == null || childButtons[j].gameObject == selector)
                    continue;

                WireButton(childButtons[j].gameObject, selectorIndex);
            }
        }
    }

    private void WireButton(GameObject buttonObject, int selectorIndex)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = buttonObject.GetComponent<Image>();
        }

        button.onClick.AddListener(() => _gmRef.HandleHUDSelectorPressed(selectorIndex));
    }

    private void SetHighlightEnabled(int selectorIndex, bool isEnabled)
    {
        GameObject highlightObject = GetHighlightObject(selectorIndex);
        if (highlightObject != null && highlightObject.activeSelf != isEnabled)
            highlightObject.SetActive(isEnabled);
    }

    private GameObject GetHighlightObject(int selectorIndex)
    {
        if (selectorIndex < 0 || selectorIndex >= transform.childCount)
            return null;

        Transform selector = transform.GetChild(selectorIndex);
        Transform highlight = selector.Find(HighlightObjectName);
        if (highlight != null)
            return highlight.gameObject;

        return null;
    }

    private Image GetSelectorPreviewImage(Transform selector)
    {
        if (selector == null)
            return null;

        Transform button = selector.Find(PreviewButtonObjectName);
        if (button == null)
            return null;

        Transform previewImage = button.Find(PreviewImageObjectName);
        return previewImage != null ? previewImage.GetComponent<Image>() : null;
    }
}
