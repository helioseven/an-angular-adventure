using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;
using System.IO;
using System;

public class Tile_Green : Tile
{

    public GameObject particlePrefab; // Assign a glowing particle prefab in Inspector
    private Tile lockedTile; // Assign the corresponding locked Tile
    private Transform keyIcon;
    private Boolean hasUnlocked = false;

    void Start()
    {
        foreach (Tile tile in gameObject.transform.parent.GetComponentsInChildren<Tile>())
        {
            bool isGreen = tile.data.color != (int)TileColor.Green;
            bool isCorrespondingSpecialNumber = tile.data.special == gameObject.GetComponent<Tile>().data.special;
            if (isGreen && isCorrespondingSpecialNumber)
            {
                // set as locked block
                lockedTile = tile;
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
            if (!hasUnlocked && lockedTile.isActiveAndEnabled)
            {
                // update the state of the tile to note that is has been unlocked
                hasUnlocked = true;

                keyIcon.gameObject.SetActive(false);

                // show the unlock trail effect
                StartCoroutine(MoveEffectToLockedTile());
            }

        }
    }

    /* Private Functions */

    private void openDoor()
    {
        // TODO: flash locked tile before disappearing
        lockedTile.gameObject.SetActive(false);
    }

    IEnumerator MoveEffectToLockedTile()
    {
        // Spawn the particle effect at the key tile's position
        Vector3 startPos = keyIcon.position;
        Vector3 endPos = lockedTile.transform.GetChild(0).GetChild(0).position;
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
        openDoor();

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
