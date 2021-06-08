using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Green : Tile
{
    void Start()
    {
        Transform key = transform.GetChild(0).GetChild(0);
        Vector3 rotation = Vector3.forward;
        key.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // triggers door open on player collision
        if (other.gameObject.CompareTag("Player")) openDoor();
    }

    /* Private Functions */

    private void openDoor()
    {
        foreach (Tile t in gameObject.transform.parent.GetComponentsInChildren<Tile>()) {
            bool b1 = t.data.color != (int)TileColor.Green;
            bool b2 = t.data.special == gameObject.GetComponent<Tile>().data.special;
            if (b1 && b2)
                t.gameObject.SetActive(false);
        }
    }
}
