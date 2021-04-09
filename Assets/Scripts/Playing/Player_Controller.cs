using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class Player_Controller : MonoBehaviour {

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

	private float maxVolume = 0.3f;
	private float volumeMultiplier = 0.07f;

	private AudioSource audioSource;

	void Awake ()
	{
		rb2d = gameObject.GetComponent<Rigidbody2D> ();
		jump_force_vec = new Vector2(0.0f , jumpForce);
		ground_check_collider = gameObject.GetComponent<Collider2D>();
		audioSource = GetComponent<AudioSource>();
	}

	void Start ()
	{
		gm_ref = PlayGM.instance;
	}

	// Update is called once per frame
	void Update ()
	{
		UpdateJumping();
		UpdateGravity();
		UpdateGodMode();
		UpdateRollingSound();
	}

	void FixedUpdate ()
	{
		Move();
		Jump();
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		num_jumps = 0;

		if (other.gameObject.tag.Equals ("Purple"))
		{
			Vector2 vel = other.relativeVelocity;
			Vector2 grav = Physics2D.gravity;

			// dot product calculates projection of velocity vector onto gravity vector
			float bounceForce = grav.x * vel.x + grav.y * vel.y;
			bounceForce = Mathf.Abs(bounceForce) / 10.0f;

			float intensity = Mathf.Clamp(bounceForce * volumeMultiplier, 0f, maxVolume);
			// Debug.Log ("intensity: " + intensity + "     \t bounceForce: " + bounceForce);
			SoundManagerScript.PlayOneShotSound ("bounce", intensity);
		}
	}

	public void UpdateJumpForce(PlayGM.GravityDirection gd)
	{
		switch (gd) {
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
		if (num_jumps == 0 ) {
			canJump = canJump && ground_check_collider.IsTouchingLayers();
		}

		if (canJump && Input.GetKeyDown(KeyCode.Space)) {
			// increment numJumps
			num_jumps++;
			// jump now!
			jump_now = true;
			// play sound
			SoundManagerScript.PlayOneShotSound("jump");
		}
	}

	void UpdateGravity()
	{
		// God Mode Gravity Control
		if (!godMode) return;
		
		// Gravity Down
		if (Input.GetKeyDown(KeyCode.K)) {
			SoundManagerScript.PlayOneShotSound("gravity");
			Physics2D.gravity = new Vector2(0.0f, -9.81f);
			this.UpdateJumpForce(PlayGM.GravityDirection.Down); 
		}

		// Gravity left
		if (Input.GetKeyDown(KeyCode.J)) {
			SoundManagerScript.PlayOneShotSound("gravity");
			Physics2D.gravity = new Vector2(-9.81f, 0.0f);
			this.UpdateJumpForce(PlayGM.GravityDirection.Left);
		}

		// Gravity Up
		if (Input.GetKeyDown(KeyCode.I)) {
			SoundManagerScript.PlayOneShotSound("gravity");
			Physics2D.gravity = new Vector2(0.0f, 9.81f);
			this.UpdateJumpForce(PlayGM.GravityDirection.Up);
		}

		if (Input.GetKeyDown(KeyCode.L)) {
			SoundManagerScript.PlayOneShotSound("gravity");
			Physics2D.gravity = new Vector2(9.81f, 0.0f);
			this.UpdateJumpForce(PlayGM.GravityDirection.Right);
		}
	}


	void UpdateGodMode()
	{
		// Toggle God Mode on G key press
		if (Input.GetKeyDown(KeyCode.G)) {
			SoundManagerScript.PlayOneShotSound("gravity");
			godMode = !godMode;
		}

	}

	void UpdateRollingSound()
	{
		Vector2 vel = rb2d.velocity;
		Vector2 grav = Physics2D.gravity;

		// dot product calculates projection of velocity vector onto vector perpendicular gravity vector
		float slideForce = grav.x * vel.y + grav.y * vel.x;
		slideForce = Mathf.Abs(slideForce) / 10.0f;
		float intensity = Mathf.Clamp(slideForce * volumeMultiplier, 0f, maxVolume);

		// Debug.Log ("Player RollingSound intensity: " + intensity + "\t slideForce: " + slideForce);
		if (!ground_check_collider.IsTouchingLayers()) intensity = 0.0f;
		audioSource.volume = intensity;
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
			rb2d.AddForce ( jump_force_vec );

			// jump logic
			jump_now = false;
		}
	}
}
