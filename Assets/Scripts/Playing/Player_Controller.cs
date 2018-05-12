using UnityEngine;
using System.Collections;

public class Player_Controller : MonoBehaviour {

	public int speed = 150;
	public float jump_force = 0.00001f;
	private int max_jumps = 1;

	public float vertical_movement_factor = 0.25f;

	private int num_jumps;
	private bool jump_now = false;
	private Vector2 jump_force_vec;

	private Collider2D pCol2d;

	private enum Gravity_Direction
	{
		down = 0,
		left,
		up,
		right
	}

	private Gravity_Direction grav_dir = Gravity_Direction.down;

	private Rigidbody2D rb2d;

	void Awake ()
	{
		rb2d = gameObject.GetComponent<Rigidbody2D>();
		jump_force_vec = new Vector2(0.0f, jump_force);
		pCol2d = gameObject.GetComponent<Collider2D>();

	}

	void Update ()
	{
		UpdateJumping();
		UpdateGravity();
	}


	void FixedUpdate ()
	{
		Move ();
		Jump ();
	}

	void OnCollisionEnter2D (Collision2D other)
	{
		num_jumps = 0;
	}


	void UpdateJumping ()
	{
		bool canJump = (num_jumps < max_jumps);
		if (num_jumps == 0 ) {
			canJump = canJump && pCol2d.IsTouchingLayers();
		}

		if (canJump && Input.GetKeyDown(KeyCode.Space)) {
			// increment numJumps
			num_jumps++;
			// jump now!
			jump_now = true;
		}
	}
	
	void UpdateGravity ()
	{
		// GRAVITY
		//Gravity Down
		if (Input.GetKeyDown(KeyCode.S)) {
			grav_dir = Gravity_Direction.down;
			Physics2D.gravity = new Vector2( 0.0f, -9.81f );
			jump_force_vec = new Vector2( 0.0f , jump_force );
		}

		// Gravity Left
		if (Input.GetKeyDown(KeyCode.A)) {
			grav_dir = Gravity_Direction.left;
			Physics2D.gravity = new Vector2( -9.81f, 0.0f );
			jump_force_vec = new Vector2( jump_force, 0.0f );
		}

		// Gravity Up
		if (Input.GetKeyDown(KeyCode.W)) {
			grav_dir = Gravity_Direction.up;
			Physics2D.gravity = new Vector2( 0.0f, 9.81f );
			jump_force_vec = new Vector2( 0.0f, - jump_force );
		}

		// Gravity Right
		if (Input.GetKeyDown(KeyCode.D)) {
			grav_dir = Gravity_Direction.right;
			Physics2D.gravity = new Vector2( 9.81f, 0.0f) ;
			jump_force_vec = new Vector2( - jump_force , 0.0f );
		}
	}

	void Move ()
	{
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		// apply vertical movement factor to slow down toward and against gravity movement
		if (grav_dir == Gravity_Direction.down || grav_dir == Gravity_Direction.up)
			moveVertical = moveVertical * vertical_movement_factor;
		if (grav_dir == Gravity_Direction.left || grav_dir == Gravity_Direction.right)
			moveHorizontal = moveHorizontal * vertical_movement_factor;

		Vector2 movement = new Vector2 (moveHorizontal, moveVertical);
		rb2d.AddForce (movement * speed * Time.deltaTime);

	}

	void Jump ()
	{
		if(jump_now) {
			// jump by force (acceleration)
			rb2d.AddForce ( jump_force_vec );
			
			// jump logic
			jump_now = false;
		}
	}
}