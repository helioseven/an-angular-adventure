using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Player_Controller : MonoBehaviour {

	public int speed = 150;
	public float jumpForce = 1000f;
	public float verticalMovementFactor = 0.25f;

	private PlayGM gm_ref;

	private int max_jumps = 1;
	private int num_jumps;
	private bool jump_now = false;
	private Collider2D ground_check_collider;

	private Rigidbody2D rb2d;
	private Vector2 jump_force_vec;

	// Use this for initialization
	void Awake () {
		gm_ref = PlayGM.instance;

		rb2d = gameObject.GetComponent<Rigidbody2D> ();
		jump_force_vec = new Vector2(0.0f , jumpForce);
		ground_check_collider = gameObject.GetComponent<Collider2D>();
	}

	// Update is called once per frame
	void Update () {
		UpdateJumping();
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

	public void ResetJumpForce()
	{
		jump_force_vec = new Vector2(0f, 0f);
		/*
		jump_force_vec = new Vector2(0.0f, jumpForce);
		jump_force_vec = new Vector2(jumpForce, 0.0f);
		jump_force_vec = new Vector2(0.0f, -jumpForce);
		jump_force_vec = new Vector2(-jumpForce, 0.0f);
		*/
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
		}
	}

	public void Move()
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVertical = Input.GetAxis("Vertical");

		PlayGM.GravityDirection gd = gm_ref.gravDirection;
		// apply vertical movement factor to slow down toward and against gravity movement
		if (gd == PlayGM.GravityDirection.Down || gd == PlayGM.GravityDirection.Up)
			moveVertical = moveVertical * verticalMovementFactor;
		if (gd == PlayGM.GravityDirection.Left || gd == PlayGM.GravityDirection.Right)
			moveHorizontal = moveHorizontal * verticalMovementFactor;

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
