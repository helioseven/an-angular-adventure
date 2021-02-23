using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile : MonoBehaviour
{

  public TileData data;

  private PlayGM gm_ref;

  void Awake ()
  {
      gm_ref = PlayGM.instance;
  }
}
