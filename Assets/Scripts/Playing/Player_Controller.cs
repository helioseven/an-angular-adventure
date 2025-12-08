using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    // public variables
    public int speed;
    public float jumpForce;
    public bool isOnIce;
    public bool isIceScalingBlockingJump;
    public Collider2D purpleGroundCheckCollider;
    public bool queueSuperJumpOnPurpleTouch = false;
    public BallSkinDatabase skinDB;

    // private references
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private PlayGM _gmRef;
    private Collider2D _groundCheckCollider;
    private Rigidbody2D _rb2d;

    // private variables
    private const float JUMP_FORCE_DEFAULT_VALUE = 420f;
    private Vector2 _jumpForceVec;
    private bool _jumpNow = false;
    private int _maxJumps = 1;
    private int _numJumps;
    public HashSet<Collider2D> recentlyTouchedPurpleTiles = new();
    private readonly Dictionary<Collider2D, float> _purpleTouchTimes = new();
    private const float PURPLE_TOUCH_TIMEOUT = 0.75f;
    private bool purpTucher => recentlyTouchedPurpleTiles.Count > 0;

    // New Input System
    private InputControls _controls;
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private System.Action<InputAction.CallbackContext> _onMove;
    private System.Action<InputAction.CallbackContext> _onMoveCanceled;
    private System.Action<InputAction.CallbackContext> _onJump;
    private System.Action<InputAction.CallbackContext> _onJumpCanceled;

    // private bool _jumpHeld;

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
        _onJump = _ => _jumpPressed = true;
        _onJumpCanceled = _ => _jumpPressed = false;

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
    }

    void Update()
    {
        CleanupStalePurpleTouches();
        UpdateJumping();
        UnityEditorGodMode();
        UpdateRollingSound();
        UpdateJumpForce();
    }

    void FixedUpdate()
    {
        Move();
        Jump();
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
        Vector2 movement = _moveInput;
        Vector2 upwardDragForcedMovement = UpdateUpwardDragForce(movement);
        _rb2d.AddForce(upwardDragForcedMovement * speed * Time.deltaTime);
    }

    public void Jump()
    {
        if (_jumpNow)
        {
            _rb2d.AddForce(_jumpForceVec);
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
            && _jumpPressed
        )
        {
            queueSuperJumpOnPurpleTouch = true;
        }

        if (canJump && _jumpPressed)
        {
            _numJumps++;
            _jumpNow = true;
            _gmRef.soundManager.Play("jump");
        }
    }

    public void UpdateJumpForce()
    {
        jumpForce = isIceScalingBlockingJump ? 0f : JUMP_FORCE_DEFAULT_VALUE;
        UpdateJumpForceVector(PlayGM.instance.gravDirection);
    }

    public void UpdateJumpForceVector(PlayGM.GravityDirection gd)
    {
        switch (gd)
        {
            case PlayGM.GravityDirection.Down:
                _jumpForceVec = new Vector2(0.0f, jumpForce);
                break;
            case PlayGM.GravityDirection.Left:
                _jumpForceVec = new Vector2(jumpForce, 0.0f);
                break;
            case PlayGM.GravityDirection.Up:
                _jumpForceVec = new Vector2(0.0f, -jumpForce);
                break;
            case PlayGM.GravityDirection.Right:
                _jumpForceVec = new Vector2(-jumpForce, 0.0f);
                break;
        }
    }

    public Vector2 UpdateUpwardDragForce(Vector2 inMovement)
    {
        float dragForce = 0.2f;
        Vector2 outMovement = inMovement;

        switch (PlayGM.instance.gravDirection)
        {
            case PlayGM.GravityDirection.Down:
                outMovement = new Vector2(
                    inMovement.x,
                    inMovement.y > 0f ? (inMovement.y * dragForce) : inMovement.y
                );
                break;
            case PlayGM.GravityDirection.Left:
                outMovement = new Vector2(
                    inMovement.x > 0f ? (inMovement.x * dragForce) : inMovement.x,
                    inMovement.y
                );
                break;
            case PlayGM.GravityDirection.Up:
                outMovement = new Vector2(
                    inMovement.x,
                    inMovement.y < 0f ? (inMovement.y * dragForce) : inMovement.y
                );
                break;
            case PlayGM.GravityDirection.Right:
                outMovement = new Vector2(
                    inMovement.x < 0f ? (inMovement.x * dragForce) : inMovement.x,
                    inMovement.y
                );
                break;
        }

        return outMovement;
    }

    public void UnityEditorGodMode()
    {
        // Gravity debug keys kept for editor desktop testing
#if UNITY_EDITOR
        if (Keyboard.current.kKey.wasPressedThisFrame)
            _gmRef.SetGravity(PlayGM.GravityDirection.Down);
        if (Keyboard.current.jKey.wasPressedThisFrame)
            _gmRef.SetGravity(PlayGM.GravityDirection.Left);
        if (Keyboard.current.iKey.wasPressedThisFrame)
            _gmRef.SetGravity(PlayGM.GravityDirection.Up);
        if (Keyboard.current.lKey.wasPressedThisFrame)
            _gmRef.SetGravity(PlayGM.GravityDirection.Right);
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

    public PlayGM.GravityDirection GetGravityDirection()
    {
        return _gmRef.gravDirection;
    }
}
