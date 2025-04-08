using System.Collections.Generic;
using UnityEngine;

public class JumpProximityZone : MonoBehaviour
{
    private HashSet<Collider2D> nearbyPurpleTiles = new();

    public bool IsNearPurple => nearbyPurpleTiles.Count > 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("[JumpProximityZone] [OnTriggerEnter2D] other.name: " + other.name);

        // Debug.Log(
        //     "[JumpProximityZone] [OnTriggerEnter2D] other.name.Contains(\"Purple\"): "
        //         + other.name.Contains("Purple")
        // );

        // Debug.Log(
        //     "[JumpProximityZone] [OnTriggerEnter2D] other.gameObject.layer: "
        //         + other.gameObject.layer
        // );

        // Debug.Log("[JumpProximityZone] [OnTriggerEnter2D] gameObject.layer " + gameObject.layer);
        if (other.name.Contains("Purple") && other.gameObject.layer == gameObject.layer)
        {
            // Debug.Log("[JumpProximityZone] [OnTriggerEnter2D] nearbyPurpleTiles.Add(other); ");
            nearbyPurpleTiles.Add(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.name.Contains("Purple") && other.gameObject.layer == gameObject.layer)
        {
            // NOTE: should these also be removed on taking a warp?

            // remove from hashset
            nearbyPurpleTiles.Remove(other);

            // remove from player controller
            gameObject
                .GetComponentInParent<Player_Controller>()
                .recentlyTouchedPurpleTiles.Remove(other);

            // This logs when we decide to cancel superjump
            //   (when we enter the expetent purple incoming state but the purple never comes before leaving the area of the player purple collider)
            // if (
            //     gameObject.GetComponentInParent<Player_Controller>().queueSuperJumpOnPurpleTouch
            //     == true
            // )
            // {
            //     Debug.Log("[JumpProximityZone] [OnTriggerExit2D] CANCELLING---superjump---");
            // }
            gameObject
                .GetComponentInParent<Player_Controller>()
                .queueSuperJumpOnPurpleTouch = false;
        }
    }
}
