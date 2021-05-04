using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Purple : Tile {

  void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Player") {
      float volume = gm_ref.ImpactIntensityToVolume(other.relativeVelocity, Physics2D.gravity);
      FindObjectOfType<SoundManager>().Play("bounce", volume);
    }
  }
}
