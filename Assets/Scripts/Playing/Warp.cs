using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Warp : MonoBehaviour
{
    // public read-accessibility state variables
    public int baseLayer
    {
        get { return data.layer; }
        set { }
    }
    public int targetLayer
    {
        get { return data.targetLayer; }
        set { }
    }

    // public variables
    public WarpData data;

    // private references
    private PlayGM _gmRef;

    [SerializeField]
    private GameObject warpRipple;

    [Header("Warp Tuning")]
    [SerializeField]
    private float warpFlowDuration = 0.85f;

    [SerializeField]
    private float warpCooldown = 3f;

    [SerializeField]
    private int cooldownArcSegments = 48;

    [SerializeField]
    private float cooldownArcRadiusScale = 1.025f;

    // state
    private bool _coolingDown;
    private Collider2D _collider;
    private SpriteRenderer _overlay;
    private SpriteRenderer _overlayBack;
    private LineRenderer _cooldownArc;
    private Color _overlayBaseColor = Color.white;

    void Awake()
    {
        _gmRef = PlayGM.instance;
        warpRipple = transform.Find("WarpRipple")?.gameObject;
        _collider = GetComponent<Collider2D>();
        _overlay = transform.Find("WarpOverlay")?.GetComponent<SpriteRenderer>();
        _overlayBack = transform.Find("WarpOverlayBack")?.GetComponent<SpriteRenderer>();
        if (_overlay != null)
            _overlayBaseColor = _overlay.color;
    }

    /* Override Functions */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;
        if (_coolingDown)
            return;

        _gmRef.soundManager.Play("warp");

        Vector3 entryPos = other.gameObject.transform.position;
        int destLayer = _gmRef.activeLayer == baseLayer ? targetLayer : baseLayer;
        Vector3 warpCenter = GetTargetWarpCenter(destLayer);

        _gmRef.WarpPlayer(
            baseLayer,
            targetLayer,
            entryPos,
            new Vector3(warpCenter.x, warpCenter.y, entryPos.z),
            warpFlowDuration
        );

        if (warpRipple != null)
        {
            warpRipple.transform.position = new Vector3(
                warpCenter.x,
                warpCenter.y,
                _gmRef.player.gameObject.transform.position.z
            );
            warpRipple.SetActive(false); // Reset in case it was left on
            warpRipple.SetActive(true); // Triggers OnEnable and the ripple animation
        }

        Camera mainCam = Camera.main;
        PlayCam_Controller camController =
            mainCam != null ? mainCam.GetComponent<PlayCam_Controller>() : null;
        if (camController != null)
        {
            camController.PlayWarpTransition(warpCenter, warpFlowDuration);
        }

        StartCoroutine(RunCooldown());
    }

    private Vector3 GetTargetWarpCenter(int targetLayer)
    {
        Transform warpMap = _gmRef.warpMap != null ? _gmRef.warpMap.transform : null;
        if (warpMap != null)
        {
            foreach (Transform child in warpMap)
            {
                Warp w = child.GetComponent<Warp>();
                if (w == null)
                    continue;

                bool isCounterpart = w.data.layer == targetLayer && w.data.locus == data.locus;

                if (!isCounterpart)
                    continue;
                SpriteRenderer sr = child.Find("WarpOverlay")?.GetComponent<SpriteRenderer>();
                if (sr != null)
                    return sr.bounds.center;
                return child.position;
            }
        }

        // fallback: use this warp's center but project to target layer's z if available
        Vector3 fallback = _overlay != null ? _overlay.bounds.center : transform.position;
        Transform tileMap = _gmRef.tileMap != null ? _gmRef.tileMap.transform : null;
        if (tileMap != null && targetLayer >= 0 && targetLayer < tileMap.childCount)
        {
            fallback.z = tileMap.GetChild(targetLayer).position.z;
        }
        return fallback;
    }

    private IEnumerator RunCooldown()
    {
        _coolingDown = true;
        if (_collider != null)
            _collider.enabled = false;

        SetOverlayDimmed(true);
        EnsureCooldownArc();

        float elapsed = 0f;
        while (elapsed < warpCooldown)
        {
            float t = Mathf.Clamp01(elapsed / warpCooldown);
            UpdateCooldownArc(t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        UpdateCooldownArc(1f);
        yield return null; // ensure a frame with full fill

        SetOverlayDimmed(false);
        if (_collider != null)
            _collider.enabled = true;

        if (_cooldownArc != null)
        {
            _cooldownArc.positionCount = 0;
            _cooldownArc.enabled = false;
        }

        _coolingDown = false;
    }

    private void SetOverlayDimmed(bool dim)
    {
        float alpha = dim ? 0.25f : _overlayBaseColor.a;
        if (_overlay != null)
            _overlay.color = new Color(
                _overlayBaseColor.r,
                _overlayBaseColor.g,
                _overlayBaseColor.b,
                alpha
            );
        if (_overlayBack != null)
            _overlayBack.color = new Color(
                _overlayBaseColor.r,
                _overlayBaseColor.g,
                _overlayBaseColor.b,
                alpha
            );
    }

    private void EnsureCooldownArc()
    {
        if (_cooldownArc != null)
        {
            _cooldownArc.enabled = true;
            return;
        }

        GameObject arcObj = new GameObject("WarpCooldownArc");
        arcObj.transform.SetParent(transform);
        arcObj.transform.localPosition = Vector3.zero;

        _cooldownArc = arcObj.AddComponent<LineRenderer>();
        _cooldownArc.useWorldSpace = false;
        _cooldownArc.loop = false;
        _cooldownArc.positionCount = 0;
        _cooldownArc.widthMultiplier = 0.08f;
        _cooldownArc.material = new Material(Shader.Find("Sprites/Default"));

        int sortingLayerId = _overlay != null ? _overlay.sortingLayerID : 0;
        int sortingOrder = _overlay != null ? _overlay.sortingOrder + 1 : 0;
        _cooldownArc.sortingLayerID = sortingLayerId;
        _cooldownArc.sortingOrder = sortingOrder;
        _cooldownArc.startColor = _overlayBaseColor;
        _cooldownArc.endColor = _overlayBaseColor;
    }

    private void UpdateCooldownArc(float fill01)
    {
        if (_cooldownArc == null)
            return;

        float radius = GetOverlayRadius() * cooldownArcRadiusScale;
        float maxAngle = Mathf.Lerp(0f, 360f, fill01);
        int steps = Mathf.Max(2, Mathf.CeilToInt((maxAngle / 360f) * cooldownArcSegments));
        _cooldownArc.positionCount = steps + 1;

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 1f : (float)i / steps;
            float angleDeg = Mathf.Lerp(0f, maxAngle, t);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), -0.01f) * radius;
            _cooldownArc.SetPosition(i, pos);
        }
    }

    private float GetOverlayRadius()
    {
        if (_overlay != null)
        {
            Bounds b = _overlay.bounds;
            return Mathf.Max(b.extents.x, b.extents.y);
        }

        return 0.5f;
    }
}
