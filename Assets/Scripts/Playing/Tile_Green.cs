using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_Green : Tile
{
    //public references
    // inspector-assigned glowing particle prefab
    public GameObject particlePrefab;

    // private consts
    private const int KEY_CHILD_INDEX = 2;

    // private references
    // list of corresponding "door tiles"
    private List<Tile> _connectedDoorTiles = new List<Tile>();
    private Transform _keyIcon;

    // private variables
    private bool _hasUnlocked = false;

    void Start()
    {
        _keyIcon = transform.GetChild(KEY_CHILD_INDEX);
        Vector3 rotation = Vector3.forward;

        int myKeyId = gameObject.GetComponent<Tile>().data.special;

        // 0 (or negative) is special - represents no key for this lock
        if (myKeyId <= 0)
        {
            // hide by setting to inactive and skip looking for doors by returning
            _keyIcon.gameObject.SetActive(false);
            _hasUnlocked = true;
            return;
        }

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
                    bool isSelf = tileComp.Equals(gameObject.GetComponent<Tile>());

                    if (tileComp != null && isCorrespondingSpecialNumber && !isSelf)
                    {
                        // add to list of connected door tiles
                        _connectedDoorTiles.Add(tileComp);
                    }
                }
            }
        }
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // triggers door open on player collision
        if (other.gameObject.CompareTag("Player"))
        {
            if (!_hasUnlocked)
            {
                SoundManager.instance.Play("key");

                // update the state of the tile to note that is has been unlocked
                _hasUnlocked = true;

                _keyIcon.gameObject.SetActive(false);

                foreach (Tile doorTile in _connectedDoorTiles)
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
        Vector3 startPos = _keyIcon.position;
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
