using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Green : Tile
{
    public Transform arrow;
    void Start()
    {
        arrow = transform.GetChild(0).GetChild(0);
        Vector3 rotation = Vector3.forward;
        arrow.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }

    // triggers door open on player collision
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player")) openDoor();
    }

    private void openDoor()
    {
        // Debug.Log("green trigger special");
        // Debug.Log(gameObject.GetComponent<Tile>().data.special);


        foreach (Tile t in gameObject.transform.parent.GetComponentsInChildren<Tile>())
        {
            if (t.data.color != (int)TileColor.Green && t.data.special == gameObject.GetComponent<Tile>().data.special)
            {
                t.gameObject.SetActive(false);
            }
        }

    }
}
