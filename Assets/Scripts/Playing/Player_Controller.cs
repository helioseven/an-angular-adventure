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
    private bool purpTucher => recentlyTouchedPurpleTiles.Count > 0;

    // ✅ new Input System
    private PlayerControls _controls;
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _jumpHeld;

    void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        _jumpForceVec = new Vector2(0.0f, jumpForce);
        _groundCheckCollider = GetComponent<Collider2D>();
        _audioSource = GetComponent<AudioSource>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // ✅ setup input
        _controls = new PlayerControls();
        _controls.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _controls.Player.Move.canceled += _ => _moveInput = Vector2.zero;
        _controls.Player.Jump.started += _ => _jumpPressed = true;
        _controls.Player.Jump.canceled += _ => _jumpPressed = false;
        _controls.Player.Jump.performed += _ => _jumpHeld = true;
        _controls.Player.Jump.canceled += _ => _jumpHeld = false;
    }

    void OnEnable() => _controls.Enable();

    void OnDisable() => _controls.Disable();

    void Start()
    {
        _gmRef = PlayGM.instance;
        int index = PlayerPrefs.GetInt("SelectedBallSkin", 0);
        _spriteRenderer.sprite = skinDB.skins[index];
    }

    void Update()
    {
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

    void OnCollisionEnter2D(Collision2D other)
    {
        _numJumps = 0;

        if (other.collider.name.Contains("Purple"))
        {
            recentlyTouchedPurpleTiles.Add(other.collider);
            if (queueSuperJumpOnPurpleTouch)
            {
                _gmRef.soundManager.Play("superJump");
                queueSuperJumpOnPurpleTouch = false;
                _jumpNow = true;
            }
        }
    }

    /* Input System–based replacements */

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

    public PlayGM.GravityDirection GetGravityDirection()
    {
        return _gmRef.gravDirection;
    }
}
