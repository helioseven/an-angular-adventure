﻿using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Warp : MonoBehaviour
{
    // public read-accessibility state variables
    public int baseLayer
    {
        get { return data.orient.layer; }
        set { }
    }
    public int targetLayer
    {
        get { return data.targetLayer; }
        set { }
    }

    // public variables
    public WarpData data;

    // private references
    private PlayGM _gmRef;

    [SerializeField]
    private GameObject warpRipple;

    void Awake()
    {
        _gmRef = PlayGM.instance;
        warpRipple = transform.Find("WarpRipple")?.gameObject;
    }

    /* Override Functions */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _gmRef.soundManager.Play("warp");
            _gmRef.WarpPlayer(baseLayer, targetLayer);

            warpRipple.transform.position = other.transform.position;

            if (warpRipple != null)
            {
                warpRipple.SetActive(false); // Reset in case it was left on
                warpRipple.SetActive(true); // Triggers OnEnable and the ripple animation
            }
        }
    }
}
