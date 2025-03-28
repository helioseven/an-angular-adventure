using System;
using System.Collections;
using circleXsquares;
using UnityEngine;
using static EditGM;

public class SpecialCreator : MonoBehaviour
{
    // public read-accessibility state variables
    public HexOrient specOrient { get; private set; }

    // public variables
    public EditTools toolType = EditTools.Warp;

    // private variables
    private EditGM _gmRef;
    private SnapCursor _anchorRef;

    void Start()
    {
        // reference to EditGM gives all information needed
        _gmRef = EditGM.instance;
        _anchorRef = _gmRef.anchorIcon;

        specOrient = new HexOrient();
        gameObject.SetActive(false);
    }

    void Update()
    {
        // when active, the special will follow the focus
        HexLocus f = _anchorRef.focus;
        int r = toolType == EditTools.Warp ? specOrient.rotation : 0;
        int l = _gmRef.activeLayer;
        specOrient = new HexOrient(f, r, l);

        Quaternion q;
        transform.position = specOrient.ToUnitySpace(out q);
        transform.rotation = q;
    }

    /* Public Functions */

    // turns the transform in 30 degree increments
    public void SetRotation(int inRotation)
    {
        if (!(toolType == EditTools.Warp))
            return;
        specOrient = new HexOrient(specOrient.locus, inRotation, specOrient.layer);
        Update();
    }

    // translates and rotates the transform according to given orientation
    public void SetOrientation(HexOrient inOrient)
    {
        if (!(toolType == EditTools.Warp))
            inOrient.rotation = 0;
        specOrient = inOrient;
        Update();
    }
}
