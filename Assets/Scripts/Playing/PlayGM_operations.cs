using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM {

	/* Public Operations */

	//
	public void DirectGravity (GravityDirection inDirect)
	{
		//
		switch (inDirect) {
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

		grav_dir = inDirect;
		player.ResetJumpForce();
	}

	// kills the player
	public void KillPlayer ()
	{
		player.gameObject.SetActive(false);
		Vector3 p = player.transform.position;
		UnityEngine.Object dp = Instantiate(deathParticles, p, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1f);
		Destroy(dp, 1.0f);
	}

	// updates last-touched checkpoint
	public void SetCheckpoint (ChkpntData inCheckpoint)
	{
		activeChkpnt = inCheckpoint;
	}

	// resets the player to last checkpoint
	public void ResetToCheckpoint ()
	{
		Vector3 v3 = activeChkpnt.locus.ToUnitySpace();
		int l = activeChkpnt.layer;
		v3.z = 2f * l;
		activateLayer(l);
		player.transform.position = v3;
		player.gameObject.SetActive(true);
	}

	// warps player from either base or target layer
	public void WarpPlayer (int baseLayer, int targetLayer)
	{
		int next_layer;
		if (activeLayer == baseLayer) // <1>
			next_layer = targetLayer;
		else if (activeLayer == targetLayer)
			next_layer = baseLayer;
		else {
			next_layer = activeLayer; // <2>
			return;
		}

		activateLayer(next_layer); // <3>

		Vector3 p = player.transform.position;
		p.z = tileMap.transform.GetChild(next_layer).position.z;
		player.transform.position = p; // <4>

		activeLayer = next_layer; // <5>

		/*
		<1> if activeLayer matches either base or target, select the other
		<2> if neither, stay at current level and break
		<3> update physics & transparency for all layers
		<4> change player's position
		<5> lastly, update activeLayer
		*/
	}
}
