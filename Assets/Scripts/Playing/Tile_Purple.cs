using UnityEngine;

public class Tile_Purple : Tile
{
    public float deformGravityScale = 0.82f;
    public float deformTangentialScale = 1.12f;
    public float deformOffsetScale = 1f;
    public float deformEaseInFraction = 0.105f;
    public float deformRecoverDuration = 0.08f;

    private Transform _visualTransform;
    private Transform _deformFrame;
    private Vector3 _baseFrameLocalPosition;
    private Quaternion _baseFrameLocalRotation = Quaternion.identity;
    private Vector3 _baseVisualLocalPosition;
    private Quaternion _baseVisualLocalRotation = Quaternion.identity;
    private Vector3 _baseVisualLocalScale = Vector3.one;
    private Vector2 _surfaceNormalWorld = Vector2.up;
    private float _deformDuration;
    private float _deformTimer;
    private float _recoverTimer;
    private bool _isDeforming;
    private bool _isRecovering;
    private Vector3 _recoverStartFramePosition;
    private Vector3 _recoverStartFrameScale;
    private Quaternion _recoverStartFrameRotation = Quaternion.identity;
    private Quaternion _recoverStartVisualRotation = Quaternion.identity;
    private Vector3 _surfaceAnchorLocalPoint;
    private Vector3 _surfaceAnchorWorldPoint;
    private bool _hasSurfaceAnchor;
    public Vector2 CurrentSurfaceWorldOffset { get; private set; }

    void Start()
    {
        CacheVisualTransform();
        ResetVisualImmediate();
    }

    void Update()
    {
        if (_deformFrame == null || _visualTransform == null)
            return;

        if (_isDeforming)
        {
            _deformTimer = Mathf.Max(0f, _deformTimer - Time.deltaTime);
            float easeInDuration = Mathf.Max(0.001f, deformEaseInFraction);
            float easedStrength = 1f - Mathf.Clamp01(_deformTimer / Mathf.Max(_deformDuration, 0.001f));
            easedStrength = Mathf.Clamp01(easedStrength / easeInDuration);
            ApplyDeformation(Mathf.SmoothStep(0f, 1f, easedStrength));
            return;
        }

        if (_isRecovering)
        {
            _recoverTimer = Mathf.Max(0f, _recoverTimer - Time.deltaTime);
            float t = 1f - Mathf.Clamp01(_recoverTimer / Mathf.Max(deformRecoverDuration, 0.001f));
            _deformFrame.localPosition = Vector3.Lerp(
                _recoverStartFramePosition,
                _baseFrameLocalPosition,
                t
            );
            _deformFrame.localScale = Vector3.Lerp(_recoverStartFrameScale, Vector3.one, t);
            _deformFrame.localRotation = Quaternion.Slerp(
                _recoverStartFrameRotation,
                _baseFrameLocalRotation,
                t
            );
            _visualTransform.localRotation = Quaternion.Slerp(
                _recoverStartVisualRotation,
                _baseVisualLocalRotation,
                t
            );
            _visualTransform.localPosition = _baseVisualLocalPosition;
            _visualTransform.localScale = _baseVisualLocalScale;
            UpdateCurrentWorldOffset();

            if (_recoverTimer <= 0f)
            {
                _isRecovering = false;
                ResetVisualImmediate();
            }
        }
    }

    public void BeginDeformation(Vector2 surfaceNormal, float duration, Vector2 surfaceWorldPoint)
    {
        CacheVisualTransform();
        if (_deformFrame == null || _visualTransform == null)
            return;

        _surfaceNormalWorld = surfaceNormal.sqrMagnitude > 0.001f
            ? surfaceNormal.normalized
            : Vector2.up;
        _deformDuration = Mathf.Max(0.01f, duration);
        _deformTimer = _deformDuration;
        _isDeforming = true;
        _isRecovering = false;
        Vector2 localNormal = GetLocalSurfaceNormal();
        Quaternion frameRotation = GetFrameRotation(localNormal);
        _deformFrame.localPosition = _baseFrameLocalPosition;
        _deformFrame.localRotation = frameRotation;
        _deformFrame.localScale = Vector3.one;
        _visualTransform.localPosition = _baseVisualLocalPosition;
        _visualTransform.localRotation = Quaternion.Inverse(frameRotation) * _baseVisualLocalRotation;
        _visualTransform.localScale = _baseVisualLocalScale;
        _surfaceAnchorWorldPoint = new Vector3(surfaceWorldPoint.x, surfaceWorldPoint.y, _visualTransform.position.z);
        _surfaceAnchorLocalPoint = _deformFrame.InverseTransformPoint(_surfaceAnchorWorldPoint);
        _hasSurfaceAnchor = true;
        ApplyDeformation(0f);
    }

    public void EndDeformation()
    {
        if (_deformFrame == null || _visualTransform == null)
            return;

        _isDeforming = false;
        _isRecovering = true;
        _recoverTimer = Mathf.Max(0.01f, deformRecoverDuration);
        _recoverStartFramePosition = _deformFrame.localPosition;
        _recoverStartFrameScale = _deformFrame.localScale;
        _recoverStartFrameRotation = _deformFrame.localRotation;
        _recoverStartVisualRotation = _visualTransform.localRotation;
    }

    private void CacheVisualTransform()
    {
        if (_deformFrame != null && _visualTransform != null)
            return;

        SpriteRenderer childRenderer = GetComponentInChildren<SpriteRenderer>();
        if (childRenderer == null || childRenderer.transform == transform)
            return;

        _visualTransform = childRenderer.transform;
        _deformFrame = new GameObject("PurpleDeformFrame").transform;
        _deformFrame.SetParent(transform, false);
        _deformFrame.localPosition = _visualTransform.localPosition;
        _deformFrame.localRotation = Quaternion.identity;
        _deformFrame.localScale = Vector3.one;
        _baseFrameLocalPosition = _deformFrame.localPosition;

        _visualTransform.SetParent(_deformFrame, true);
        _baseVisualLocalPosition = _visualTransform.localPosition;
        _baseVisualLocalRotation = _visualTransform.localRotation;
        _baseVisualLocalScale = _visualTransform.localScale;

    }

    private void ApplyDeformation(float strength)
    {
        if (_deformFrame == null || _visualTransform == null)
            return;

        Vector2 localNormal = GetLocalSurfaceNormal();
        Quaternion frameRotation = GetFrameRotation(localNormal);
        Quaternion visualRotation = Quaternion.Inverse(frameRotation) * _baseVisualLocalRotation;

        _deformFrame.localRotation = frameRotation;
        _visualTransform.localRotation = visualRotation;
        _visualTransform.localPosition = _baseVisualLocalPosition;
        _visualTransform.localScale = _baseVisualLocalScale;

        float halfExtentAlongNormal = Mathf.Max(0.001f, Mathf.Abs(_surfaceAnchorLocalPoint.y));
        float compressionDistance =
            halfExtentAlongNormal * (1f - deformGravityScale) * deformOffsetScale;

        _deformFrame.localScale = Vector3.Lerp(
            Vector3.one,
            new Vector3(deformTangentialScale, deformGravityScale, 1f),
            strength
        );
        _deformFrame.localPosition =
            _baseFrameLocalPosition - (Vector3)(localNormal * compressionDistance * strength);
        UpdateCurrentWorldOffset();
    }

    private Vector2 GetLocalSurfaceNormal()
    {
        Transform referenceFrame = _deformFrame != null && _deformFrame.parent != null
            ? _deformFrame.parent
            : transform;
        Vector3 localNormal3 = referenceFrame.InverseTransformDirection(_surfaceNormalWorld);
        Vector2 localNormal = new Vector2(localNormal3.x, localNormal3.y);
        if (localNormal.sqrMagnitude <= 0.0001f)
            return Vector2.up;

        return localNormal.normalized;
    }

    private Quaternion GetFrameRotation(Vector2 localNormal)
    {
        float signedAngle = Vector2.SignedAngle(Vector2.up, localNormal);
        return Quaternion.Euler(0f, 0f, signedAngle);
    }

    private void ResetVisualImmediate()
    {
        if (_deformFrame == null || _visualTransform == null)
            return;

        _deformFrame.localPosition = _baseFrameLocalPosition;
        _deformFrame.localRotation = _baseFrameLocalRotation;
        _deformFrame.localScale = Vector3.one;
        _visualTransform.localPosition = _baseVisualLocalPosition;
        _visualTransform.localRotation = _baseVisualLocalRotation;
        _visualTransform.localScale = _baseVisualLocalScale;
        UpdateCurrentWorldOffset();
    }

    private void UpdateCurrentWorldOffset()
    {
        if (_deformFrame == null || !_hasSurfaceAnchor)
        {
            CurrentSurfaceWorldOffset = Vector2.zero;
            return;
        }

        Vector3 currentWorldAnchor = _deformFrame.TransformPoint(_surfaceAnchorLocalPoint);
        Vector3 worldOffset = currentWorldAnchor - _surfaceAnchorWorldPoint;
        CurrentSurfaceWorldOffset = new Vector2(worldOffset.x, worldOffset.y);
    }
}
