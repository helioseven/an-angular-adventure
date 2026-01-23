using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RadialMenuNavigation : MonoBehaviour
{
    [SerializeField]
    private Transform scopeRoot;

    [SerializeField]
    private bool disableUnityNavigation = true;

    [SerializeField]
    private bool allowNavigationInPointerMode = true;

    [Header("Center")]
    [SerializeField]
    private Selectable centerSelectable;

    [SerializeField]
    private bool enableCenterSteal = true;

    [Header("Debug")]
    [SerializeField]
    private bool drawGizmos = true;

    private InputAction _navigate;
    private bool _navHeld;
    private readonly List<Selectable> _selectables = new();

    private const float Deadzone = 0.4f;
    private const float CenterEatDot = 0.8f;
    private const float DistancePenalty = 0.55f;

    private void OnEnable()
    {
        InputModeTracker.EnsureInstance();
        CacheSelectables();
        if (InputManager.Instance != null)
            _navigate = InputManager.Instance.Controls.UI.Navigate;
    }

    private void OnDisable()
    {
        _navHeld = false;
    }

    private void Update()
    {
        if (InputModeTracker.Instance == null)
        {
            return;
        }

        if (_navigate == null && InputManager.Instance != null)
            _navigate = InputManager.Instance.Controls.UI.Navigate;

        if (
            InputModeTracker.Instance.CurrentMode != InputMode.Navigation
            && !allowNavigationInPointerMode
        )
        {
            _navHeld = false;
            return;
        }

        if (_navigate == null)
            return;

        Vector2 input = ReadNavigateInput();
        if (input.sqrMagnitude < Deadzone * Deadzone)
        {
            _navHeld = false;
            return;
        }

        if (_navHeld)
            return;

        _navHeld = true;
        MoveSelection(input.normalized);
    }

    private void MoveSelection(Vector2 desiredDir)
    {
        if (EventSystem.current == null)
            return;

        CacheSelectables();
        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
        {
            MenuFocusUtility.SelectPreferred(GetScopeRoot().gameObject);
            return;
        }

        var currentSelectable = current.GetComponent<Selectable>();
        if (currentSelectable == null)
        {
            MenuFocusUtility.SelectPreferred(GetScopeRoot().gameObject);
            return;
        }

        Vector2 currentPos = currentSelectable.transform.position;
        Selectable best = null;
        float bestScore = float.NegativeInfinity;
        float bestDist = float.MaxValue;

        var center = ResolveCenterSelectable();
        if (center != null && enableCenterSteal)
        {
            Vector2 toCenter = (Vector2)center.transform.position - currentPos;
            if (toCenter.sqrMagnitude > 0.001f)
            {
                float centerDot = Vector2.Dot(desiredDir, toCenter.normalized);
                if (centerDot >= CenterEatDot)
                {
                    best = center;
                    bestScore = 1f;
                    bestDist = toCenter.sqrMagnitude;
                }
            }
        }

        foreach (var selectable in _selectables)
        {
            if (selectable == null || selectable == currentSelectable)
                continue;
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;

            Vector2 toCandidate = (Vector2)selectable.transform.position - currentPos;
            if (toCandidate.sqrMagnitude < 0.001f)
                continue;

            float dot = Vector2.Dot(desiredDir, toCandidate.normalized);
            float dist = toCandidate.sqrMagnitude;
            float normDist = Mathf.Sqrt(dist) / 300f;
            float score = dot - normDist * DistancePenalty;
            if (
                best == null
                || score > bestScore
                || (Mathf.Abs(score - bestScore) < 0.0001f && dist < bestDist)
            )
            {
                best = selectable;
                bestScore = score;
                bestDist = dist;
            }
        }

        if (best == null)
            best = FindBestByDot(desiredDir, currentPos, currentSelectable);

        if (best != null)
            EventSystem.current.SetSelectedGameObject(best.gameObject);
    }

    private Selectable FindBestByDot(
        Vector2 desiredDir,
        Vector2 currentPos,
        Selectable currentSelectable
    )
    {
        Selectable best = null;
        float bestDot = float.NegativeInfinity;
        float bestDist = float.MaxValue;

        foreach (var selectable in _selectables)
        {
            if (selectable == null || selectable == currentSelectable)
                continue;
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;

            Vector2 toCandidate = (Vector2)selectable.transform.position - currentPos;
            if (toCandidate.sqrMagnitude < 0.001f)
                continue;

            float dot = Vector2.Dot(desiredDir, toCandidate.normalized);
            float dist = toCandidate.sqrMagnitude;
            float normDist = Mathf.Sqrt(dist) / 300f;
            float score = dot - normDist * DistancePenalty;
            if (score > bestDot || (Mathf.Abs(score - bestDot) < 0.0001f && dist < bestDist))
            {
                best = selectable;
                bestDot = score;
                bestDist = dist;
            }
        }

        return best;
    }

    private void CacheSelectables()
    {
        _selectables.Clear();
        var root = GetScopeRoot();
        if (root == null)
            return;

        var selectables = root.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;
            _selectables.Add(selectable);
            if (!disableUnityNavigation)
                continue;
            var nav = selectable.navigation;
            nav.mode = Navigation.Mode.None;
            selectable.navigation = nav;
        }
    }

    private Transform GetScopeRoot()
    {
        return scopeRoot != null ? scopeRoot : transform;
    }

    private Selectable ResolveCenterSelectable()
    {
        if (centerSelectable == null)
            return centerSelectable;

        if (!centerSelectable.gameObject.activeInHierarchy || !centerSelectable.IsInteractable())
            return null;

        return centerSelectable;
    }

    private Vector2 ReadNavigateInput()
    {
        Vector2 input = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 dpad = pad.dpad.ReadValue();
            if (dpad.sqrMagnitude > 0.01f)
                input = dpad;
        }
        return input;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;
        if (EventSystem.current == null)
            return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
            return;

        var currentSelectable = current.GetComponent<Selectable>();
        if (currentSelectable == null)
            return;

        Vector2 currentPos = currentSelectable.transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPos, 12f);

        if (_navigate == null)
        {
            if (InputManager.Instance != null)
                _navigate = InputManager.Instance.Controls.UI.Navigate;
            else
                return;
        }

        Vector2 input = ReadNavigateInput();
        if (input.sqrMagnitude < 0.001f)
            return;

        Gizmos.color = Color.cyan;
        Vector2 dir = input.normalized;
        Gizmos.DrawLine(currentPos, currentPos + dir * 120f);

#if UNITY_EDITOR
        CacheSelectables();
        var center = ResolveCenterSelectable();
        bool centerSteal = false;
        Selectable gizmoBest = null;
        float gizmoBestScore = float.NegativeInfinity;
        float gizmoBestDist = float.MaxValue;
        if (center != null && enableCenterSteal)
        {
            Vector2 toCenter = (Vector2)center.transform.position - currentPos;
            if (toCenter.sqrMagnitude > 0.001f)
            {
                float centerDot = Vector2.Dot(dir, toCenter.normalized);
                centerSteal = centerDot >= CenterEatDot;
                if (centerSteal)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(center.transform.position, 6f);
                    gizmoBest = center;
                    gizmoBestScore = 1f;
                    gizmoBestDist = toCenter.sqrMagnitude;
                }
            }
        }
        foreach (var selectable in _selectables)
        {
            if (selectable == null || selectable == currentSelectable)
                continue;
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;

            Vector2 toCandidate = (Vector2)selectable.transform.position - currentPos;
            if (toCandidate.sqrMagnitude < 0.001f)
                continue;

            float dot = Vector2.Dot(dir, toCandidate.normalized);
            float normDist = Mathf.Sqrt(toCandidate.sqrMagnitude) / 300f;
            float score = dot - normDist * DistancePenalty;

            bool isCenter = selectable == center;
            Gizmos.color = isCenter ? new Color(1f, 0.5f, 0.1f, 1f) : Color.white;
            Gizmos.DrawLine(currentPos, selectable.transform.position);

            Handles.color = isCenter ? new Color(1f, 0.6f, 0.2f, 1f) : Color.white;
            Handles.Label(selectable.transform.position, $"dot:{dot:F2} score:{score:F2}");

            if (
                gizmoBest == null
                || score > gizmoBestScore
                || (
                    Mathf.Abs(score - gizmoBestScore) < 0.0001f
                    && toCandidate.sqrMagnitude < gizmoBestDist
                )
            )
            {
                gizmoBest = selectable;
                gizmoBestScore = score;
                gizmoBestDist = toCandidate.sqrMagnitude;
            }
        }

        if (gizmoBest != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
            Gizmos.DrawWireSphere(gizmoBest.transform.position, 10f);
        }

#endif
    }
}
