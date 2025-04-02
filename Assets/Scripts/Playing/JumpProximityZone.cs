using System.Collections.Generic;
using UnityEngine;

public class JumpProximityZone : MonoBehaviour
{
    private HashSet<Collider2D> nearbyPurpleTiles = new();

    public bool IsNearPurple => nearbyPurpleTiles.Count > 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[JumpProximityZone] [OnTriggerEnter2D] other.name: " + other.name);

        Debug.Log(
            "[JumpProximityZone] [OnTriggerEnter2D] other.name.Contains(\"Purple\"): "
                + other.name.Contains("Purple")
        );
        if (other.name.Contains("Purple"))
        {
            nearbyPurpleTiles.Add(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.name.Contains("Purple"))
        {
            nearbyPurpleTiles.Remove(other);
        }
    }
}
