using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_Green : Tile
{
    public GameObject particlePrefab; // Assign a glowing particle prefab in Inspector
    private List<Tile> connectedDoorTiles = new List<Tile>(); // To be filled with list of corresponding locked "door tiles"
    private Transform keyIcon;
    private bool hasUnlocked = false;

    void Start()
    {
        // get the grandparent tilemap
        Transform tileMap = transform.parent?.parent;

        if (tileMap != null)
        {
            foreach (Transform layer in tileMap)
            {
                foreach (Transform otherTile in layer)
                {
                    // check for each tile to add to list of connected door tiles
                    Tile tileComp = otherTile.GetComponent<Tile>();
                    bool isCorrespondingSpecialNumber =
                        tileComp.data.doorId == gameObject.GetComponent<Tile>().data.special;

                    if (tileComp != null && isCorrespondingSpecialNumber)
                    {
                        // add to list of connected door tiles
                        connectedDoorTiles.Add(tileComp);
                    }
                }
            }
        }

        keyIcon = transform.GetChild(0).GetChild(0);
        Vector3 rotation = Vector3.forward;
        keyIcon.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // triggers door open on player collision
        if (other.gameObject.CompareTag("Player"))
        {
            if (!hasUnlocked)
            {
                SoundManager.instance.Play("key");

                // update the state of the tile to note that is has been unlocked
                hasUnlocked = true;

                keyIcon.gameObject.SetActive(false);

                foreach (Tile doorTile in connectedDoorTiles)
                {
                    // show the unlock trail effect
                    StartCoroutine(MoveEffectToLockedTile(doorTile));
                }
            }
        }
    }

    /* Private Functions */

    private void openDoor(Tile doorTile)
    {
        // TODO: flash locked tile before disappearing
        SoundManager.instance.Play("door");
        doorTile.gameObject.SetActive(false);
    }

    IEnumerator MoveEffectToLockedTile(Tile doorTile)
    {
        // Spawn the particle effect at the key tile's position
        Vector3 startPos = keyIcon.position;
        Vector3 endPos = doorTile.transform.GetChild(0).GetChild(0).position;
        GameObject effect = Instantiate(particlePrefab, startPos, Quaternion.identity);

        // Get the ParticleSystem component
        ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();

        float duration = 0.3f;
        float elapsedTime = 0f;

        // Move the effect towards the locked tile over time
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration; // Normalized time (0 to 1)
            effect.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null; // Wait for next frame
        }

        // remove the tile as soon as the trail gets there
        openDoor(doorTile);

        // Stop particle emission but allow existing particles to fade out
        if (particleSystem != null)
        {
            particleSystem.Stop(); // Stops new particles from spawning
            yield return new WaitForSeconds(particleSystem.main.startLifetime.constantMax); // Wait for remaining particles to disappear
        }

        // Remove the effect after it reaches the locked tile and grace period is over
        Destroy(effect);
    }
}
