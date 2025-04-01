using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Orange : Tile
{
    // public references
    public Transform arrow;

    void Start()
    {
        arrow = transform.GetChild(0).GetChild(0);

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
        PlayGM.GravityDirection gd;
        switch (newDir)
        {
            case 0:
                gd = PlayGM.GravityDirection.Down;
                break;
            case 1:
                gd = PlayGM.GravityDirection.Left;
                break;
            case 2:
                gd = PlayGM.GravityDirection.Up;
                break;
            case 3:
                gd = PlayGM.GravityDirection.Right;
                break;
            default:
                return;
        }

        _gmRef.DirectGravity(gd);
    }
}
