using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Player_Controller : MonoBehaviour {

	public int speed = 150;
	public float jumpForce = 1000f;
	public float verticalMovementFactor = 0.25f;

	private int max_jumps = 1;
	private int num_jumps;
	private bool jump_now = false;
	private Collider2D ground_check_collider;

	private enum Gravity_Direction
	{
		down = 0,
		left,
		up,
		right
	}

	private Gravity_Direction grav_dir = Gravity_Direction.down;
	private Rigidbody2D rb2d;
	private Vector2 jump_force_vec;

	// Use this for initialization
	void Awake () {
		rb2d = gameObject.GetComponent<Rigidbody2D> ();
		jump_force_vec = new Vector2(0.0f , jumpForce);
		ground_check_collider = gameObject.GetComponent<Collider2D>();
	}

	// Update is called once per frame
	void Update () {
		UpdateJumping();
		UpdateGravity();
	}


	void FixedUpdate ()
	{
		Move();
		Jump();
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		num_jumps = 0;
	}


	void UpdateJumping()
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
		}
	}

	void UpdateGravity()
	{/*
		// GRAVITY
		//Gravity Down
		if (Input.GetKeyDown(KeyCode.S)) {
			grav_dir = Gravity_Direction.down;
			Physics2D.gravity = new Vector2(0.0f, -9.81f);
			jump_force_vec = new Vector2(0.0f, jumpForce);
		}

		// Gravity left
		if (Input.GetKeyDown(KeyCode.A)) {
			grav_dir = Gravity_Direction.left;
			Physics2D.gravity = new Vector2(-9.81f, 0.0f);
			jump_force_vec = new Vector2(jumpForce, 0.0f);
		}

		// Gravity Up
		if (Input.GetKeyDown(KeyCode.W)) {
			grav_dir = Gravity_Direction.up;
			Physics2D.gravity = new Vector2(0.0f, 9.81f);
			jump_force_vec = new Vector2(0.0f, -jumpForce);
		}

		if (Input.GetKeyDown(KeyCode.D)) {
			grav_dir = Gravity_Direction.right;
			Physics2D.gravity = new Vector2(9.81f, 0.0f);
			jump_force_vec = new Vector2(-jumpForce, 0.0f);
		}
	*/}

	void Move()
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVertical = Input.GetAxis("Vertical");

		// apply vertical movement factor to slow down toward and against gravity movement
		if (grav_dir == Gravity_Direction.down || grav_dir == Gravity_Direction.up)
			moveVertical = moveVertical * verticalMovementFactor;
		if (grav_dir == Gravity_Direction.left || grav_dir == Gravity_Direction.right)
			moveHorizontal = moveHorizontal * verticalMovementFactor;

		Vector2 movement = new Vector2(moveHorizontal, moveVertical);
		rb2d.AddForce(movement * speed * Time.deltaTime);
	}

	void Jump()
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
