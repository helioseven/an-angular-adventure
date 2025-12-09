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
    public float lookAheadSmoothTime = 0.15f;
    public float lookAheadDeadZoneSpeed = 0.5f;
    public float lookAheadReleaseSpeed = 0.2f;

    [Header("Warp Transition")]
    public float warpDuration = 0.35f;
    public float warpFocusLookAhead = 1.5f;

    // private references
    private PlayGM _gmRef;

    // private state
    private Vector3 _cameraVelocity = Vector3.zero;
    private float _currentLookAhead;
    private float _lookAheadVelocity;
    private float _lookAheadDirection;
    private Coroutine _warpRoutine;
    private bool _isWarping;

    void Awake()
    {
        _gmRef = PlayGM.instance;
        if (!player)
            player = GameObject.FindWithTag("Player")?.transform;
        if (!playerBody && player)
            playerBody = player.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!player)
            return;

        if (_isWarping)
            return;

        Vector2 moveAxis = GetMovementAxis();
        float signedSpeed = GetSignedMovementSpeed(moveAxis);
        float absSpeed = Mathf.Abs(signedSpeed);

        if (absSpeed > lookAheadDeadZoneSpeed)
        {
            _lookAheadDirection = Mathf.Sign(signedSpeed);
        }
        else if (absSpeed < lookAheadReleaseSpeed)
        {
            _lookAheadDirection = 0f;
        }

        float targetLookAhead = _lookAheadDirection * lookAheadDistance;

        _currentLookAhead = Mathf.SmoothDamp(
            _currentLookAhead,
            targetLookAhead,
            ref _lookAheadVelocity,
            lookAheadSmoothTime
        );

        Vector3 target = player.position;
        target += (Vector3)(moveAxis * _currentLookAhead);
        target.y += verticalOffset;
        target.z = player.position.z + zOffset;

        Vector3 next = Vector3.SmoothDamp(
            transform.position,
            target,
            ref _cameraVelocity,
            followSmoothTime
        );
        transform.position = next;
    }

    public void PlayWarpTransition(Vector3 playerPos)
    {
        if (_warpRoutine != null)
            StopCoroutine(_warpRoutine);
        _warpRoutine = StartCoroutine(WarpTransition(playerPos));
    }

    private IEnumerator WarpTransition(Vector3 playerPos)
    {
        _isWarping = true;
        _cameraVelocity = Vector3.zero;
        _lookAheadDirection = 0f;

        Vector2 moveAxis = GetMovementAxis();
        float startLook = _currentLookAhead;
        float targetLook = warpFocusLookAhead;
        Vector3 start = transform.position;

        Vector3 target = playerPos;
        target += (Vector3)(moveAxis * targetLook);
        target.y += verticalOffset;
        target.z = playerPos.z + zOffset;

        float t = 0f;
        while (t < warpDuration)
        {
            float lerpT = Mathf.SmoothStep(0f, 1f, t / warpDuration);
            _currentLookAhead = Mathf.Lerp(startLook, targetLook, lerpT);
            transform.position = Vector3.Lerp(start, target, lerpT);
            t += Time.deltaTime;
            yield return null;
        }

        _currentLookAhead = targetLook;
        transform.position = target;
        _isWarping = false;
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
}
