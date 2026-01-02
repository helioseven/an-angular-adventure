using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_Green : Tile
{
    //public references
    // inspector-assigned glowing particle prefab
    public GameObject particlePrefab;

    // inspector-assigned burst prefab on door unlock
    public GameObject doorBurstPrefab;

    // private consts
    private const int KEY_CHILD_INDEX = 2;
    private const float DOOR_FADE_DURATION = 0.2f;

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

        Tile myTile = gameObject.GetComponent<Tile>();
        int myKeyId = myTile.data.special;

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
                    if (tileComp == null)
                        continue;
                    bool isCorrespondingSpecialNumber = tileComp.data.doorID == myKeyId;
                    bool isSelf = tileComp == myTile;

                    if (isCorrespondingSpecialNumber && !isSelf)
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
        StartCoroutine(FadeAndOpenDoor(doorTile));
    }

    IEnumerator MoveEffectToLockedTile(Tile doorTile)
    {
        // Spawn the particle effect at the key tile's position
        Vector3 startPos = _keyIcon.position;
        SpriteRenderer doorRenderer = doorTile.GetComponentInChildren<SpriteRenderer>();
        Vector3 endPos = doorRenderer ? doorRenderer.bounds.center : doorTile.transform.position;
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

    private IEnumerator FadeAndOpenDoor(Tile doorTile)
    {
        SpriteRenderer doorRenderer = doorTile.GetComponentInChildren<SpriteRenderer>();
        Vector3 burstPos = doorRenderer ? doorRenderer.bounds.center : doorTile.transform.position;
        string burstName =
            doorRenderer && doorRenderer.sprite ? doorRenderer.sprite.name : doorTile.name;
        Color doorColor = GetColorFromName(burstName);

        if (doorBurstPrefab != null)
        {
            GameObject burst = Instantiate(doorBurstPrefab, burstPos, Quaternion.identity);
            ParticleSystem[] burstSystems = burst.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem burstSystem in burstSystems)
            {
                ParticleSystem.MainModule main = burstSystem.main;
                main.startColor = doorColor;
            }
        }

        if (doorRenderer != null)
        {
            Color startColor = doorRenderer.color;
            float elapsed = 0f;

            while (elapsed < DOOR_FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DOOR_FADE_DURATION);
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                doorRenderer.color = c;
                yield return null;
            }

            doorTile.gameObject.SetActive(false);
            doorRenderer.color = startColor;
            SoundManager.instance.Play("door");
            yield break;
        }

        SoundManager.instance.Play("door");
        doorTile.gameObject.SetActive(false);
    }

    private static Color GetColorFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Color.white;

        string lower = name.ToLowerInvariant();
        if (lower.Contains("black"))
            return Color.black;
        if (lower.Contains("blue"))
            return Color.blue;
        if (lower.Contains("brown"))
            return new Color(0.45f, 0.3f, 0.2f, 1f);
        if (lower.Contains("green"))
            return Color.green;
        if (lower.Contains("orange"))
            return new Color(1f, 0.5f, 0.1f, 1f);
        if (lower.Contains("purple"))
            return new Color(0.6f, 0.3f, 0.8f, 1f);
        if (lower.Contains("red"))
            return Color.red;
        if (lower.Contains("white"))
            return Color.white;

        return Color.white;
    }
}
