using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelectControl : MonoBehaviour
{
    // private variables
    private int _activeColor;
    private EditGM _gmRef;
    private RectTransform _rtRef;
    private Quaternion _rotationTarget;
    private Quaternion _rotationOrigin;
    private float _startTime;
    private TileCreator _tcRef;

    void Start()
    {
        _gmRef = EditGM.instance;
        _tcRef = _gmRef.tileCreator;
        _rtRef = transform.GetChild(0).GetComponent<RectTransform>();
        _activeColor = 0;
        _startTime = 0f;

        WireColorButtons();

        // bump scale for default color
        _rtRef.transform.GetChild(_activeColor).localScale = Vector3.one * 1.5f;
    }

    void Update()
    {
        int newColor = _tcRef.tileColor;
        // whenever the tileCreator changes color, update target
        if (_activeColor != newColor)
        {
            // the current target has its scale reset to one
            _rtRef.transform.GetChild(_activeColor).localScale = Vector3.one;

            // start time for transition effect is logged
            _startTime = Time.time;
            _rotationOrigin = _rtRef.transform.rotation;
            // target rotations are simply increments of 45 degrees
            _rotationTarget = Quaternion.Euler(new Vector3(0, 0, -45f * newColor));

            _activeColor = newColor;
            // the new target has its scale bumped up 20%
            _rtRef.transform.GetChild(_activeColor).localScale = Vector3.one * 1.5f;
        }

        float t = Time.time - _startTime;
        // transitions are capped at 1 second in length
        if (t < 1f)
        {
            Quaternion q = Quaternion.RotateTowards(_rotationOrigin, _rotationTarget, 180 * t);
            // rotation for this frame is calculated and applied
            _rtRef.transform.rotation = q;
        }
    }

    private void WireColorButtons()
    {
        for (int i = 0; i < _rtRef.childCount; i++)
        {
            int colorIndex = i;
            GameObject colorObject = _rtRef.GetChild(i).gameObject;
            WireButton(colorObject, colorIndex);

            Button[] childButtons = colorObject.GetComponentsInChildren<Button>(true);
            for (int j = 0; j < childButtons.Length; j++)
            {
                if (childButtons[j] == null || childButtons[j].gameObject == colorObject)
                    continue;

                WireButton(childButtons[j].gameObject, colorIndex);
            }
        }
    }

    private void WireButton(GameObject buttonObject, int colorIndex)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = buttonObject.GetComponent<Image>();
        }

        button.onClick.AddListener(() => _gmRef.HandleHudColorPressed(colorIndex));
    }
}
