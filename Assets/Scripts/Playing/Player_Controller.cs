using circleXsquares;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Player_Controller : MonoBehaviour
{
    private enum PurpleBounceState
    {
        Inactive,
        Latched,
        ResolvedCooldown,
    }

    // public variables
    public int speed = 600;
    public float jumpForce = 420;
    public bool isOnIce;
    public bool isIceScalingBlockingJump;
    public BallSkinDatabase skinDB;

    // Tweakables
    // Audio - video gamey pitch jump
    public float jumpPitchVariance = 0.03f;

    // Ball Rolling mechanics tweakables
    public float groundProbeDistance = 0.02f; // 0.2 was too long
    public float torqueStrength = 8f; // 6-10 feels controlled
    public float iceControlMultiplier = 0.5f; // lower = slipperier (0.35-0.6)
    public float iceSlideBoost = 6f; // add carry on ice (4-8 range)

    [Header("Purple Bounce")]
    public float purpleDeformDuration = 0.2f;
    public float purpleSuperJumpReadyDelay = 0f;
    public float purpleMinImpactSpeed = 0.5f;

    [FormerlySerializedAs("purpleCancelRecontactGraceDuration")]
    public float purpleResolvedRelatchCooldownDuration = 0.12f;
    public float purpleResolvedRelatchSeparation = 0.08f;
    public int purpleLostContactToleranceSteps = 1;
    public float purpleLatchProbeDistance = 0.04f;
    public float purpleSurfaceMinDot = 0.45f;
    public float purpleBounceBaseSpeed = 4.8f;
    public float purpleBounceImpactScale = 0.3f;
    public float purpleBounceMaxSpeed = 7.5f;
    public float purpleSuperBaseSpeed = 8.6f;
    public float purpleSuperImpactScale = 0.8f;
    public float purpleSuperMaxSpeed = 15.5f;
    public float purpleLaunchSeparation = 0.08f;
    public float purpleFollowGlideDuration = 0.04f;
    public float purpleAngularDampingDuringDeform = 0.9f;
    public bool debugPurpleBounceLogging = true;

    // private references
    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _purpleOverlayRenderer;
    private AudioSource _audioSource;
    private PlayGM _gmRef;
    private Collider2D _groundCheckCollider;
    private Rigidbody2D _rb2d;
    private Transform _purpleOverlayTransform;

    // private variables
    // air woosh settings
    private string airWooshSoundName = "air-woosh";
    private float airWooshMinSpeed = 3f;
    private float airWooshMaxSpeed = 14f;
    private float airWooshMaxVolume = 0.6f;
    private float airWooshFadeInSpeed = 0.33f; // lower number means takes longer to reach max volume
    private float airWooshFadeOutSpeed = 8f; // higher number means faster fadout
    private float airWooshPitchMin = 0.8f;
    private float airWooshPitchMax = 1.2f;

    // jump and more
    private Vector2 _jumpForceVec;
    private bool _jumpNow = false;
    private int _maxJumps = 1;
    private int _numJumps;
    private const float MAX_SPEED_JUMP_BONUS = 0.15f;
    private const float SPEED_FOR_MAX_BONUS = 10f;
    private const float JUMP_HOLD_FORCE_SCALE = 0.33f;
    private const float SHORT_HOP_RELEASE_DAMP = 0.55f;
    private const float MAX_ANGULAR_VELOCITY = 380f;
    private float _pendingJumpMultiplier = 1f;
    private bool _jumpHoldActive;
    private float _jumpHoldForce;
    private Vector2 _jumpHoldDirection;
    private bool _jumpTriggered;
    private bool _groundOverrideJumpBlock;
    private bool _inputEnabled = true;
    private bool _suppressJumpUntilRelease;
    private float _airWooshVolume;
    private bool _loopingAudioSuppressed;
    private bool _rollingMuted;
    private float _defaultGravityScale;
    private const float PROBE_SKIN = 0.01f;
    private const float ROLLING_FADE_START_SPEED = 0.1f;
    private const float ROLLING_FADE_MID_SPEED = 5f;
    private const float ROLLING_FADE_END_SPEED = 9f;
    private const float ROLLING_PITCH = 1f;
    private string rollingSoftSoundName = "rolling-soft";
    private string rollingLoudSoundName = "rolling-loud";

    // Active latched session state.
    private PurpleBounceState _purpleBounceState;
    private Collider2D _activePurpleCollider;
    private Tile_Purple _activePurpleTile;
    private Vector2 _activePurpleSupportNormal;
    private float _purpleBounceTimer;
    private float _purpleImpactSpeed;
    private bool _purpleResolveQueued;
    private bool _purpleResolveAsSuperJump;
    private int _jumpPressSerial;
    private int _purpleSessionSequence;
    private int _activePurpleSessionId;
    private int _activePurpleSessionStartJumpSerial;
    private int _activePurpleConsumedJumpSerial;
    private int _lastConsumedJumpPressSerial;
    private int _lastConsumedJumpPressSessionId;
    private bool _purpleVisualWasVisible;
    private bool _loggedPurpleVisualMissingRefs;
    private string _lastPurpleLogSignature;
    private float _lastPurpleLogTime;
    private Vector2 _prePhysicsVelocity;
    private Vector2 _purpleHeldTangentialDirection;
    private float _purpleHeldTangentialSpeed;
    private Vector2 _purpleLastFollowOffset;
    private bool _activePurpleContactCached;
    private int _activePurpleLostContactSteps;

    // Post-resolve same-tile relatch gate.
    private Collider2D _resolvedPurpleCollider;
    private Tile_Purple _resolvedPurpleTile;
    private int _resolvedPurpleSessionId;
    private float _resolvedPurpleUntilTime;
    private bool _resolvedPurpleExitObserved;
    private bool _resolvedPurpleUsesStrictRelatch;
    private float _resolvedPurpleLastSeparation;
    private const float PURPLE_LOG_REPEAT_WINDOW = 0.2f;
    private const float PURPLE_FOLLOW_CAST_SKIN = 0.005f;
    private const float PURPLE_FOLLOW_DELTA_EPSILON = 0.000001f;
    private const float PURPLE_RELATCH_NEAR_EXPIRY_WINDOW = 0.025f;
    private readonly RaycastHit2D[] _purpleFollowCastHits = new RaycastHit2D[8];

    // New Input System
    private InputControls _controls;
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private System.Action<InputAction.CallbackContext> _onMove;
    private System.Action<InputAction.CallbackContext> _onMoveCanceled;
    private System.Action<InputAction.CallbackContext> _onJump;
    private System.Action<InputAction.CallbackContext> _onJumpCanceled;

    void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d != null)
        {
            // Smooth render between physics steps to avoid "double image" ghosting at high fall speed.
            _rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb2d.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            _defaultGravityScale = _rb2d.gravityScale;
        }
        _jumpForceVec = new Vector2(0.0f, jumpForce);
        _groundCheckCollider = GetComponent<Collider2D>();
        _audioSource = GetComponent<AudioSource>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        InitializePurpleOverlay();

        // use the global InputManager's shared controls
        _controls = InputManager.Instance.Controls;

        InputControls.PlayerActions player = _controls.Player;

        _onMove = ctx => _moveInput = ctx.ReadValue<Vector2>();
        _onMoveCanceled = _ => _moveInput = Vector2.zero;
        _onJump = _ =>
        {
            _jumpPressSerial++;
            _jumpPressed = true;
            _jumpTriggered = true;
        };
        _onJumpCanceled = _ =>
        {
            _jumpPressed = false;
            _jumpHoldActive = false;
        };

        player.Move.performed += _onMove;
        player.Move.canceled += _onMoveCanceled;

        player.Jump.started += _onJump;
        player.Jump.canceled += _onJumpCanceled;
    }

    void Start()
    {
        _gmRef = PlayGM.instance;
        _loopingAudioSuppressed = false;
        int index = PlayerPrefs.GetInt("SelectedBallSkin", 0);
        _spriteRenderer.sprite = skinDB.skins[index];
        SyncPurpleOverlaySprite();
        UpdateJumpForce();
    }

    void Update()
    {
        _groundOverrideJumpBlock = ProbeForNonIceGround(); // only used to override ice blocking
        UpdateJumpForce();
        UpdatePurpleBounceWindow();
        UpdateJumping();
        UpdatePurpleBounceVisual();
        UnityEditorGodMode();
        UpdateRollingSound();
        UpdateAirWooshSound();
    }

    void FixedUpdate()
    {
        _prePhysicsVelocity = _rb2d != null ? _rb2d.linearVelocity : Vector2.zero;
        ResolveQueuedPurpleBounce();
        ApplyPurpleBounceConstraint();
        Move();
        Jump();
        ApplyJumpHoldForce();
    }

    void OnDestroy()
    {
        var player = _controls.Player;
        player.Move.performed -= _onMove;
        player.Move.canceled -= _onMoveCanceled;
        player.Jump.started -= _onJump;
        player.Jump.canceled -= _onJumpCanceled;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        _numJumps = 0;
        Tile tile = other.collider.GetComponentInParent<Tile>();
        if (tile != null && tile.data.color != TileColor.Red)
        {
            float baseVolume = _gmRef.ImpactIntensityToVolume(
                other.relativeVelocity,
                Physics2D.gravity
            );
            float volume = baseVolume * 0.75f;
            _gmRef.soundManager.Play("thud", volume);
        }

        if (IsPurpleCollider(other.collider))
        {
            if (_activePurpleCollider == other.collider)
                _activePurpleContactCached = true;
            if (MatchesResolvedPurple(other.collider))
                _resolvedPurpleLastSeparation = 0f;
            LogPurpleBounce(
                "Touch",
                $"{other.collider.name} phase=Enter contacts={other.contactCount} rv={FormatVector2(other.relativeVelocity)} vel={FormatVector2(_rb2d != null ? _rb2d.linearVelocity : Vector2.zero)}"
            );
        }
        TryBeginPurpleBounceFromEnter(other);
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (_activePurpleCollider == other.collider)
            _activePurpleContactCached = true;
        if (MatchesResolvedPurple(other.collider))
            _resolvedPurpleLastSeparation = 0f;
        MaintainPurpleBounceContact(other);
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (_activePurpleCollider == other.collider)
        {
            _activePurpleContactCached = false;
            LogPurpleBounce("Exit", $"{other.collider.name} {DescribePurpleState()}");
        }
        if (MatchesResolvedPurple(other.collider))
        {
            _resolvedPurpleExitObserved = true;
            LogPurpleBounce(
                "CooldownExit",
                $"{other.collider.name} session={_resolvedPurpleSessionId}"
            );
            ClearResolvedPurpleCooldown("exit");
        }
    }

    /* Input System-based replacements */

    public void Move()
    {
        if (!_inputEnabled)
            return;
        if (_purpleBounceState == PurpleBounceState.Latched)
            return;

        // read input
        float rollInput = GetPerpendicularComponent(_moveInput);
        Vector2 movement =
            rollInput == 0f ? Vector2.zero : GetPerpendicularUnit(Mathf.Sign(rollInput));
        Vector2 upwardDragForcedMovement = UpdateUpwardDragForce(movement);
        float controlScale = isOnIce ? iceControlMultiplier : 1f;
        // Less input authority on ice; add a bit of carry to keep skidding
        _rb2d.AddForce(upwardDragForcedMovement * speed * controlScale * Time.deltaTime);
        if (isOnIce && _rb2d.linearVelocity.sqrMagnitude > 0.001f)
        {
            _rb2d.AddForce(_rb2d.linearVelocity.normalized * iceSlideBoost * Time.deltaTime);
        }
        ApplyTorqueForMovement(upwardDragForcedMovement);
    }

    public void Jump()
    {
        if (!_inputEnabled)
            return;

        if (_jumpNow)
        {
            Vector2 jumpVec = _jumpForceVec * _pendingJumpMultiplier;
            _rb2d.AddForce(jumpVec);
            _jumpHoldDirection = jumpVec.normalized;
            _jumpHoldForce = _jumpForceVec.magnitude * JUMP_HOLD_FORCE_SCALE;
            _jumpHoldActive = _jumpPressed;
            _pendingJumpMultiplier = 1f;
            _jumpNow = false;
        }
    }

    public void UpdateJumping()
    {
        if (!_inputEnabled)
        {
            _jumpTriggered = false;
            return;
        }

        if (_purpleBounceState == PurpleBounceState.Latched)
        {
            TryConsumePurpleSuperJumpPress();
            _jumpTriggered = false;
            return;
        }

        if (_suppressJumpUntilRelease)
        {
            if (!_jumpPressed)
                _suppressJumpUntilRelease = false;
            _jumpTriggered = false;
            return;
        }

        bool canJump = _numJumps < _maxJumps;
        if (_numJumps == 0)
        {
            canJump = canJump && _groundCheckCollider.IsTouchingLayers();
        }

        if (isIceScalingBlockingJump && !_groundOverrideJumpBlock)
        {
            canJump = false;
        }

        if (canJump && _jumpTriggered)
        {
            _numJumps++;
            _jumpNow = true;
            _pendingJumpMultiplier = ComputeJumpSpeedMultiplier();
            _gmRef.soundManager.PlayWithPitchVariance("jump", jumpPitchVariance);
        }
        _jumpTriggered = false;
    }

    public void UpdateJumpForce()
    {
        UpdateJumpForceVector(PlayGM.instance.gravDirection);
    }

    public void UpdateJumpForceVector(GravityDirection gd)
    {
        switch (gd)
        {
            case GravityDirection.Down:
                _jumpForceVec = new Vector2(0.0f, jumpForce);
                break;
            case GravityDirection.Left:
                _jumpForceVec = new Vector2(jumpForce, 0.0f);
                break;
            case GravityDirection.Up:
                _jumpForceVec = new Vector2(0.0f, -jumpForce);
                break;
            case GravityDirection.Right:
                _jumpForceVec = new Vector2(-jumpForce, 0.0f);
                break;
        }
    }

    public Vector2 UpdateUpwardDragForce(Vector2 inMovement)
    {
        // Ignore input along the gravity axis so movement is only perpendicular to gravity
        switch (PlayGM.instance.gravDirection)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                inMovement.y = 0f;
                break;
            case GravityDirection.Left:
            case GravityDirection.Right:
                inMovement.x = 0f;
                break;
        }

        return inMovement;
    }

    public void UnityEditorGodMode()
    {
        // Gravity debug keys kept for editor desktop testing
#if UNITY_EDITOR
        if (Keyboard.current.kKey.wasPressedThisFrame)
            _gmRef.SetGravity(GravityDirection.Down);
        if (Keyboard.current.jKey.wasPressedThisFrame)
            _gmRef.SetGravity(GravityDirection.Left);
        if (Keyboard.current.iKey.wasPressedThisFrame)
            _gmRef.SetGravity(GravityDirection.Up);
        if (Keyboard.current.lKey.wasPressedThisFrame)
            _gmRef.SetGravity(GravityDirection.Right);
#endif
    }

    public void UpdateRollingSound()
    {
        if (_loopingAudioSuppressed)
        {
            StopRollingSound();
            return;
        }

        if (_gmRef.victoryAchieved)
        {
            StopRollingSound();
            return;
        }

        if (_rollingMuted)
        {
            StopRollingSound();
            return;
        }

        float volume = _gmRef.SlideIntensityToVolume(_rb2d.linearVelocity, Physics2D.gravity);
        if (!_groundCheckCollider.IsTouchingLayers())
            volume = 0.0f;
        if (volume <= 0f)
        {
            StopRollingSound();
            return;
        }

        float speed = GetPerpendicularSpeed();
        float t;
        if (speed <= ROLLING_FADE_MID_SPEED)
        {
            t = 0.5f * Mathf.InverseLerp(ROLLING_FADE_START_SPEED, ROLLING_FADE_MID_SPEED, speed);
        }
        else
        {
            t =
                0.5f
                + 0.5f * Mathf.InverseLerp(ROLLING_FADE_MID_SPEED, ROLLING_FADE_END_SPEED, speed);
        }
        float blend = Mathf.SmoothStep(0f, 1f, t);
        float softVolume = volume * (1f - blend);
        float loudVolume = volume * blend;

        _gmRef.soundManager.SetLoopingSound(rollingSoftSoundName, softVolume, ROLLING_PITCH);
        _gmRef.soundManager.SetLoopingSound(rollingLoudSoundName, loudVolume, ROLLING_PITCH);

        if (_audioSource != null)
        {
            _audioSource.volume = 0f;
            if (_audioSource.isPlaying)
                _audioSource.Stop();
        }
    }

    private void UpdateAirWooshSound()
    {
        if (_gmRef == null)
            return;
        if (_loopingAudioSuppressed)
        {
            StopAirWooshSound();
            return;
        }

        float targetVolume = 0f;
        if (!_gmRef.victoryAchieved && !_groundCheckCollider.IsTouchingLayers())
        {
            float speed = _rb2d.linearVelocity.magnitude;
            float t = Mathf.InverseLerp(airWooshMinSpeed, airWooshMaxSpeed, speed);
            targetVolume = airWooshMaxVolume * Mathf.SmoothStep(0f, 1f, t);
        }

        float fadeSpeed =
            targetVolume > _airWooshVolume ? airWooshFadeInSpeed : airWooshFadeOutSpeed;
        _airWooshVolume = Mathf.MoveTowards(
            _airWooshVolume,
            targetVolume,
            fadeSpeed * Time.deltaTime
        );

        float pitchT = airWooshMaxVolume > 0f ? _airWooshVolume / airWooshMaxVolume : 0f;
        float pitch = Mathf.Lerp(airWooshPitchMin, airWooshPitchMax, pitchT);
        _gmRef.soundManager.SetLoopingSound(airWooshSoundName, _airWooshVolume, pitch);
    }

    public void StopAirWooshSound()
    {
        _airWooshVolume = 0f;
        if (_gmRef != null)
            _gmRef.soundManager.StopSound(airWooshSoundName);
    }

    public void StopRollingSound()
    {
        if (_gmRef != null)
        {
            _gmRef.soundManager.StopSound(rollingSoftSoundName);
            _gmRef.soundManager.StopSound(rollingLoudSoundName);
        }

        if (_audioSource != null)
        {
            _audioSource.volume = 0f;
            if (_audioSource.isPlaying)
                _audioSource.Stop();
        }
    }

    public void SetRollingMuted(bool muted)
    {
        _rollingMuted = muted;
        if (muted)
            StopRollingSound();
    }

    public void PrepareForSceneExit()
    {
        _loopingAudioSuppressed = true;
        SetInputEnabled(false);
        SetRollingMuted(true);
        StopAirWooshSound();
        StopRollingSound();
    }

    private void UpdatePurpleBounceWindow()
    {
        switch (_purpleBounceState)
        {
            case PurpleBounceState.Latched:
                _purpleBounceTimer = Mathf.Max(0f, _purpleBounceTimer - Time.deltaTime);
                if (_purpleBounceTimer <= 0f)
                    QueuePurpleBounceResolve(false);
                break;
            case PurpleBounceState.ResolvedCooldown:
                UpdateResolvedPurpleCooldown();
                break;
        }
    }

    private void ApplyPurpleBounceConstraint()
    {
        if (_purpleBounceState != PurpleBounceState.Latched)
        {
            RestoreDefaultGravityScale();
            return;
        }

        _rb2d.gravityScale = 0f;
        Vector2 tangentialVelocity = ExtractPerpendicularVelocity(_rb2d.linearVelocity);
        if (_purpleHeldTangentialDirection.sqrMagnitude > 0.001f)
        {
            float alongHeld = Vector2.Dot(tangentialVelocity, _purpleHeldTangentialDirection);
            float preservedSpeed = Mathf.Max(alongHeld, _purpleHeldTangentialSpeed);
            tangentialVelocity = _purpleHeldTangentialDirection * preservedSpeed;
        }
        _rb2d.linearVelocity = tangentialVelocity;
        ApplyPurpleBounceFollowOffset();
        _rb2d.angularVelocity *= purpleAngularDampingDuringDeform;
    }

    private void ApplyPurpleBounceFollowOffset()
    {
        if (_activePurpleTile == null || _rb2d == null)
            return;
        bool hasActiveContact = HasActivePurpleContact(
            out string contactSource,
            out float contactSeparation
        );
        if (!hasActiveContact)
        {
            _activePurpleLostContactSteps++;
            int toleranceSteps = Mathf.Max(0, purpleLostContactToleranceSteps);
            if (_activePurpleLostContactSteps > toleranceSteps)
            {
                LogPurpleBounce(
                    "CancelFollowLostContact",
                    $"{_activePurpleCollider?.name} target={FormatVector2(_activePurpleTile.CurrentSurfaceWorldOffset)} missSteps={_activePurpleLostContactSteps} separation={contactSeparation:F3} {DescribePurpleState()}"
                );
                CancelPurpleBounceWindow(enterResolvedCooldown: true, reason: "lostContact");
                return;
            }

            LogPurpleBounce(
                "HoldFollowLostContact",
                $"{_activePurpleCollider?.name} missSteps={_activePurpleLostContactSteps}/{toleranceSteps} separation={contactSeparation:F3} {DescribePurpleState()}"
            );
        }
        else
        {
            if (_activePurpleLostContactSteps > 0 || contactSource != "callback")
            {
                LogPurpleBounce(
                    "KeepFollowContact",
                    $"{_activePurpleCollider?.name} source={contactSource} separation={contactSeparation:F3} {DescribePurpleState()}"
                );
            }
            _activePurpleLostContactSteps = 0;
        }

        Vector2 targetFollowOffset = ProjectPurpleFollowOffset(
            _activePurpleTile.CurrentSurfaceWorldOffset
        );
        float glideT =
            purpleFollowGlideDuration <= 0f
                ? 1f
                : Mathf.Clamp01(Time.fixedDeltaTime / purpleFollowGlideDuration);
        Vector2 nextFollowOffset = Vector2.Lerp(
            _purpleLastFollowOffset,
            targetFollowOffset,
            glideT
        );
        Vector2 followDelta = nextFollowOffset - _purpleLastFollowOffset;
        if (followDelta.sqrMagnitude <= PURPLE_FOLLOW_DELTA_EPSILON)
            return;

        Vector2 clampedFollowDelta = ClampPurpleFollowDeltaAgainstSolids(followDelta);
        if (clampedFollowDelta.sqrMagnitude > 0f)
        {
            _rb2d.position += clampedFollowDelta;
            _purpleLastFollowOffset += clampedFollowDelta;
            return;
        }

        LogPurpleBounce(
            "CancelFollowBlocked",
            $"{_activePurpleCollider?.name} target={FormatVector2(targetFollowOffset)} requested={FormatVector2(followDelta)} blocked={FormatVector2(clampedFollowDelta)} {DescribePurpleState()}"
        );
        CancelPurpleBounceWindow(enterResolvedCooldown: true, reason: "followBlocked");
    }

    private void ResolveQueuedPurpleBounce()
    {
        if (!_purpleResolveQueued)
            return;

        bool asSuperJump = _purpleResolveAsSuperJump;
        _purpleResolveQueued = false;
        _purpleResolveAsSuperJump = false;
        ResolvePurpleBounce(asSuperJump);
    }

    private void QueuePurpleBounceResolve(bool asSuperJump)
    {
        if (_purpleBounceState != PurpleBounceState.Latched)
            return;

        _purpleResolveQueued = true;
        _purpleResolveAsSuperJump = _purpleResolveAsSuperJump || asSuperJump;
        LogPurpleBounce("QueueResolve", _purpleResolveAsSuperJump ? "super" : "normal");
    }

    private void TryConsumePurpleSuperJumpPress()
    {
        if (_purpleBounceState != PurpleBounceState.Latched || !_jumpTriggered)
            return;

        if (_jumpPressSerial <= _activePurpleSessionStartJumpSerial)
        {
            LogPurpleBounce(
                "IgnoreSuperPress",
                $"press={_jumpPressSerial} session={_activePurpleSessionId} reason=stale"
            );
            return;
        }

        if (_lastConsumedJumpPressSerial == _jumpPressSerial)
        {
            LogPurpleBounce(
                "IgnoreSuperPress",
                $"press={_jumpPressSerial} session={_activePurpleSessionId} consumedBy={_lastConsumedJumpPressSessionId}"
            );
            return;
        }

        if (!IsPurpleSuperJumpReady())
        {
            LogPurpleBounce(
                "IgnoreSuperPress",
                $"press={_jumpPressSerial} session={_activePurpleSessionId} timer={_purpleBounceTimer:F3}"
            );
            return;
        }

        _lastConsumedJumpPressSerial = _jumpPressSerial;
        _lastConsumedJumpPressSessionId = _activePurpleSessionId;
        _activePurpleConsumedJumpSerial = _jumpPressSerial;
        LogPurpleBounce(
            "ConsumeSuperPress",
            $"press={_jumpPressSerial} session={_activePurpleSessionId}"
        );
        QueuePurpleBounceResolve(true);
    }

    private bool IsPurpleSuperJumpReady()
    {
        return GetPurpleBounceElapsed() >= purpleSuperJumpReadyDelay;
    }

    private float GetPurpleBounceElapsed()
    {
        return Mathf.Max(0f, purpleDeformDuration - _purpleBounceTimer);
    }

    private void MaintainPurpleBounceContact(Collision2D collision)
    {
        if (!IsPurpleCollider(collision.collider))
            return;

        if (_purpleBounceState == PurpleBounceState.Latched)
        {
            if (_activePurpleCollider == collision.collider)
                _activePurpleContactCached = true;
            return;
        }

        if (_purpleBounceState == PurpleBounceState.Inactive)
            return;

        if (MatchesResolvedPurple(collision.collider))
        {
            LogPurpleBounce(
                "CooldownStay",
                $"{collision.collider.name} phase=Stay session={_resolvedPurpleSessionId} {DescribePurpleCooldownGate()}"
            );
        }
    }

    private void TryBeginPurpleBounceFromEnter(Collision2D collision)
    {
        if (!IsPurpleCollider(collision.collider))
            return;

        if (_purpleBounceState == PurpleBounceState.Latched)
        {
            LogPurpleBounce(
                "SkipBegin",
                $"phase=Enter incoming={collision.collider.name} active={_activePurpleCollider?.name} {DescribePurpleState()}"
            );
            return;
        }

        if (
            _purpleBounceState == PurpleBounceState.ResolvedCooldown
            && MatchesResolvedPurple(collision.collider)
        )
        {
            if (!CanRelatchResolvedPurple(collision, out string relatchReason))
            {
                LogPurpleBounce(
                    "BlockBegin",
                    $"{collision.collider.name} phase=Enter reason=sameTileRelatchBlocked {relatchReason}"
                );
                return;
            }
        }

        if (!IsPurpleBounceImpact(collision, "Enter", out float impactSpeed))
            return;

        BeginPurpleBounceSession(collision, impactSpeed);
    }

    private void BeginPurpleBounceSession(Collision2D collision, float impactSpeed)
    {
        if (_purpleBounceState == PurpleBounceState.ResolvedCooldown)
            ClearResolvedPurpleCooldown("relatchAllowed");

        _purpleBounceState = PurpleBounceState.Latched;
        _activePurpleSessionId = ++_purpleSessionSequence;
        _activePurpleCollider = collision.collider;
        _activePurpleTile = collision.collider.GetComponentInParent<Tile_Purple>();
        _purpleBounceTimer = purpleDeformDuration;
        _purpleImpactSpeed = impactSpeed;
        _purpleResolveQueued = false;
        _purpleResolveAsSuperJump = false;
        _activePurpleSessionStartJumpSerial = _jumpPressSerial;
        _activePurpleConsumedJumpSerial = 0;
        _jumpNow = false;
        _jumpHoldActive = false;
        _numJumps = _maxJumps;
        _rb2d.gravityScale = 0f;
        Vector2 tangentialVelocity = ExtractPerpendicularVelocity(_rb2d.linearVelocity);
        _rb2d.linearVelocity = tangentialVelocity;
        _purpleHeldTangentialSpeed = tangentialVelocity.magnitude;
        _purpleHeldTangentialDirection =
            _purpleHeldTangentialSpeed > 0.001f
                ? tangentialVelocity / _purpleHeldTangentialSpeed
                : Vector2.zero;
        _purpleLastFollowOffset = Vector2.zero;
        _activePurpleContactCached = true;
        _activePurpleLostContactSteps = 0;
        GetPurpleSurfaceAnchor(
            collision,
            GetJumpDirectionVector(),
            out Vector2 surfaceAnchorPoint,
            out Vector2 surfaceNormal
        );
        _activePurpleSupportNormal =
            surfaceNormal.sqrMagnitude > 0.001f
                ? surfaceNormal.normalized
                : GetJumpDirectionVector();
        _activePurpleTile?.BeginDeformation(surfaceNormal, _purpleBounceTimer, surfaceAnchorPoint);
        LogPurpleBounce(
            "Begin",
            $"{collision.collider.name} phase=Enter session={_activePurpleSessionId} impact={impactSpeed:F2} support={FormatVector2(_activePurpleSupportNormal)} anchor={FormatVector2(surfaceAnchorPoint)} tangential={_purpleHeldTangentialSpeed:F2}"
        );
    }

    private bool IsPurpleBounceImpact(Collision2D collision, string phase, out float impactSpeed)
    {
        impactSpeed = 0f;
        if (collision.contactCount == 0)
        {
            LogPurpleBounce("RejectImpact", $"{collision.collider.name} noContacts");
            return false;
        }

        Vector2 jumpDir = GetJumpDirectionVector();
        bool hasSupportedSurface = TryGetPurpleSupportAnchor(
            collision,
            jumpDir,
            out _,
            out _,
            out float bestDot
        );

        if (!hasSupportedSurface)
        {
            LogPurpleBounce(
                "RejectImpact",
                $"{collision.collider.name} phase={phase} unsupported bestDot={bestDot:F2} contacts={collision.contactCount} jumpDir={FormatVector2(jumpDir)}"
            );
            return false;
        }

        Vector2 gravityDir = GravityDirectionVector();
        float relativeImpact = Mathf.Max(0f, Vector2.Dot(collision.relativeVelocity, gravityDir));
        float bodyImpact = Mathf.Max(0f, Vector2.Dot(_rb2d.linearVelocity, gravityDir));
        float cachedImpact = Mathf.Max(0f, Vector2.Dot(_prePhysicsVelocity, gravityDir));
        impactSpeed = Mathf.Max(relativeImpact, bodyImpact, cachedImpact);
        bool validSpeed = impactSpeed >= purpleMinImpactSpeed;
        if (!validSpeed)
        {
            LogPurpleBounce(
                "RejectImpact",
                $"{FormatImpactDetails(collision, phase, relativeImpact, bodyImpact, cachedImpact)} slow={impactSpeed:F2}"
            );
        }
        return validSpeed;
    }

    private bool TryGetPurpleSupportAnchor(
        Collision2D collision,
        Vector2 jumpDir,
        out Vector2 anchorPoint,
        out Vector2 supportNormal,
        out float bestDot
    )
    {
        anchorPoint = collision.transform.position;
        supportNormal = jumpDir;
        bestDot = float.MinValue;
        Vector2 pointSum = Vector2.zero;
        Vector2 normalSum = Vector2.zero;
        int supportedContactCount = 0;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            float dot = Vector2.Dot(contact.normal, jumpDir);
            bestDot = Mathf.Max(bestDot, dot);
            if (dot < purpleSurfaceMinDot)
                continue;

            pointSum += contact.point;
            normalSum += contact.normal;
            supportedContactCount++;
        }

        if (supportedContactCount == 0)
            return false;

        anchorPoint = pointSum / supportedContactCount;
        supportNormal = normalSum.sqrMagnitude > 0.001f ? normalSum.normalized : jumpDir;
        return true;
    }

    private void GetPurpleSurfaceAnchor(
        Collision2D collision,
        Vector2 jumpDir,
        out Vector2 anchorPoint,
        out Vector2 supportNormal
    )
    {
        if (
            TryGetPurpleSupportAnchor(collision, jumpDir, out anchorPoint, out supportNormal, out _)
        )
            return;

        anchorPoint = collision.transform.position;
        supportNormal = jumpDir;
    }

    private void ResolvePurpleBounce(bool asSuperJump)
    {
        if (_purpleBounceState != PurpleBounceState.Latched)
            return;

        Vector2 jumpDir = GetJumpDirectionVector();
        Vector2 tangentialVelocity = ExtractPerpendicularVelocity(_rb2d.linearVelocity);
        float baseSpeed = asSuperJump ? purpleSuperBaseSpeed : purpleBounceBaseSpeed;
        float impactScale = asSuperJump ? purpleSuperImpactScale : purpleBounceImpactScale;
        float maxSpeed = asSuperJump ? purpleSuperMaxSpeed : purpleBounceMaxSpeed;
        float launchSpeed = Mathf.Min(maxSpeed, baseSpeed + _purpleImpactSpeed * impactScale);
        Vector2 releaseNormal =
            _activePurpleSupportNormal.sqrMagnitude > 0.001f
                ? _activePurpleSupportNormal.normalized
                : jumpDir;
        Vector2 launchVelocity = tangentialVelocity + jumpDir * launchSpeed;
        float launchOutOfSurface = Vector2.Dot(launchVelocity, releaseNormal);
        if (launchOutOfSurface < 0f)
            launchVelocity -= releaseNormal * launchOutOfSurface;

        _rb2d.gravityScale = _defaultGravityScale;
        _rb2d.linearVelocity = launchVelocity;
        _rb2d.position += releaseNormal * purpleLaunchSeparation;
        _numJumps = _maxJumps;
        _jumpNow = false;
        _jumpTriggered = false;
        _pendingJumpMultiplier = 1f;
        _jumpHoldDirection = jumpDir;
        _jumpHoldForce = _jumpForceVec.magnitude * JUMP_HOLD_FORCE_SCALE;
        _jumpHoldActive = asSuperJump && _jumpPressed;

        float volume = Mathf.Lerp(
            0.3f,
            0.95f,
            Mathf.InverseLerp(0f, purpleSuperMaxSpeed, launchSpeed)
        );
        if (asSuperJump)
            _gmRef.soundManager.PlayWithPitchVariance("superJump", jumpPitchVariance, volume);
        else
            _gmRef.soundManager.Play("bounce", volume);

        LogPurpleBounce(
            "Resolve",
            $"{(asSuperJump ? "super" : "normal")} session={_activePurpleSessionId} speed={launchSpeed:F2} consumedPress={_activePurpleConsumedJumpSerial}"
        );
        EndPurpleBounceWindow(
            enterResolvedCooldown: true,
            reason: asSuperJump ? "resolveSuper" : "resolveNormal"
        );
    }

    private void CancelPurpleBounceWindow(
        bool enterResolvedCooldown = false,
        string reason = "cancel"
    )
    {
        if (_purpleBounceState == PurpleBounceState.Latched)
        {
            EndPurpleBounceWindow(enterResolvedCooldown, reason);
            return;
        }

        if (_purpleBounceState == PurpleBounceState.ResolvedCooldown)
        {
            ClearResolvedPurpleCooldown(reason);
            return;
        }
    }

    private void EndPurpleBounceWindow(bool enterResolvedCooldown = false, string reason = "end")
    {
        Collider2D endingCollider = _activePurpleCollider;
        Tile_Purple endingTile = _activePurpleTile;
        int endingSessionId = _activePurpleSessionId;
        bool hadOwner = endingCollider != null || endingTile != null;

        LogPurpleBounce(
            "End",
            $"reason={reason} session={endingSessionId} collider={endingCollider?.name ?? "null"}"
        );

        endingTile?.EndDeformation();
        ResetActivePurpleSessionState();

        if (enterResolvedCooldown && hadOwner)
            EnterResolvedPurpleCooldown(endingCollider, endingTile, endingSessionId, reason);

        RestoreDefaultGravityScale();
        UpdatePurpleBounceVisual();
    }

    private void EnterResolvedPurpleCooldown(
        Collider2D collider,
        Tile_Purple tile,
        int sessionId,
        string reason
    )
    {
        if (collider == null && tile == null)
            return;

        _purpleBounceState = PurpleBounceState.ResolvedCooldown;
        _resolvedPurpleCollider = collider;
        _resolvedPurpleTile = tile;
        _resolvedPurpleSessionId = sessionId;
        _resolvedPurpleUntilTime = Time.time + Mathf.Max(0f, purpleResolvedRelatchCooldownDuration);
        _resolvedPurpleExitObserved = false;
        _resolvedPurpleUsesStrictRelatch = reason != "resolveNormal" && reason != "resolveSuper";
        _resolvedPurpleLastSeparation = 0f;
        LogPurpleBounce(
            "EnterCooldown",
            $"reason={reason} session={sessionId} collider={collider?.name ?? "null"} until={_resolvedPurpleUntilTime:F3} strictRelatch={_resolvedPurpleUsesStrictRelatch}"
        );
    }

    private void UpdateResolvedPurpleCooldown()
    {
        if (_purpleBounceState != PurpleBounceState.ResolvedCooldown)
            return;

        if (Time.time >= _resolvedPurpleUntilTime)
        {
            LogPurpleBounce(
                "CooldownClear",
                $"reason=timer session={_resolvedPurpleSessionId} separation={_resolvedPurpleLastSeparation:F3}"
            );
            ClearResolvedPurpleCooldown("timer");
            return;
        }

        if (HasResolvedPurpleSeparated(out float separation))
        {
            _resolvedPurpleLastSeparation = separation;
            LogPurpleBounce(
                "CooldownClear",
                $"reason=separation session={_resolvedPurpleSessionId} separation={separation:F3}"
            );
            ClearResolvedPurpleCooldown("separation");
            return;
        }

        if (TryGetResolvedPurpleSeparation(out separation))
            _resolvedPurpleLastSeparation = separation;
    }

    private bool CanRelatchResolvedPurple(Collision2D collision, out string relatchReason)
    {
        relatchReason = DescribePurpleCooldownGate();
        if (!MatchesResolvedPurple(collision.collider))
            return true;

        if (_resolvedPurpleExitObserved)
            return true;

        if (
            !_resolvedPurpleUsesStrictRelatch
            && GetResolvedPurpleCooldownRemaining() <= GetResolvedPurpleNearExpiryWindow()
        )
        {
            relatchReason = $"{relatchReason} gate=nearExpiry";
            return true;
        }

        if (HasResolvedPurpleSeparated(out float separation))
        {
            _resolvedPurpleLastSeparation = separation;
            relatchReason = $"{relatchReason} gate=separation";
            return true;
        }

        if (Time.time >= _resolvedPurpleUntilTime)
        {
            relatchReason = $"{relatchReason} gate=cooldownElapsed";
            return true;
        }

        return false;
    }

    private bool MatchesResolvedPurple(Collider2D collider)
    {
        if (collider == null)
            return false;

        Tile_Purple tile = collider.GetComponentInParent<Tile_Purple>();
        bool sameCollider = _resolvedPurpleCollider != null && collider == _resolvedPurpleCollider;
        bool sameTile = _resolvedPurpleTile != null && tile == _resolvedPurpleTile;
        return sameCollider || sameTile;
    }

    private bool HasResolvedPurpleSeparated(out float separation)
    {
        separation = float.PositiveInfinity;
        if (!TryGetResolvedPurpleSeparation(out separation))
            return false;

        return separation >= GetResolvedPurpleRequiredSeparation();
    }

    private bool TryGetResolvedPurpleSeparation(out float separation)
    {
        separation = float.PositiveInfinity;
        if (_groundCheckCollider == null || _resolvedPurpleCollider == null)
            return false;

        if (_groundCheckCollider.IsTouching(_resolvedPurpleCollider))
        {
            separation = 0f;
            return true;
        }

        ColliderDistance2D distance = _groundCheckCollider.Distance(_resolvedPurpleCollider);
        separation = distance.isOverlapped ? 0f : distance.distance;
        return true;
    }

    private void ClearResolvedPurpleCooldown(string reason)
    {
        if (_purpleBounceState == PurpleBounceState.ResolvedCooldown)
        {
            LogPurpleBounce(
                "ExitCooldown",
                $"reason={reason} session={_resolvedPurpleSessionId} collider={_resolvedPurpleCollider?.name ?? "null"}"
            );
            _purpleBounceState = PurpleBounceState.Inactive;
        }

        ResetResolvedPurpleCooldownState();
    }

    private void ResetActivePurpleSessionState()
    {
        _purpleBounceState = PurpleBounceState.Inactive;
        _activePurpleCollider = null;
        _activePurpleTile = null;
        _activePurpleSessionId = 0;
        _activePurpleSessionStartJumpSerial = 0;
        _activePurpleConsumedJumpSerial = 0;
        _purpleBounceTimer = 0f;
        _purpleImpactSpeed = 0f;
        _purpleResolveQueued = false;
        _purpleResolveAsSuperJump = false;
        _activePurpleSupportNormal = Vector2.zero;
        _purpleHeldTangentialDirection = Vector2.zero;
        _purpleHeldTangentialSpeed = 0f;
        _purpleLastFollowOffset = Vector2.zero;
        _activePurpleContactCached = false;
        _activePurpleLostContactSteps = 0;
    }

    private void ResetResolvedPurpleCooldownState()
    {
        _resolvedPurpleCollider = null;
        _resolvedPurpleTile = null;
        _resolvedPurpleSessionId = 0;
        _resolvedPurpleUntilTime = 0f;
        _resolvedPurpleExitObserved = false;
        _resolvedPurpleUsesStrictRelatch = false;
        _resolvedPurpleLastSeparation = 0f;
    }

    private string DescribePurpleCooldownGate()
    {
        float timeRemaining = GetResolvedPurpleCooldownRemaining();
        return $"cooldownRemaining={timeRemaining:F3} exitObserved={_resolvedPurpleExitObserved} strictRelatch={_resolvedPurpleUsesStrictRelatch} separation={_resolvedPurpleLastSeparation:F3} threshold={GetResolvedPurpleRequiredSeparation():F3}";
    }

    private float GetResolvedPurpleCooldownRemaining()
    {
        return Mathf.Max(0f, _resolvedPurpleUntilTime - Time.time);
    }

    private float GetResolvedPurpleNearExpiryWindow()
    {
        return Mathf.Max(PURPLE_RELATCH_NEAR_EXPIRY_WINDOW, Time.fixedDeltaTime);
    }

    private float GetResolvedPurpleRequiredSeparation()
    {
        if (!_resolvedPurpleUsesStrictRelatch)
            return Mathf.Max(0f, purpleResolvedRelatchSeparation);

        return Mathf.Max(0f, purpleResolvedRelatchSeparation + purpleLatchProbeDistance);
    }

    private void RestoreDefaultGravityScale()
    {
        if (_rb2d != null)
            _rb2d.gravityScale = _defaultGravityScale;
    }

    private void UpdatePurpleBounceVisual()
    {
        if (
            _purpleOverlayRenderer == null
            || _purpleOverlayTransform == null
            || _spriteRenderer == null
        )
        {
            if (!_loggedPurpleVisualMissingRefs)
            {
                _loggedPurpleVisualMissingRefs = true;
                LogPurpleBounce("VisualMissingRefs", "missing refs");
            }
            return;
        }

        _loggedPurpleVisualMissingRefs = false;

        SyncPurpleOverlaySprite();

        if (_purpleBounceState != PurpleBounceState.Latched || purpleDeformDuration <= 0f)
        {
            if (_purpleVisualWasVisible)
            {
                LogPurpleBounce("VisualHide");
            }
            _purpleVisualWasVisible = false;
            _spriteRenderer.enabled = true;
            _purpleOverlayRenderer.enabled = false;
            _purpleOverlayTransform.localPosition = Vector3.zero;
            _purpleOverlayTransform.localScale = Vector3.one;
            _purpleOverlayRenderer.color = Color.white;
            return;
        }

        Vector3 localFollowOffset = transform.InverseTransformVector(_purpleLastFollowOffset);
        _spriteRenderer.enabled = false;
        _purpleOverlayRenderer.enabled = true;
        _purpleOverlayRenderer.color = Color.white;
        _purpleOverlayTransform.localScale = Vector3.one;
        _purpleOverlayTransform.localPosition = localFollowOffset;
        if (!_purpleVisualWasVisible)
        {
            LogPurpleBounce("VisualShow");
        }
        _purpleVisualWasVisible = true;
    }

    private void InitializePurpleOverlay()
    {
        GameObject overlayGO = new GameObject("PurpleBounceOverlay");
        _purpleOverlayTransform = overlayGO.transform;
        _purpleOverlayTransform.SetParent(transform, false);

        _purpleOverlayRenderer = overlayGO.AddComponent<SpriteRenderer>();
        _purpleOverlayRenderer.enabled = false;
        if (_spriteRenderer != null)
        {
            _purpleOverlayRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            _purpleOverlayRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }

        LogPurpleBounce("OverlayInit");
    }

    private void SyncPurpleOverlaySprite()
    {
        if (_purpleOverlayRenderer == null || _spriteRenderer == null)
            return;

        _purpleOverlayRenderer.sprite = _spriteRenderer.sprite;
        _purpleOverlayRenderer.flipX = _spriteRenderer.flipX;
        _purpleOverlayRenderer.flipY = _spriteRenderer.flipY;
        if (_purpleOverlayRenderer.sprite == null)
            LogPurpleBounce("OverlaySprite", "sprite is null after sync");
    }

    private bool IsPurpleCollider(Collider2D other)
    {
        return other != null && other.GetComponentInParent<Tile_Purple>() != null;
    }

    private bool HasActivePurpleContact(out string contactSource, out float contactSeparation)
    {
        contactSource = "none";
        contactSeparation = float.PositiveInfinity;

        if (_activePurpleCollider == null)
            return false;

        if (_activePurpleContactCached)
        {
            contactSource = "callback";
            contactSeparation = 0f;
            return true;
        }

        if (_groundCheckCollider == null)
            return false;

        if (_groundCheckCollider.IsTouching(_activePurpleCollider))
        {
            contactSource = "touching";
            contactSeparation = 0f;
            return true;
        }

        ColliderDistance2D distance = _groundCheckCollider.Distance(_activePurpleCollider);
        contactSeparation = distance.distance;
        if (distance.isOverlapped)
        {
            contactSource = "distanceOverlap";
            contactSeparation = 0f;
            return true;
        }

        if (distance.distance <= purpleLatchProbeDistance)
        {
            contactSource = "probe";
            return true;
        }

        return false;
    }

    private Vector2 ClampPurpleFollowDeltaAgainstSolids(Vector2 followDelta)
    {
        if (_groundCheckCollider == null)
            return followDelta;

        float distance = followDelta.magnitude;
        if (distance <= 0.0001f)
            return Vector2.zero;

        Vector2 direction = followDelta / distance;
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false,
        };
        int hitCount = _groundCheckCollider.Cast(
            direction,
            filter,
            _purpleFollowCastHits,
            distance
        );
        float allowedDistance = distance;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _purpleFollowCastHits[i].collider;
            if (hitCollider == null || hitCollider == _groundCheckCollider)
                continue;
            if (hitCollider.isTrigger || IsPurpleCollider(hitCollider))
                continue;

            allowedDistance = Mathf.Min(
                allowedDistance,
                Mathf.Max(0f, _purpleFollowCastHits[i].distance - PURPLE_FOLLOW_CAST_SKIN)
            );
        }

        if (allowedDistance <= 0f)
            return Vector2.zero;

        return direction * allowedDistance;
    }

    private Vector2 GetJumpDirectionVector()
    {
        return -GravityDirectionVector();
    }

    private Vector2 ExtractPerpendicularVelocity(Vector2 velocity)
    {
        Vector2 gravityDir = GravityDirectionVector();
        return velocity - gravityDir * Vector2.Dot(velocity, gravityDir);
    }

    private Vector2 ProjectPurpleFollowOffset(Vector2 followOffset)
    {
        Vector2 followAxis =
            _activePurpleSupportNormal.sqrMagnitude > 0.001f
                ? _activePurpleSupportNormal.normalized
                : GetJumpDirectionVector();
        if (followAxis.sqrMagnitude <= 0.001f)
            return Vector2.zero;

        return followAxis * Vector2.Dot(followOffset, followAxis);
    }

    private string FormatVector2(Vector2 value)
    {
        return $"({value.x:F2},{value.y:F2})";
    }

    private string DescribePurpleState()
    {
        return $"state={_purpleBounceState} activeSession={_activePurpleSessionId} active={_activePurpleCollider?.name ?? "null"} cached={_activePurpleContactCached} lostSteps={_activePurpleLostContactSteps} consumedPress={_activePurpleConsumedJumpSerial} cooldownSession={_resolvedPurpleSessionId} cooldown={_resolvedPurpleCollider?.name ?? "null"} {DescribePurpleCooldownGate()} vel={FormatVector2(_rb2d != null ? _rb2d.linearVelocity : Vector2.zero)} pre={FormatVector2(_prePhysicsVelocity)} follow={FormatVector2(_purpleLastFollowOffset)}";
    }

    private string FormatImpactDetails(
        Collision2D collision,
        string phase,
        float relativeImpact,
        float bodyImpact,
        float cachedImpact
    )
    {
        return $"{collision.collider.name} phase={phase} contacts={collision.contactCount} rel={relativeImpact:F2} body={bodyImpact:F2} cached={cachedImpact:F2} rv={FormatVector2(collision.relativeVelocity)} vel={FormatVector2(_rb2d != null ? _rb2d.linearVelocity : Vector2.zero)} pre={FormatVector2(_prePhysicsVelocity)}";
    }

    private void LogPurpleBounce(string eventName, string message = null)
    {
        if (!debugPurpleBounceLogging)
            return;

        string normalizedMessage = string.IsNullOrWhiteSpace(message) ? null : message;
        string signature = $"{eventName}|{normalizedMessage}";
        if (
            signature == _lastPurpleLogSignature
            && Time.unscaledTime - _lastPurpleLogTime < PURPLE_LOG_REPEAT_WINDOW
        )
        {
            return;
        }

        _lastPurpleLogSignature = signature;
        _lastPurpleLogTime = Time.unscaledTime;
        if (normalizedMessage == null)
            Debug.Log($"[PurpleBounce] [{eventName}]", this);
        else
            Debug.Log($"[PurpleBounce] [{eventName}] {normalizedMessage}", this);
    }

    private bool ProbeForNonIceGround()
    {
        if (groundProbeDistance <= 0f || _groundCheckCollider == null)
            return false;

        int playerLayer = gameObject.layer;
        Vector2 origin = _groundCheckCollider.bounds.center;
        Vector2 dir = GravityDirectionVector();
        float castDistance = GetProbeDistance(dir);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, castDistance);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == _groundCheckCollider)
                continue;
            if (hit.collider.isTrigger)
                continue;
            if (hit.collider.gameObject.layer != playerLayer)
                continue; // ignore other physics layers (e.g., warps)
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile == null)
                continue; // only solid tiles count as ground
            if (tile is Tile_Blue)
                continue;
            return true;
        }

        return false;
    }

    private void ApplyJumpHoldForce()
    {
        if (!_jumpHoldActive)
            return;

        float alongJump = Vector2.Dot(_rb2d.linearVelocity, _jumpHoldDirection);
        if (!_jumpPressed)
        {
            if (alongJump > 0f)
            {
                Vector2 vel = _rb2d.linearVelocity;
                Vector2 proj = _jumpHoldDirection * alongJump;
                Vector2 tangent = vel - proj;
                _rb2d.linearVelocity = tangent + proj * SHORT_HOP_RELEASE_DAMP; // short-hop cut
            }
            _jumpHoldActive = false;
            return;
        }

        if (alongJump <= 0f)
        {
            _jumpHoldActive = false;
            return;
        }

        _rb2d.AddForce(_jumpHoldDirection * _jumpHoldForce * Time.fixedDeltaTime);
    }

    private float ComputeJumpSpeedMultiplier()
    {
        float perpendicularSpeed = GetPerpendicularSpeed();
        float t = Mathf.Clamp01(perpendicularSpeed / SPEED_FOR_MAX_BONUS);
        return 1f + t * MAX_SPEED_JUMP_BONUS; // modest speed-to-jump bonus
    }

    private void ApplyTorqueForMovement(Vector2 movement)
    {
        // Use the gravity-perpendicular component only; diagonals count the same as straight input
        float rollInput = GetPerpendicularComponent(_moveInput);
        if (Mathf.Abs(rollInput) <= 0.01f)
            return;
        rollInput = Mathf.Sign(rollInput); // keyboard or stick diagonals give full torque

        float controlScale = isOnIce ? iceControlMultiplier : 1f;
        float torqueSign =
            (
                PlayGM.instance.gravDirection == GravityDirection.Down
                || PlayGM.instance.gravDirection == GravityDirection.Right
            )
                ? -1f
                : 1f;
        float torque = rollInput * torqueStrength * controlScale * torqueSign; // keeps spin matching roll per gravity side
        _rb2d.AddTorque(torque, ForceMode2D.Force);
        _rb2d.angularVelocity = Mathf.Clamp(
            _rb2d.angularVelocity,
            -MAX_ANGULAR_VELOCITY,
            MAX_ANGULAR_VELOCITY
        );
    }

    private float GetProbeDistance(Vector2 dir)
    {
        Bounds b = _groundCheckCollider.bounds;
        float extent = Mathf.Abs(dir.x) > 0.5f ? b.extents.x : b.extents.y;
        return extent + groundProbeDistance + PROBE_SKIN;
    }

    private Vector2 GravityDirectionVector()
    {
        switch (PlayGM.instance.gravDirection)
        {
            case GravityDirection.Down:
                return Vector2.down;
            case GravityDirection.Left:
                return Vector2.left;
            case GravityDirection.Up:
                return Vector2.up;
            case GravityDirection.Right:
                return Vector2.right;
            default:
                return Vector2.down;
        }
    }

    private float GetPerpendicularComponent(Vector2 v)
    {
        switch (PlayGM.instance.gravDirection)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                return v.x;
            case GravityDirection.Left:
            case GravityDirection.Right:
                return v.y;
            default:
                return 0f;
        }
    }

    private float GetPerpendicularSpeed()
    {
        Vector2 v = _rb2d.linearVelocity;
        switch (PlayGM.instance.gravDirection)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                v.y = 0f;
                break;
            case GravityDirection.Left:
            case GravityDirection.Right:
                v.x = 0f;
                break;
        }

        return v.magnitude;
    }

    private Vector2 GetPerpendicularUnit(float sign)
    {
        switch (PlayGM.instance.gravDirection)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                return new Vector2(sign, 0f);
            case GravityDirection.Left:
            case GravityDirection.Right:
                return new Vector2(0f, sign);
            default:
                return Vector2.zero;
        }
    }

    public GravityDirection GetGravityDirection()
    {
        return _gmRef.gravDirection;
    }

    void OnDrawGizmosSelected()
    {
        if (_groundCheckCollider == null)
            return;

        Vector2 origin = _groundCheckCollider.bounds.center;
        Vector2 dir = GravityDirectionVector();
        float castDistance =
            _groundCheckCollider != null ? GetProbeDistance(dir) : groundProbeDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + dir * castDistance); // shows ground override ray
    }

    public void SetSpawnJumpCooldown()
    {
        _numJumps = _maxJumps;
        _jumpTriggered = false;
        _jumpNow = false;
        _jumpHoldActive = false;
        _jumpPressed = false;
        CancelPurpleBounceWindow();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled)
        {
            _moveInput = Vector2.zero;
            _jumpPressed = false;
            _jumpTriggered = false;
            _jumpHoldActive = false;
            _jumpNow = false;
            _suppressJumpUntilRelease = false;
            CancelPurpleBounceWindow();
        }
    }

    public void SuppressJumpUntilRelease()
    {
        _suppressJumpUntilRelease = true;
        _jumpTriggered = false;
        _jumpNow = false;
        CancelPurpleBounceWindow();
    }
}
