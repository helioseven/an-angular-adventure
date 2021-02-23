using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boundary : MonoBehaviour {

  public bool isPositive;
  public bool isVertical;

  private PlayGM gm_ref;

  void Awake()
  {
    gm_ref = PlayGM.instance;
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
