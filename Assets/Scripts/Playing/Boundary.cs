using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Boundary : MonoBehaviour
{
    // public variables
    public bool isPositive;
    public bool isVertical;
    public float boundaryPadding = 8f;
    public float boundarySpanPadding = 0.25f;

    // private references
    private PlayGM _gmRef;

    void Awake()
    {
        _gmRef = PlayGM.instance;
    }

    void Update()
    {
        Vector3 v3 = transform.position;
        v3.z = _gmRef.GetLayerDepth();
        transform.position = v3;
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // delete white tiles by color
        Tile tile = other.gameObject.GetComponent<Tile>();
        if ((tile != null) && (tile.data.color == TileColor.White))
            tile.gameObject.SetActive(false);
    }

    /* Public Functions */

    // after level is built, find appropriate boundary positions
    public void SetBoundary()
    {
        float minX = 0f;
        float maxX = 0f;
        float minY = 0f;
        float maxY = 0f;
        bool hasTiles = false;
        foreach (Transform layer in _gmRef.tileMap.transform)
        {
            foreach (Transform tile in layer)
            {
                if (!TryGetTileBounds(tile, out Bounds bounds))
                    continue;

                if (!hasTiles)
                {
                    minX = bounds.min.x;
                    maxX = bounds.max.x;
                    minY = bounds.min.y;
                    maxY = bounds.max.y;
                    hasTiles = true;
                }
                else
                {
                    if (bounds.min.x < minX)
                        minX = bounds.min.x;
                    if (bounds.max.x > maxX)
                        maxX = bounds.max.x;
                    if (bounds.min.y < minY)
                        minY = bounds.min.y;
                    if (bounds.max.y > maxY)
                        maxY = bounds.max.y;
                }
            }
        }

        if (!hasTiles)
            return;

        float edge = isVertical ? (isPositive ? maxY : minY) : (isPositive ? maxX : minX);
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        Vector2 pos = transform.position;
        if (isVertical)
        {
            pos.y = edge + (isPositive ? boundaryPadding : -boundaryPadding);
            pos.x = centerX;
        }
        else
        {
            pos.x = edge + (isPositive ? boundaryPadding : -boundaryPadding);
            pos.y = centerY;
        }
        transform.position = pos;
    }

    public void SetBoundarySpanFromPeers()
    {
        if (_gmRef == null)
            return;

        Boundary left = _gmRef.boundaryLeft;
        Boundary right = _gmRef.boundaryRight;
        Boundary up = _gmRef.boundaryUp;
        Boundary down = _gmRef.boundaryDown;
        if (left == null || right == null || up == null || down == null)
            return;

        float span = isVertical
            ? (right.transform.position.x - left.transform.position.x)
            : (up.transform.position.y - down.transform.position.y);
        span = Mathf.Max(1f, span + boundarySpanPadding * 2f);

        Vector3 scale = transform.localScale;
        if (isVertical)
            scale.x = span;
        else
            scale.y = span;
        transform.localScale = scale;
    }

    private bool TryGetTileBounds(Transform tile, out Bounds bounds)
    {
        Collider2D[] colliders = tile.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            return true;
        }

        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        bounds = default;
        return false;
    }
}
