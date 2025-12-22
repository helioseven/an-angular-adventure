using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Orange : Tile
{
    // public references
    public Transform arrow;

    // private consts
    private const int ARROW_CHILD_INDEX = 2;

    void Start()
    {
        arrow = transform.GetChild(ARROW_CHILD_INDEX);

        int direction = (data.special + 1) % 4;
        if (direction % 2 == 1)
            direction += 2;
        Vector3 rotation = Vector3.forward * (direction * 90);

        arrow.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }

    /* Override Functions */

    // triggers gravity redirection when it detects player collision
    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
        {
            redirectGravity();
        }
    }

    /* Private Functions */

    // redirect gravity according to special value
    private void redirectGravity()
    {
        int newDir = data.special % 4;

        _gmRef.SetGravity((PlayGM.GravityDirection)newDir);
    }
}
