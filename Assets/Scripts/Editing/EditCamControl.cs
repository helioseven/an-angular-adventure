using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using InputKeys = EditGM.InputKeys;

public class EditCamControl : MonoBehaviour {

    // private variables
    private InputKeys _camInputs;
    private InputKeys _keyMask;
    private EditGM _gmRef;

    void Start ()
    {
        _gmRef = EditGM.instance;
        _keyMask = (InputKeys.Up | InputKeys.Left | InputKeys.Down | InputKeys.Right);
    }

    void Update ()
    {
        if (_gmRef.inputMode) return;

        // mask identifying the keys relevant to the camera control (WASD)
        _camInputs = _gmRef.getInputs;
        _camInputs &= _keyMask;

        // uses the isolated _camInputs to modify a temporary position variable
        Vector3 v3 = transform.position;
        if ((_camInputs & InputKeys.Up) == InputKeys.Up)
            v3.y += (5.0f * Time.deltaTime);
        if ((_camInputs & InputKeys.Left) == InputKeys.Left)
            v3.x -= (5.0f * Time.deltaTime);
        if ((_camInputs & InputKeys.Down) == InputKeys.Down)
            v3.y -= (5.0f * Time.deltaTime);
        if ((_camInputs & InputKeys.Right) == InputKeys.Right)
            v3.x += (5.0f * Time.deltaTime);

        //  get active layer depth and set temp position back 8 units from it
        v3.z = _gmRef.GetLayerDepth() - 8f;
        transform.position = v3;
    }
}
