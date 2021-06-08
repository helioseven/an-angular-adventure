using UnityEngine;
using System;
using System.Collections;
using circleXsquares;

public class SpecialCreator : MonoBehaviour {

    // public read-accessibility state variables
    public HexOrient specOrient { get; private set; }

    // public variables
    public bool isWarp;

    // private variables
    private EditGM _gmRef;
    private SnapCursor _anchorRef;

    void Start ()
    {
        // reference to EditGM gives all information needed
        _gmRef = EditGM.instance;
        _anchorRef = _gmRef.anchorIcon;

        specOrient = new HexOrient();
    }

    void Update ()
    {
        // when active, the special will follow the focus
        HexLocus f = _anchorRef.focus;
        int r = isWarp ? specOrient.rotation : 0;
        int l = _gmRef.activeLayer;
        specOrient = new HexOrient(f, r, l);

        Quaternion q;
        transform.position = specOrient.ToUnitySpace(out q);
        transform.rotation = q;
    }

    /* Public Functions */

    // turns the transform in 30 degree increments
    public void SetRotation (int inRotation)
    {
        if (!isWarp)
            return;
        specOrient = new HexOrient(specOrient.locus, inRotation, specOrient.layer);
        Update();
    }

    // translates and rotates the transform according to given orientation
    public void SetOrientation (HexOrient inOrient)
    {
        if (!isWarp)
            inOrient.rotation = 0;
        specOrient = inOrient;
        Update();
    }
}
