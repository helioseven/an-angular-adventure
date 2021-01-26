using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM {

	/* Public Operations */

	// kills the player
	public void KillPlayer ()
	{
		player.SetActive(false);
		Vector3 p = player.transform.position;
		UnityEngine.Object dp = Instantiate(deathParticles, p, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1f);
		Destroy(dp, 1.0f);
	}

	// updates last-touched checkpoint
	public void SetCheckpoint (GameObject checkpoint)
	{
		currentCheckpoint = checkpoint;
	}

	// resets the player to last checkpoint
	public void ResetToCheckpoint ()
	{
		player.transform.position = currentCheckpoint.transform.position;
		player.SetActive(true);
	}

	// warps player from base to target layer
	public void WarpPlayer (Warp warp)
	{
		int next_layer;
		if (currentLayer == warp.baseLayer) // <1>
			next_layer = warp.targetLayer;
		else if (currentLayer == warp.targetLayer)
			next_layer = warp.baseLayer;
		else {
			next_layer = currentLayer; // <2>
			return;
		}

		activateLayer(next_layer); // <3>

		Vector3 p = player.transform.position;
		p.z = tileMap.transform.GetChild(next_layer).position.z;
		player.transform.position = p; // <4>

		currentLayer = next_layer; // <5>

		/*
		<1> if currentLayer matches either base or target, select the other
		<2> if neither, stay at current level and break
		<3> update physics & transparency for all layers
		<4> change player's position
		<5> lastly, update currentLayer
		*/
	}
}