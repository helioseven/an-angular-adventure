using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Boundary : MonoBehaviour {

  public bool isPositive;
  public bool isVertical;

  private PlayGM gm_ref;

  void Awake ()
  {
    gm_ref = PlayGM.instance;
  }

  void Update ()
  {
    Vector3 v3 = transform.position;
    v3.z = gm_ref.GetLayerDepth();
    transform.position = v3;
  }

    void OnCollisionEnter2D(Collision2D other)
    {
        // delete white tiles by color
        Tile tile = other.gameObject.GetComponent<Tile>();
        if (tile != null && tile.data.color == (int)TileColor.White)
        {
            Destroy(tile.gameObject);
        }
    }

  // after level is built, find appropriate boundary positions
  public void SetBoundary()
  {
    float max = 0f;
    foreach (Transform layer in gm_ref.tileMap.transform) {
      foreach (Transform tile in layer) {
        Vector2 v2 = tile.GetChild(0).position;
        float f = 0f;
        if (isVertical) f = v2.y * (isPositive ? 1 : -1);
        else f = v2.x * (isPositive ? 1 : -1);

        if (f > max) max = f;
      }
    }

    max += 20f;
    Vector2 pos = transform.position;
    if (isVertical) pos.y = max * (isPositive ? 1 : -1);
    else pos.x = max * (isPositive ? 1 : -1);
    transform.position = pos;
  }
}
