using System;
using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class PlayGM
{
    /* Public Operations */

    // redirects gravity in the specified direction
    public void SetGravity(GravityDirection inDirect)
    {
        // if new gravity direction matches current, simply return
        if (inDirect == _gravDir)
            return;

        // update PlayGM instance state or gravity direction
        _gravDir = inDirect;

        // set gravity vector according to direction
        switch (inDirect)
        {
            case GravityDirection.Down:
                Physics2D.gravity = new Vector2(0.0f, -9.81f);
                break;
            case GravityDirection.Left:
                Physics2D.gravity = new Vector2(-9.81f, 0.0f);
                break;
            case GravityDirection.Up:
                Physics2D.gravity = new Vector2(0.0f, 9.81f);
                break;
            case GravityDirection.Right:
                Physics2D.gravity = new Vector2(9.81f, 0.0f);
                break;
            default:
                return;
        }

        // reset player's jump force
        _gravDir = inDirect;
        soundManager.Play("gravity");
        player.UpdateJumpForceVector(inDirect);
    }

    // kills the player
    public void KillPlayer()
    {
        // play the death sound
        soundManager.Play("death");

        // hide and reset the player
        player.gameObject.SetActive(false);
        Vector3 p = player.transform.position;
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        UnityEngine.Object dp = Instantiate(deathParticles, p, Quaternion.identity);

        // respawn at checkpoint
        StartCoroutine(ResetToCheckpoint(rb2d));
        Destroy(dp, 1.0f);
    }

    // updates last-touched checkpoint
    public void SetCheckpoint(GameObject checkpointGO)
    {
        // set active checkpoint variables
        activeCheckpoint = checkpointGO;
        activeCheckpointGravity = gravDirection;

        // update opacity for all checkpoints
        foreach (Transform checkpoint in checkpointMap.transform)
        {
            int layerNumber = checkpoint.gameObject.GetComponent<Checkpoint>().data.layer;
            int distance = Math.Abs(layerNumber - activeLayer);
            if (activeLayer > layerNumber)
                distance += 2;
            setCheckpointOpacity(checkpoint, distance);
        }
    }

    // updates last-touched checkpoint (just the data)
    public void SetCheckpointData(CheckpointData inCheckpoint)
    {
        activeCheckpointData = inCheckpoint;
    }

    // allows Victory prefabs to register victory with PlayGM
    public void RegisterVictory(Victory inVictory)
    {
        // skip this if we've already finished
        if (victoryAchieved)
        {
            return;
        }

        // Victory and it feels so good
        victoryAchieved = true;

        // Stop the clock
        _clock.PauseClock();
        // Set time for victory panel
        victoryTimeText.text = _clock.clockTimeToString();
        // Open the victory panel
        levelCompletePanel.GetComponent<LevelCompletePanel>().Show();

        ParticleSystem victoryBurst;
        Transform child = inVictory.transform.Find("VictoryBurst");
        if (child != null)
        {
            victoryBurst = child.GetComponent<ParticleSystem>();
            // Trigger the particle burst
            victoryBurst.Play();
        }
    }

    // resets the player to last checkpoint
    private IEnumerator ResetToCheckpoint(Rigidbody2D rb, bool isSpawn = false)
    {
        PlayCam_Controller camController = Camera.main
            ? Camera.main.GetComponent<PlayCam_Controller>()
            : null;
        if (camController != null)
            camController.SetZoomSuppressed(true);

        // pause to watch death particles
        yield return new WaitForSeconds(1f);

        // Freeze physics
        var rb2d = player.gameObject.GetComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        // reset gravity direction
        SetGravity(activeCheckpointGravity);

        // reset player
        Vector3 v3 = activeCheckpointData.locus.ToUnitySpace();
        int l = activeCheckpointData.layer;
        v3.z = 2f * l;
        activateLayer(l);
        player.transform.position = v3;

        string BurstComponentName = "CheckpointBurstReverse";
        if (isSpawn)
        {
            BurstComponentName = "CheckpointBurstSpawn";
        }

        // reverse burst before respawn
        ParticleSystem checkpointBurstReverse;
        Transform child = activeCheckpoint.transform.Find(BurstComponentName);
        if (child != null)
        {
            checkpointBurstReverse = child.GetComponent<ParticleSystem>();
            // Trigger the particle burst
            checkpointBurstReverse.Play();
        }

        // wait (for the burst animation)
        yield return new WaitForSeconds(0.5f);

        // Show the player visually by activating the game object
        player.gameObject.SetActive(true);

        // Show locked in visual Game Object
        Transform lockInTransform = activeCheckpoint.transform.Find("CheckpointLockedIn");
        if (lockInTransform != null)
        {
            GameObject lockInObject = lockInTransform.gameObject;
            lockInObject.SetActive(true);
        }

        // wait (for the freeze in time mechanic)
        yield return new WaitForSeconds(1f);

        // Remove locked in visual (Have fun!)
        if (lockInTransform != null)
        {
            GameObject lockInObject = lockInTransform.gameObject;
            lockInObject.SetActive(false);
        }

        // Unfreeze and restore momentum
        rb2d.bodyType = RigidbodyType2D.Dynamic;

        // Start the clock
        if (isSpawn)
        {
            _clock.StartClock();
        }

        if (camController != null)
            camController.SetZoomSuppressed(false);
    }

    void CheckForTelefrag(int playerLayer)
    {
        Vector2 playerPos = player.transform.position;
        float radius = 0.4f; // Match player size

        int layerMask = 1 << playerLayer;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, radius, layerMask);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.name.Contains("Player"))
                continue; // Ignore Self

            if (hit.isTrigger)
                continue; // Ignore triggers like checkpoints

            // Lethal collision detected
            Debug.Log("Telefragged by: " + hit.gameObject.name);
            KillPlayer();
            return;
        }
    }

    // warps player from either base or target layer with a short visual flow
    public void WarpPlayer(
        int baseLayer,
        int targetLayer,
        Vector3 entryPos,
        Vector3 targetCenter,
        float flowDuration
    )
    {
        StartCoroutine(
            WarpPlayerRoutine(baseLayer, targetLayer, entryPos, targetCenter, flowDuration)
        );
    }

    private IEnumerator WarpPlayerRoutine(
        int baseLayer,
        int targetLayer,
        Vector3 entryPos,
        Vector3 targetCenter,
        float flowDuration
    )
    {
        int currentLayer = activeLayer;
        int nextLayer = activeLayer == baseLayer ? targetLayer : baseLayer;

        float startZ = tileMap.transform.GetChild(currentLayer).position.z;
        float targetZ = tileMap.transform.GetChild(nextLayer).position.z;

        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        RigidbodyType2D prevBodyType = rb2d.bodyType;
        float prevAngularVelocity = rb2d.angularVelocity;

        // Freeze motion during flow
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.linearVelocity = Vector2.zero;
        rb2d.angularVelocity = 0f;

        // Swap to the target layer (updates physics masks and visuals)
        activateLayer(nextLayer);

        Vector3 start = entryPos;
        start.z = startZ;
        Vector3 target = targetCenter;
        target.z = targetZ;

        float elapsed = 0f;
        while (elapsed < flowDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flowDuration);
            player.transform.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        player.transform.position = target;

        // Restore motion, but without prior linear velocity to keep the landing stable
        rb2d.bodyType = prevBodyType;
        rb2d.linearVelocity = Vector2.zero;
        rb2d.angularVelocity = prevAngularVelocity;

        // Telefragging
        CheckForTelefrag(LayerMask.NameToLayer(INT_TO_NAME[activeLayer]));
    }
}
