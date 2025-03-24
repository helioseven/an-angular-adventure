using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Boundary : MonoBehaviour
{
    // public variables
    public bool isPositive;
    public bool isVertical;

    // private references
    private PlayGM _gmRef;

    void Awake()
    {
        _gmRef = PlayGM.instance;
    }

    void Update()
    {
        Vector3 v3 = transform.position;
        v3.z = _gmRef.GetLayerDepth();
        transform.position = v3;
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // delete white tiles by color
        Tile tile = other.gameObject.GetComponent<Tile>();
        if ((tile != null) && (tile.data.color == (int)TileColor.White))
            tile.gameObject.SetActive(false);
    }

    /* Public Functions */

    // after level is built, find appropriate boundary positions
    public void SetBoundary()
    {
        float max = 0f;
        foreach (Transform layer in _gmRef.tileMap.transform)
        {
            foreach (Transform tile in layer)
            {
                // test each tile against current known max extents
                Vector2 v2 = tile.GetChild(0).position;
                float f = 0f;
                if (isVertical)
                    f = v2.y * (isPositive ? 1 : -1);
                else
                    f = v2.x * (isPositive ? 1 : -1);

                // write new max extent if found
                if (f > max)
                    max = f;
            }
        }

        // set each boundary 20 units beyond appropriate max extent
        max += 20f;
        Vector2 pos = transform.position;
        if (isVertical)
            pos.y = max * (isPositive ? 1 : -1);
        else
            pos.x = max * (isPositive ? 1 : -1);
        transform.position = pos;
    }
}
