using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using circleXsquares;

public class Player_Controller : MonoBehaviour
{

    public int speed;
    public float jumpForce;

    private PlayGM gm_ref;

    private int max_jumps = 1;
    private int num_jumps;
    private bool jump_now = false;
    private Collider2D ground_check_collider;

    private Rigidbody2D rb2d;
    private Vector2 jump_force_vec;

    private bool godMode = false;

    private AudioSource audioSource;

    void Awake()
    {
        rb2d = gameObject.GetComponent<Rigidbody2D>();
        jump_force_vec = new Vector2(0.0f, jumpForce);
        ground_check_collider = gameObject.GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        gm_ref = PlayGM.instance;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateJumping();
        UpdateGravity();
        UpdateGodMode();
        UpdateRollingSound();
    }

    void FixedUpdate()
    {
        Move();
        Jump();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        num_jumps = 0;
    }

    public void UpdateJumpForce(PlayGM.GravityDirection gd)
    {
        switch (gd)
        {
            case PlayGM.GravityDirection.Down:
                jump_force_vec = new Vector2(0.0f, jumpForce);
                break;
            case PlayGM.GravityDirection.Left:
                jump_force_vec = new Vector2(jumpForce, 0.0f);
                break;
            case PlayGM.GravityDirection.Up:
                jump_force_vec = new Vector2(0.0f, -jumpForce);
                break;
            case PlayGM.GravityDirection.Right:
                jump_force_vec = new Vector2(-jumpForce, 0.0f);
                break;
            default:
                return;
        }
    }

    public void UpdateJumping()
    {
        bool canJump = (num_jumps < max_jumps);
        if (num_jumps == 0)
        {
            canJump = canJump && ground_check_collider.IsTouchingLayers();
        }

        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            // increment numJumps
            num_jumps++;
            // jump now!
            jump_now = true;
            // play sound
            gm_ref.soundManager.Play("jump");
        }
    }

    void UpdateGravity()
    {
        // God Mode Gravity Control
        if (!godMode) return;

        // Gravity Down
        if (Input.GetKeyDown(KeyCode.K))
        {
            gm_ref.soundManager.Play("gravity");
            Physics2D.gravity = new Vector2(0.0f, -9.81f);
            this.UpdateJumpForce(PlayGM.GravityDirection.Down);
        }

        // Gravity left
        if (Input.GetKeyDown(KeyCode.J))
        {
            gm_ref.soundManager.Play("gravity");
            Physics2D.gravity = new Vector2(-9.81f, 0.0f);
            this.UpdateJumpForce(PlayGM.GravityDirection.Left);
        }

        // Gravity Up
        if (Input.GetKeyDown(KeyCode.I))
        {
            gm_ref.soundManager.Play("gravity");
            Physics2D.gravity = new Vector2(0.0f, 9.81f);
            this.UpdateJumpForce(PlayGM.GravityDirection.Up);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            gm_ref.soundManager.Play("gravity");
            Physics2D.gravity = new Vector2(9.81f, 0.0f);
            this.UpdateJumpForce(PlayGM.GravityDirection.Right);
        }
    }


    void UpdateGodMode()
    {
        // Toggle God Mode on G key press
        if (Input.GetKeyDown(KeyCode.G))
        {
            gm_ref.soundManager.Play("gravity");
            godMode = !godMode;
        }

    }

    void UpdateRollingSound()
    {
        float volume = gm_ref.SlideIntensityToVolume(rb2d.velocity, Physics2D.gravity);
        if (!ground_check_collider.IsTouchingLayers()) volume = 0.0f;
        audioSource.volume = volume;
    }

    void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);
        rb2d.AddForce(movement * speed * Time.deltaTime);
    }

    public void Jump()
    {
        if (jump_now)
        {
            // jump by force (acceleration)
            rb2d.AddForce(jump_force_vec);

            // jump logic
            jump_now = false;
        }
    }
}
