using System.Collections.Generic;
using circleXsquares;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    // public variables
    public int speed = 420;
    public float jumpForce = 600;
    public bool isOnIce;
    public bool isIceScalingBlockingJump;
    public Collider2D purpleGroundCheckCollider;
    public bool queueSuperJumpOnPurpleTouch = false;
    public BallSkinDatabase skinDB;

    // Tweakables (feels good around these defaults; push slightly to taste)
    public float groundProbeDistance = 0.02f; // 0.2 was too long
    public float torqueStrength = 8f; // 6–10 feels controlled
    public float iceControlMultiplier = 0.5f; // lower = slipperier (0.35–0.6)
    public float iceSlideBoost = 6f; // add carry on ice (4–8 range)

    // private references
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private PlayGM _gmRef;
    private Collider2D _groundCheckCollider;
    private Rigidbody2D _rb2d;

    // private variables
    private Vector2 _jumpForceVec;
    private bool _jumpNow = false;
    private int _maxJumps = 1;
    private int _numJumps;
    public HashSet<Collider2D> recentlyTouchedPurpleTiles = new();
    private readonly Dictionary<Collider2D, float> _purpleTouchTimes = new();
    private const float PURPLE_TOUCH_TIMEOUT = 0.75f;
    private const float MAX_SPEED_JUMP_BONUS = 0.15f;
    private const float SPEED_FOR_MAX_BONUS = 10f;
    private const float JUMP_HOLD_FORCE_SCALE = 0.33f;
    private const float SHORT_HOP_RELEASE_DAMP = 0.55f;
    private const float MAX_ANGULAR_VELOCITY = 380f;
    private bool purpTucher => recentlyTouchedPurpleTiles.Count > 0;
    private float _pendingJumpMultiplier = 1f;
    private bool _jumpHoldActive;
    private float _jumpHoldForce;
    private Vector2 _jumpHoldDirection;
    private bool _jumpTriggered;
    private bool _groundOverrideJumpBlock;
    private const float PROBE_SKIN = 0.01f;

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
        _jumpForceVec = new Vector2(0.0f, jumpForce);
        _groundCheckCollider = GetComponent<Collider2D>();
        _audioSource = GetComponent<AudioSource>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // use the global InputManager's shared controls
        _controls = InputManager.Instance.Controls;

        InputControls.PlayerActions player = _controls.Player;

        _onMove = ctx => _moveInput = ctx.ReadValue<Vector2>();
        _onMoveCanceled = _ => _moveInput = Vector2.zero;
        _onJump = _ =>
        {
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
        int index = PlayerPrefs.GetInt("SelectedBallSkin", 0);
        _spriteRenderer.sprite = skinDB.skins[index];
        _jumpPressed = false;
        _jumpTriggered = false;
        _jumpHoldActive = false;
        UpdateJumpForce();
    }

    void Update()
    {
        _groundOverrideJumpBlock = ProbeForNonIceGround(); // only used to override ice blocking
        UpdateJumpForce();
        CleanupStalePurpleTouches();
        UpdateJumping();
        UnityEditorGodMode();
        UpdateRollingSound();
    }

    void FixedUpdate()
    {
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

        if (other.collider.name.Contains("Purple"))
        {
            recentlyTouchedPurpleTiles.Add(other.collider);
            _purpleTouchTimes[other.collider] = Time.time;
            if (queueSuperJumpOnPurpleTouch)
            {
                _gmRef.soundManager.Play("superJump");
                queueSuperJumpOnPurpleTouch = false;
                _jumpNow = true;
            }
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (other.collider.name.Contains("Purple"))
        {
            recentlyTouchedPurpleTiles.Add(other.collider);
            _purpleTouchTimes[other.collider] = Time.time;
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.collider.name.Contains("Purple"))
        {
            recentlyTouchedPurpleTiles.Remove(other.collider);
            _purpleTouchTimes.Remove(other.collider);
        }
    }

    /* Input System-based replacements */

    public void Move()
    {
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
        bool canJump = _numJumps < _maxJumps;
        if (_numJumps == 0)
        {
            canJump = canJump && _groundCheckCollider.IsTouchingLayers();
        }

        canJump =
            canJump
            || (
                purpleGroundCheckCollider.GetComponent<JumpProximityZone>().IsNearPurple
                && purpTucher
            );

        if (
            purpleGroundCheckCollider.GetComponent<JumpProximityZone>().IsNearPurple
            && !purpTucher
            && _jumpTriggered
        )
        {
            queueSuperJumpOnPurpleTouch = true;
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
            _gmRef.soundManager.Play("jump");
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
        if (_gmRef.victoryAchieved)
        {
            _audioSource.volume = 0f;
            return;
        }

        float volume = _gmRef.SlideIntensityToVolume(_rb2d.linearVelocity, Physics2D.gravity);
        if (!_groundCheckCollider.IsTouchingLayers())
            volume = 0.0f;
        _audioSource.volume = volume;
    }

    private void CleanupStalePurpleTouches()
    {
        if (_purpleTouchTimes.Count == 0)
            return;

        float now = Time.time;
        List<Collider2D> toRemove = null;

        foreach (KeyValuePair<Collider2D, float> entry in _purpleTouchTimes)
        {
            if (now - entry.Value > PURPLE_TOUCH_TIMEOUT)
            {
                toRemove ??= new List<Collider2D>();
                toRemove.Add(entry.Key);
            }
        }

        if (toRemove == null)
            return;

        foreach (Collider2D col in toRemove)
        {
            _purpleTouchTimes.Remove(col);
            recentlyTouchedPurpleTiles.Remove(col);
        }
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
    }
}
