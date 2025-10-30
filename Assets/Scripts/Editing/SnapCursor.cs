using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class SnapCursor : MonoBehaviour
{
    // public read-accessibility state variables
    public HexLocus anchor { get; private set; }
    public HexLocus focus { get; private set; }

    // private variables
    private float _depth;
    private Vector2 _focusShift;
    private EditGM _gmRef;
    private Plane _layerPlane;
    private TileCreator _tcRef;

    void Awake()
    {
        focus = new HexLocus();
        anchor = new HexLocus();
        _focusShift = new Vector2();
        _layerPlane = new Plane(Vector3.back, 0f);
        _depth = 0f;
    }

    void Start()
    {
        _gmRef = EditGM.instance;
        _tcRef = _gmRef.tileCreator;
    }

    void Update()
    {
        // if the tileCreator is active, an offset needs to be calculated
        Vector2 tileOffset = Vector2.zero;
        if (_tcRef.gameObject.activeSelf)
        {
            int tt = _tcRef.tileType;
            Transform tile = _tcRef.transform.GetChild(tt).GetChild(0);
            // tileOffset is the difference between sprite's and prefab's positions
            tileOffset = tile.GetChild(0).position - tile.position;
        }

        // _focusShift is this offset between anchor point and current mouse position
        Vector2 mouseIn = findPointOnPlane();
        _focusShift = mouseIn - tileOffset - anchor.ToUnitySpace();
        // focus is the nearest grid point to which the genesis_tile will snap
        focus = new HexLocus(_focusShift);
        focus += anchor;
    }

    /* Public Functions */

    // finds the closest snap point to the current mouse position and sets the anchor there
    public void FindNewAnchor()
    {
        // Make the sound
        _gmRef.soundManager.Play("anchor");

        // lists all collisions within radius 0.5 circle from mouse position
        Vector2 mouseIn = findPointOnPlane();
        Collider2D[] hitCols = Physics2D.OverlapCircleAll(mouseIn, 0.5f, 1);
        List<HexLocus> locusSnaps = new List<HexLocus>();

        foreach (Collider2D c2d in hitCols)
        {
            // check tile hits first by casting as PolygonCollider2D
            PolygonCollider2D pc2d = c2d as PolygonCollider2D;
            if (pc2d)
            {
                TileData tData;
                bool b = _gmRef.IsMappedTile(pc2d.gameObject, out tData);
                // if the collision is not from a tile in the map, it is skipped
                if (!b)
                    continue;
                // then check every vertex of of the collider's polygon
                HexLocus tHL = tData.orient.locus;
                foreach (Vector2 subPoint in pc2d.points)
                {
                    // translate each vertex from local space into world space
                    Vector2 v2 = c2d.transform.TransformPoint(subPoint);
                    HexLocus newPoint = new HexLocus(v2 - tHL.ToUnitySpace());
                    newPoint += tHL;
                    // adds translated vertex to list of possible snaps
                    locusSnaps.Add(newPoint);
                    tHL = newPoint;
                }

                // if our hit found a pc2d, it won't find a special
                continue;
            }

            // check for special hits next
            CircleCollider2D cc2d = c2d as CircleCollider2D;
            if (cc2d)
            {
                // gets special's locus and adds it to possible snaps
                CheckpointData cData;
                if (_gmRef.IsMappedCheckpoint(cc2d.gameObject, out cData))
                    locusSnaps.Add(cData.locus);
                WarpData wData;
                if (_gmRef.IsMappedWarp(cc2d.gameObject, out wData))
                    locusSnaps.Add(wData.locus);
                VictoryData vData;
                if (_gmRef.IsMappedVictory(cc2d.gameObject, out vData))
                    locusSnaps.Add(vData.locus);
            }
        }

        // finds the snap point with the smallest offset from original input position
        HexLocus newAnchor = new HexLocus();
        foreach (HexLocus hL in locusSnaps)
        {
            Vector2 newOffset = (Vector2)(hL.ToUnitySpace() - mouseIn);
            Vector2 oldOffset = (Vector2)(newAnchor.ToUnitySpace() - mouseIn);
            if (oldOffset.magnitude > newOffset.magnitude)
                newAnchor = hL;
        }

        // updates global variables
        anchor = newAnchor;
        Vector3 returnV3 = anchor.ToUnitySpace();
        returnV3.z = _depth;
        transform.position = returnV3;
    }

    /* Private Functions */

    // uses mouse position ray's intersection with current level plane to generate a 2D coordinate
    private Vector2 findPointOnPlane()
    {
        // set plane's distance from origin according to layer depth
        _depth = _gmRef.GetLayerDepth();
        _layerPlane.distance = _depth;

        float distance;
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!_layerPlane.Raycast(inputRay, out distance))
        {
            // if the raycast doesn't hit our plane, something is very wrong
            Debug.LogError("Screen click ray did not intersect with layer plane.");
            return new Vector2();
        }
        else
            // simply return the point along ray at distance from origin
            return inputRay.GetPoint(distance);
    }
}
