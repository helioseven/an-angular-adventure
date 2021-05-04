using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Orange : Tile
{

    public Transform arrow;
    void Start()
    {
        arrow = transform.GetChild(0).GetChild(0);

        int direction = (data.special + 1) % 4;
        if (direction % 2 == 1) direction += 2;
        Vector3 rotation = Vector3.forward * (direction * 90);

        arrow.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }

    // triggers gravity redirection when it detects player collision
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        { // <1>
            gm_ref.soundManager.Play("gravity");
            redirectGravity();
        }

        /*
        <1> identifies the player by tag
        */
    }

    private void redirectGravity()
    {
        int newDir = data.special % 4;
        PlayGM.GravityDirection gd = PlayGM.GravityDirection.Down;
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

        gm_ref.DirectGravity(gd);
    }
}
