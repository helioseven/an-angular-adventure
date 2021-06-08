using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteControl : MonoBehaviour {

    // private variables
    private RectTransform _canvasRT;
    private Vector2 _localPosition;
    private RectTransform _localRT;
    private Camera _mainCam;

    public void Awake () {
        _mainCam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        _localPosition = Vector2.zero;

        Canvas c = GetComponentInParent<Canvas>();
        if (c) {
            _canvasRT = c.transform as RectTransform;
            _localRT = transform as RectTransform;
        } else {
            // this panel will not initialize properly if not the child of a canvas
            Debug.LogError("Failed to find the canvas.");
            _canvasRT = new RectTransform();
            _localRT = new RectTransform();
        }

        gameObject.SetActive(false);
    }

    /* Public Functions */

    // activates the panel and places it at the given location
    public void TogglePalette () {
        if (gameObject.activeSelf)
            // if the panel is already active, deactivate it
            gameObject.SetActive(false);
        else {
            // otherwise mouse input is translated to local rect space
            Vector2 lp, mP = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, mP, _mainCam, out lp);
            _localPosition = lp;
            // panel is then moved to the translated position and activated
            _localRT.localPosition = _localPosition;
            gameObject.SetActive(true);
        }
    }
}
