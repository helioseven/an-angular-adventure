using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    // public variables
    public int speed;
    public float jumpForce;
    public bool isOnIce;
    public bool isIceScalingBlockingJump;
    public Collider2D purpleGroundCheckCollider;

    // private references
    private AudioSource _audioSource;
    private PlayGM _gmRef;
    private Collider2D _groundCheckCollider;
    private Rigidbody2D _rb2d;

    // private variables
    private const float JUMP_FORCE_DEFAULT_VALUE = 420f;
    private bool _godMode = false;
    private Vector2 _jumpForceVec;
    private bool _jumpNow = false;
    private int _maxJumps = 1;
    private int _numJumps;

    void Awake()
    {
        _rb2d = gameObject.GetComponent<Rigidbody2D>();
        _jumpForceVec = new Vector2(0.0f, jumpForce);
        _groundCheckCollider = gameObject.GetComponent<Collider2D>();
        _audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        _gmRef = PlayGM.instance;
    }

    void Update()
    {
        UpdateJumping();
        UpdateGravity();
        UpdateGodMode();
        UpdateRollingSound();
        UpdateJumpForce();
    }

    void FixedUpdate()
    {
        Move();
        Jump();
    }

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        _numJumps = 0;
    }

    /* Public Functions */

    public void Jump()
    {
        if (_jumpNow)
        {
            // jump by force (acceleration)
            _rb2d.AddForce(_jumpForceVec);

            // jump logic
            _jumpNow = false;
        }
    }

    public void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        Vector2 upwardDragForcedMovement = UpdateUpwardDragForce(movement);

        // reduce force in oppsite direction of gravity
        _rb2d.AddForce(upwardDragForcedMovement * speed * Time.deltaTime);
    }

    public PlayGM.GravityDirection GetGravityDirection()
    {
        return _gmRef.gravDirection;
    }

    // update "upward drag" force based on current gravity direction
    // this is to prevent hovering while allowing a higher speed (original 420 -> target: 600+)
    public Vector2 UpdateUpwardDragForce(Vector2 inMovement)
    {
        float dragForce = 0.2f; // 0 would be no upward mobility, 1 would be full upward mobility.

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

    // update jump force based on current gravity direction
    public void UpdateJumpForce()
    {
        // update the jumpForce value based on ice block toggle to 0 or default
        jumpForce = isIceScalingBlockingJump ? 0f : JUMP_FORCE_DEFAULT_VALUE;
        // use the udpated value in the jump force vector
        UpdateJumpForceVector(PlayGM.instance.gravDirection);
    }

    // update jump force vector based on current gravity direction
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
            default:
                return;
        }
    }

    // update jumping state and number of jumps
    public void UpdateJumping()
    {
        bool canJump = _numJumps < _maxJumps;
        if (_numJumps == 0)
        {
            canJump = canJump && _groundCheckCollider.IsTouchingLayers();
        }

        // purpleGroundCheck override
        canJump =
            canJump || purpleGroundCheckCollider.GetComponent<JumpProximityZone>().IsNearPurple;

        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            // increment numJumps
            _numJumps++;
            // jump now!
            _jumpNow = true;
            // play sound
            _gmRef.soundManager.Play("jump");
        }
    }

    // activates/deactivates God Mode itself
    public void UpdateGodMode()
    {
        // toggle God Mode on G key press
        if (Input.GetKeyDown(KeyCode.G))
        {
            _gmRef.soundManager.Play("gravity");
            _godMode = !_godMode;
        }
    }

    // update gravity force with God Mode if activated
    public void UpdateGravity()
    {
        // God Mode gravity control
        if (!_godMode)
            return;

        // K sets gravity down
        if (Input.GetKeyDown(KeyCode.K))
        {
            _gmRef.SetGravity(PlayGM.GravityDirection.Down);
        }

        // J sets gravity left
        if (Input.GetKeyDown(KeyCode.J))
        {
            _gmRef.SetGravity(PlayGM.GravityDirection.Left);
        }

        // I sets gravity up
        if (Input.GetKeyDown(KeyCode.I))
        {
            _gmRef.SetGravity(PlayGM.GravityDirection.Up);
        }

        // L sets gravity right
        if (Input.GetKeyDown(KeyCode.L))
        {
            _gmRef.SetGravity(PlayGM.GravityDirection.Right);
        }
    }

    // update sound affects for player rolling on surfaces
    public void UpdateRollingSound()
    {
        // disable rolling sounds once victory is achieved
        if (_gmRef.victoryAchieved)
        {
            _audioSource.volume = 0f;
            return;
        }
        // use player velocity to set sound intensity
        float volume = _gmRef.SlideIntensityToVolume(_rb2d.linearVelocity, Physics2D.gravity);
        if (!_groundCheckCollider.IsTouchingLayers())
            volume = 0.0f;
        _audioSource.volume = volume;
    }
}
