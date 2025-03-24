using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // public variables
    public TileData data;

    // protected references
    protected PlayGM _gmRef;

    void Awake()
    {
        _gmRef = PlayGM.instance;
    }
}
