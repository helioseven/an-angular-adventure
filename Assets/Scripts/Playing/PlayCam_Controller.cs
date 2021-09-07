using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCam_Controller : MonoBehaviour
{
    // private references
    private GameObject _player;

    // private variables
    private Vector3 _velocity = Vector3.zero;

    void Start ()
    {
        _player = GameObject.FindWithTag("Player");
        _velocity = _player.transform.position;
    }

    // uses SmoothDamp to move camera towards the player at all times
    void Update ()
    {
        Vector3 target = _player.transform.position;
        target.z = target.z - 8f;
        Vector3 v3 = transform.position;

        v3 = Vector3.SmoothDamp(v3, target, ref _velocity, 0.3f);
        transform.position = v3;
    }
}
