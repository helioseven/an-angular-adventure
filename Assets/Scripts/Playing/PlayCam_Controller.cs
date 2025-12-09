using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCam_Controller : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform player;
    public Rigidbody2D playerBody;

    [Header("Follow Tuning")]
    public float followSmoothTime = 0.3f;
    public float zOffset = -8f;
    public float verticalOffset = 0f;

    [Header("Look-Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothTime = 0.44f;
    public float lookAheadDeadZoneSpeed = 0.5f;
    public float lookAheadReleaseSpeed = 0.2f;
    public float lookAheadRampRange = 2f;
    public float lookAheadGravityScale = 8.0f;
    public float gravityLookAheadMin = 20f;
    public float gravityLookAheadMax = 110f;
    public float gravityLookAheadSpeedScale = 2.0f;
    public float velocityLookAheadTime = 0.35f;
    public float velocityLookAheadMax = 90f;
    public float velocityLookAheadSmoothTime = 0.4f;

    [Header("Warp Transition")]
    public float warpDuration = 0.35f;
    public float warpFocusLookAhead = 1.5f;

    [Header("Zoom")]
    public float zoomOutSize = 9f;
    public float zoomOutExtra = 25f;
    public float zoomSpeedThreshold = 0.75f;
    public float zoomIdleDelay = 2f;
    public float zoomOutSmoothTime = 0.66f;
    public float zoomInSmoothTime = 0.25f;

    // private references
    private PlayGM _gmRef;
    private Camera _cam;

    // private state
    private Vector3 _cameraVelocity = Vector3.zero;
    private float _currentLookAheadPerp;
    private float _lookAheadVelocityPerp;
    private float _lookAheadDirectionPerp;
    private float _currentLookAheadGrav;
    private float _lookAheadVelocityGrav;
    private float _lookAheadDirectionGrav;
    private Vector2 _currentVelocityLookAhead;
    private Vector2 _velocityLookAheadVel;
    private float _baseOrthoSize;
    private float _baseFov;
    private float _orthoVelocity;
    private float _idleTimer;
    private bool _zoomSuppressed;

    void Awake()
    {
        _gmRef = PlayGM.instance;
        _cam = GetComponent<Camera>();
        if (_cam == null)
            _cam = Camera.main;
        if (_cam != null)
        {
            _baseOrthoSize = _cam.orthographicSize;
            _baseFov = _cam.fieldOfView;
        }
        if (!player)
            player = GameObject.FindWithTag("Player")?.transform;
        if (!playerBody && player)
            playerBody = player.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!player)
            return;

        Vector2 moveAxis = GetMovementAxis();
        Vector2 gravAxis = GetGravityAxis();
        float signedPerpSpeed = GetSignedMovementSpeed(moveAxis);
        float absPerpSpeed = Mathf.Abs(signedPerpSpeed);
        float signedGravSpeed = GetSignedMovementSpeed(gravAxis);
        float absGravSpeed = Mathf.Abs(signedGravSpeed);
        float speedMagnitude = playerBody ? playerBody.linearVelocity.magnitude : absPerpSpeed;
        Vector2 velocity = playerBody ? playerBody.linearVelocity : Vector2.zero;

        UpdateLookAhead(
            ref _lookAheadDirectionPerp,
            ref _currentLookAheadPerp,
            ref _lookAheadVelocityPerp,
            absPerpSpeed,
            signedPerpSpeed,
            lookAheadDistance
        );
        UpdateGravityLookAhead(absGravSpeed, signedGravSpeed);
        UpdateVelocityLookAhead(velocity);

        Vector3 target = player.position;
        target += (Vector3)(moveAxis * _currentLookAheadPerp);
        target += (Vector3)(gravAxis * _currentLookAheadGrav);
        target += (Vector3)_currentVelocityLookAhead;
        target.y += verticalOffset;
        target.z = player.position.z + zOffset;

        Vector3 next = Vector3.SmoothDamp(
            transform.position,
            target,
            ref _cameraVelocity,
            _warpFollowActive
                ? Mathf.Max(0.05f, Mathf.Min(followSmoothTime, _warpFollowDuration))
                : followSmoothTime
        );
        transform.position = next;

        if (_warpFollowActive)
        {
            _warpFollowTimer += Time.deltaTime;
            if (_warpFollowTimer >= _warpFollowDuration)
            {
                _warpFollowActive = false;
            }
        }

        if (!_zoomSuppressed)
        {
            UpdateZoom(speedMagnitude);
        }
    }

    public void PlayWarpTransition(Vector3 playerPos, float duration)
    {
        _warpFollowTimer = 0f;
        _warpFollowDuration = duration;
        _warpFollowActive = true;
        _cameraVelocity = Vector3.zero;
        _lookAheadDirectionPerp = 0f;
        _currentLookAheadPerp = 0f;
        _lookAheadDirectionGrav = 0f;
        _currentLookAheadGrav = 0f;
    }

    public void SetZoomSuppressed(bool suppressed)
    {
        _zoomSuppressed = suppressed;
        if (_cam == null)
            return;

        if (suppressed)
        {
            _idleTimer = 0f;
            _orthoVelocity = 0f;
            if (_cam.orthographic)
                _cam.orthographicSize = _baseOrthoSize;
            else
                _cam.fieldOfView = _baseFov;
        }
    }

    private Vector2 GetMovementAxis()
    {
        if (_gmRef == null)
            return Vector2.right;

        switch (_gmRef.gravDirection)
        {
            case PlayGM.GravityDirection.Left:
            case PlayGM.GravityDirection.Right:
                return Vector2.up;
            default:
                return Vector2.right;
        }
    }

    private float GetSignedMovementSpeed(Vector2 moveAxis)
    {
        if (!playerBody)
            return 0f;
        return Vector2.Dot(playerBody.linearVelocity, moveAxis);
    }

    // warp follow state
    private bool _warpFollowActive;
    private float _warpFollowTimer;
    private float _warpFollowDuration;

    private void UpdateZoom(float speed)
    {
        if (_cam == null)
            return;

        if (speed < zoomSpeedThreshold)
            _idleTimer += Time.deltaTime;
        else
            _idleTimer = 0f;

        bool shouldZoomOut = _idleTimer >= zoomIdleDelay;
        float smooth = shouldZoomOut ? zoomOutSmoothTime : zoomInSmoothTime;

        if (_cam.orthographic)
        {
            float targetOut = Mathf.Max(_baseOrthoSize + zoomOutExtra, zoomOutSize);
            float targetSize = shouldZoomOut ? targetOut : _baseOrthoSize;
            float newSize = Mathf.SmoothDamp(
                _cam.orthographicSize,
                targetSize,
                ref _orthoVelocity,
                smooth
            );
            _cam.orthographicSize = newSize;
        }
        else
        {
            float targetOut = Mathf.Max(_baseFov + zoomOutExtra, zoomOutSize);
            float targetFov = shouldZoomOut ? targetOut : _baseFov;
            float newFov = Mathf.SmoothDamp(
                _cam.fieldOfView,
                targetFov,
                ref _orthoVelocity,
                smooth
            );
            _cam.fieldOfView = newFov;
        }
    }

    private void UpdateLookAhead(
        ref float dir,
        ref float current,
        ref float vel,
        float absSpeed,
        float signedSpeed,
        float maxDistance
    )
    {
        if (absSpeed > lookAheadDeadZoneSpeed)
        {
            dir = Mathf.Sign(signedSpeed);
        }
        else if (absSpeed < lookAheadReleaseSpeed)
        {
            dir = 0f;
        }

        float speedT = Mathf.InverseLerp(
            lookAheadDeadZoneSpeed,
            lookAheadDeadZoneSpeed + lookAheadRampRange,
            absSpeed
        );
        float target = dir * maxDistance * speedT;
        current = Mathf.SmoothDamp(current, target, ref vel, lookAheadSmoothTime);
    }

    private void UpdateGravityLookAhead(float absSpeed, float signedSpeed)
    {
        if (absSpeed > lookAheadDeadZoneSpeed)
            _lookAheadDirectionGrav = Mathf.Sign(signedSpeed);
        else if (absSpeed < lookAheadReleaseSpeed)
            _lookAheadDirectionGrav = 0f;

        float baseTarget = lookAheadDistance * lookAheadGravityScale;
        float speedLead = absSpeed * gravityLookAheadSpeedScale;
        float target = _lookAheadDirectionGrav
            * Mathf.Clamp(baseTarget + speedLead, gravityLookAheadMin, gravityLookAheadMax);

        _currentLookAheadGrav = Mathf.SmoothDamp(
            _currentLookAheadGrav,
            target,
            ref _lookAheadVelocityGrav,
            lookAheadSmoothTime
        );
    }

    private void UpdateVelocityLookAhead(Vector2 velocity)
    {
        Vector2 target = Vector2.ClampMagnitude(velocity * velocityLookAheadTime, velocityLookAheadMax);
        _currentVelocityLookAhead = Vector2.SmoothDamp(
            _currentVelocityLookAhead,
            target,
            ref _velocityLookAheadVel,
            velocityLookAheadSmoothTime
        );
    }

    private Vector2 GetGravityAxis()
    {
        if (_gmRef == null)
            return Vector2.zero;

        switch (_gmRef.gravDirection)
        {
            case PlayGM.GravityDirection.Down:
                return Vector2.down;
            case PlayGM.GravityDirection.Up:
                return Vector2.up;
            case PlayGM.GravityDirection.Left:
                return Vector2.left;
            case PlayGM.GravityDirection.Right:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }
}
