using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelectControl : MonoBehaviour
{
    // private variables
    private int _activeColor;
    private RectTransform _rtRef;
    private Quaternion _rotationTarget;
    private Quaternion _rotationOrigin;
    private float _startTime;
    private TileCreator _tcRef;

    void Start()
    {
        _tcRef = EditGM.instance.tileCreator;
        _rtRef = transform.GetChild(0).GetComponent<RectTransform>();
        _activeColor = 0;
        _startTime = 0f;

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
}
