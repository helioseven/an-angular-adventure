using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death_Trigger : MonoBehaviour
{
    // private references
    private PlayGM _playGM;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private Boundary _boundary;

    void Awake()
    {
        _playGM = PlayGM.instance;
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _boundary = GetComponent<Boundary>();
    }

    /* Override Functions */

    // triggers player's death when it detects player collision
    void OnTriggerEnter2D(Collider2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
        {
            if (IsInKillZone(other))
            {
                _playGM.KillPlayer();
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (IsInKillZone(other))
            {
                _playGM.KillPlayer();
            }
        }
    }

    private bool IsInKillZone(Collider2D other)
    {
        if (_boundary == null || _collider == null)
        {
            Bounds bounds = _spriteRenderer != null ? _spriteRenderer.bounds : _collider.bounds;
            float cutoff = bounds.center.y;
            return other.bounds.center.y <= cutoff;
        }

        Vector2 dir = GetBoundaryOutwardDirection();
        Vector2 boundaryCenter = _collider.bounds.center;
        Vector2 otherPoint = GetOutermostPoint(other.bounds, dir);
        float signedDist = Vector2.Dot(otherPoint - boundaryCenter, dir);
        return signedDist > 0f;
    }

    private Vector2 GetBoundaryOutwardDirection()
    {
        if (_boundary.isVertical)
        {
            return _boundary.isPositive ? Vector2.up : Vector2.down;
        }

        return _boundary.isPositive ? Vector2.right : Vector2.left;
    }

    private Vector2 GetOutermostPoint(Bounds bounds, Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0f
                ? new Vector2(bounds.max.x, bounds.center.y)
                : new Vector2(bounds.min.x, bounds.center.y);
        }

        return dir.y > 0f
            ? new Vector2(bounds.center.x, bounds.max.y)
            : new Vector2(bounds.center.x, bounds.min.y);
    }
}
