using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class TileEditGreen : MonoBehaviour
{
    private EditGM _editGM;
    [SerializeField]
    private Color _linkColor = new Color(0.25f, 0.9f, 0.25f, 0.9f);
    [SerializeField]
    private float _linkWidth = 0.05f;
    [SerializeField]
    private float _inactiveAlpha = 0.2f;
    [SerializeField]
    private float _activeAlpha = 1f;

    private class DoorLink
    {
        public GameObject target;
        public LineRenderer line;
    }

    private readonly List<DoorLink> _doorLinks = new List<DoorLink>();
    private bool _isSelected;
    private bool _linksVisible = true;

    void Start()
    {
        _editGM = EditGM.instance;
        Subscribe();
        _linksVisible = _editGM != null ? _editGM.keyDoorLinksVisible : true;
        DrawLinesToAllTargets();
    }

    void OnDestroy()
    {
        if (_editGM != null)
        {
            _editGM.KeyDoorMappingChanged -= HandleMappingChanged;
            _editGM.KeyDoorVisibilityChanged -= HandleVisibilityChanged;
        }
        ClearLines();
    }

    void LateUpdate()
    {
        // keep lines aligned if tiles move while editing
        foreach (DoorLink link in _doorLinks)
            UpdateLinePositions(link);

        bool currentlySelected =
            _editGM != null && _editGM.selectedItem.instance != null && _editGM.selectedItem.instance == gameObject;
        if (currentlySelected != _isSelected)
        {
            _isSelected = currentlySelected;
            UpdateLinkColors();
        }

        UpdateLinkVisibility();
    }

    public void DrawLinesToAllTargets()
    {
        if (!_editGM)
            _editGM = EditGM.instance;
        if (!_editGM)
            return;

        ClearLines();

        int keyID = _editGM.GetGreenTileKey(gameObject);
        if (keyID == 0)
            return;

        _editGM.GetDoorSet(keyID, out HashSet<GameObject> targets);

        foreach (GameObject go in targets)
        {
            if (!go)
                continue;
            DoorLink link = new DoorLink { target = go, line = CreateLineRenderer() };
            _doorLinks.Add(link);
            UpdateLinePositions(link);
        }

        UpdateLinkVisibility();
        UpdateLinkColors();
    }

    private void Subscribe()
    {
        if (_editGM != null)
        {
            _editGM.KeyDoorMappingChanged += HandleMappingChanged;
            _editGM.KeyDoorVisibilityChanged += HandleVisibilityChanged;
        }
    }

    private void HandleMappingChanged()
    {
        DrawLinesToAllTargets();
    }

    private void HandleVisibilityChanged(bool visible)
    {
        _linksVisible = visible;
        UpdateLinkVisibility();
    }

    private LineRenderer CreateLineRenderer()
    {
        GameObject go = new GameObject("KeyDoorLink");
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = _linkWidth;
        lr.endWidth = _linkWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = WithAlpha(_linkColor, _inactiveAlpha);
        lr.endColor = WithAlpha(_linkColor, _inactiveAlpha);
        lr.sortingOrder = 20;
        return lr;
    }

    private void ClearLines()
    {
        foreach (DoorLink link in _doorLinks)
        {
            if (link != null && link.line != null)
                Destroy(link.line.gameObject);
        }
        _doorLinks.Clear();
    }

    private void UpdateLinePositions(DoorLink link)
    {
        if (link == null || link.line == null || link.target == null)
            return;

        Vector3 start = GetTileCenter(gameObject);
        Vector3 end = GetTileCenter(link.target);

        link.line.SetPosition(0, start);
        link.line.SetPosition(1, end);
    }

    private void UpdateLinkColors()
    {
        float targetAlpha = _isSelected ? _activeAlpha : _inactiveAlpha;
        Color color = WithAlpha(_linkColor, targetAlpha);

        foreach (DoorLink link in _doorLinks)
        {
            if (link == null || link.line == null)
                continue;
            link.line.startColor = color;
            link.line.endColor = color;
        }
    }

    private void UpdateLinkVisibility()
    {
        foreach (DoorLink link in _doorLinks)
        {
            if (link == null || link.line == null)
                continue;
            link.line.enabled = _linksVisible;
        }
    }

    private Color WithAlpha(Color baseColor, float alpha)
    {
        return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }

    private Vector3 GetTileCenter(GameObject tile)
    {
        if (!tile)
            return Vector3.zero;

        SpriteRenderer sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.center;

        return tile.transform.position;
    }
}
