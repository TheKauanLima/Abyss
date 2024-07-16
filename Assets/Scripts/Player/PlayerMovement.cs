using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats MoveStats;
    [SerializeField] private Collider2D feetCollider;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Animator animator;

    [Header("Cameras")]
    [SerializeField] private GameObject cameraFollowPlayer;

    private Rigidbody2D rb;

    //movement vars
    private Vector2 moveSpeed;
    public bool facingRight;

    //collision check vars
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;
    private bool isGrounded;
    private bool isHeadBumped;

    //jump vars
    public float VerticalVelocity { get; private set; }
    private bool isJumping;
    private bool isFastFalling;
    private bool isFalling;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int jumpsUsed;

    //apex vars
    private float apex;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;

    //jump buffer vars
    private float jumpBufferTimer;
    private bool jumpBufferRelease;

    //coyote time vars
    private float coyoteTimer;

    private CameraFollowObject cameraFollowObject;
    private float fallSpeedYDampingChangeThreshold;

    private void Awake()
    {
        facingRight = true;

        rb = GetComponent<Rigidbody2D>();

		cameraFollowObject = cameraFollowPlayer.GetComponent<CameraFollowObject>();

        animator = gameObject.GetComponent<Animator>();

        fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingChangeThreshold;
	}

	private void Update()
	{
        CountTimers();
        JumpChecks();
	}

	private void FixedUpdate()
	{
        CollisionChecks();
        //TurnCheck();
        Jump();

        if (isGrounded)
			Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
        else Move(MoveStats.AirAcceleration, MoveStats.AirDeceleration, InputManager.Movement);

        //if we are flaling past a certain threshold
        if (rb.velocity.y < fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
            CameraManager.instance.LerpYDamping(true);

        //if we are standing still or moving up
        if (rb.velocity.y >= 0f && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            //reset so it can be called again
            CameraManager.instance.LerpedFromPlayerFalling = false;

            CameraManager.instance.LerpYDamping(false);
        }
	}

	private void OnDrawGizmos()
	{
		if (MoveStats.ShowWalkJumpArc)
            DrawJumpArc(MoveStats.maxWalkSpeed, Color.white);
        if (MoveStats.ShowRunJumpArc)
            DrawJumpArc(MoveStats.maxRunSpeed, Color.red);
	}

	#region Movement

	private void Move(float acceleration, float deceleration, Vector2 moveInput)
	{
		if (moveInput != Vector2.zero)
        {
            animator.SetBool("Running", true);
            TurnCheck(moveInput);

            //check if needs to turn
            Vector2 targetVelocity = Vector2.zero;
            if (InputManager.RunIsHeld)
                targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.maxRunSpeed;
            else
                targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.maxWalkSpeed;

            moveSpeed = Vector2.Lerp(moveSpeed, targetVelocity, acceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector2(moveSpeed.x, rb.velocity.y);
		}
        else if (moveInput == Vector2.zero)
        {
			animator.SetBool("Running", false);
			moveSpeed = Vector2.Lerp(moveSpeed, Vector2.zero, deceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector2(moveSpeed.x, rb.velocity.y);
        }
    }

	private void TurnCheck(Vector2 moveInput)
	{
		if (facingRight && moveInput.x < 0)
			Turn(false);
		else if (!facingRight && moveInput.x > 0)
			Turn(true);
	}

	private void Turn(bool turnRight)
    {
        if (turnRight)
        {
			facingRight = true;
			transform.Rotate(0f, 180f, 0f);

			cameraFollowObject.CallTurn();
		}
        else
        {
            facingRight = false;
            transform.Rotate(0f, -180f, 0f);

			cameraFollowObject.CallTurn();
		}
    }

	#endregion

	#region Jump

    private void JumpChecks()
    {
        //when jump button pressed
        if (InputManager.JumpWasPressed)
        {
            jumpBufferTimer = MoveStats.JumpBufferTime;
            jumpBufferRelease = false;
        }

        //when jump button released
        if (InputManager.JumpWasReleased)
        {
            if (jumpBufferTimer > 0f)
				jumpBufferRelease = true;

            if (isJumping && VerticalVelocity > 0f)
            {
				if (isPastApexThreshold)
				{
					isPastApexThreshold = false;
					isFastFalling = true;
					fastFallTime = MoveStats.TimeForUpwardsCancel;
					VerticalVelocity = 0f;
				}
				else
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = VerticalVelocity;
                }
			}
        }

        //initiate jump with jump buffering and coyote time
        if (jumpBufferTimer > 0f && !isJumping && (isGrounded || coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (jumpBufferRelease)
            {
                isFastFalling = true;
                fastFallReleaseSpeed = VerticalVelocity;
            }
        }

        //double jump
        else if (jumpBufferTimer > 0f && isJumping && jumpsUsed < MoveStats.NumberOfJumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);
        }

        //air jump after coyote time lapsed
        else if (jumpBufferTimer > 0f && isFalling && jumpsUsed < MoveStats.NumberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            isFastFalling = false;
        }

        //landed
        if ((isJumping || isFalling) && isGrounded && VerticalVelocity <= 0f)
        {
            isJumping = false;
            isFalling = false;
            isFastFalling = false;
            fastFallTime = 0f;
            isPastApexThreshold = false;
            jumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;
        }
    }

    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!isJumping)
			isJumping = true;
        jumpBufferTimer = 0f;
        jumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = MoveStats.InitialJumpvelocity;
    }

    private void Jump()
    {
        //apply gravity while jumping
        if (isJumping)
        {
            //check for head bump
            if (isHeadBumped)
				isFastFalling = true;

            //gravity on ascending
            if (VerticalVelocity >= 0f)
            {
                //apex controls
                apex = Mathf.InverseLerp(MoveStats.InitialJumpvelocity, 0f, VerticalVelocity);

                if (apex > MoveStats.ApexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;
                    }

                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < MoveStats.ApexHangTime)
							VerticalVelocity = 0f;
                        else VerticalVelocity = -0.01f;
                    }
                }

                //gravity on ascending but not past apex threshold
                else
                {
                    VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
                    if (isPastApexThreshold)
						isPastApexThreshold = false;
				}
            }

            //gravity on descending
            else if (!isFastFalling)
				VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            else if (VerticalVelocity < 0f)
                if (!isFalling)
					isFalling = true;
		}

		//jump cut
        if (isFastFalling)
        {
            if (fastFallTime >= MoveStats.TimeForUpwardsCancel)
				VerticalVelocity += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            else if (fastFallTime < MoveStats.TimeForUpwardsCancel)
				VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / MoveStats.TimeForUpwardsCancel));
            fastFallTime += Time.fixedDeltaTime;
        }

		//normal gravity while falling
        if (!isGrounded && !isJumping)
        {
            if (!isFalling)
				isFalling = true;
            VerticalVelocity += MoveStats.Gravity * Time.fixedDeltaTime;
        }

        //clamp fall speed
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -MoveStats.MaxFallSpeed, 50f);

        rb.velocity = new Vector2(rb.velocity.x, VerticalVelocity);
	}

	#endregion

	#region Collision Checks

	private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetCollider.bounds.size.x, MoveStats.GroundDetectionRayLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);
        if (groundHit.collider != null)
			isGrounded = true;
        else isGrounded = false;

		#region Debug Visualization
        if (MoveStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if (isGrounded)
                rayColor = Color.green;
            else rayColor = Color.red;

			Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
			Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
			Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
		}
		#endregion
	}

	private void BumpedHead()
	{
		Vector2 boxCastOrigin = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.max.y);
		Vector2 boxCastSize = new Vector2(feetCollider.bounds.size.x * MoveStats.HeadWidth, MoveStats.HeadDetectionRayLength);

		headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.HeadDetectionRayLength, MoveStats.GroundLayer);
		if (headHit.collider != null)
			isHeadBumped = true;
		else isHeadBumped = false;

		#region Debug Visualization
		if (MoveStats.DebugShowHeadBumpBox)
		{
            float headWidth = MoveStats.HeadWidth;

			Color rayColor;
			if (isHeadBumped)
				rayColor = Color.green;
			else rayColor = Color.red;

			Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.HeadDetectionRayLength, rayColor);
			Debug.DrawRay(new Vector2(boxCastOrigin.x + (boxCastSize.x / 2) * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.HeadDetectionRayLength, rayColor);
			Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y - MoveStats.HeadDetectionRayLength), Vector2.right * boxCastSize.x * headWidth, rayColor);
		}
		#endregion
	}

	private void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();
    }
	#endregion

	#region Timers

    private void CountTimers()
    {
        jumpBufferTimer -= Time.deltaTime;

        if (!isGrounded)
			coyoteTimer -= Time.deltaTime;
        else coyoteTimer = MoveStats.JumpCoyoteTime;
	}

	#endregion

	#region Jump Arc Visualizer

    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
    {
        Vector2 startPosition = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);
        Vector2 previousPosition = startPosition;
        float speed = 0f;
        if (MoveStats.DrawRight)
            speed = moveSpeed;
        else speed = -moveSpeed;
        Vector2 velocity = new Vector2(speed, MoveStats.InitialJumpvelocity);

        Gizmos.color = gizmoColor;

        float timeStep = 2 * MoveStats.TimeTillJumpApex / MoveStats.ArcResolution; //time step for the simulation
        //float totalTime = (2 * MoveStats.TimeTillJumpApex) + MoveStats.ApexHangTime; //total time of the arc including hang time

        for (int i = 0; i < MoveStats.VisualizationSteps; i++)
        {
            float simulationTime = i * timeStep;
            Vector2 displacement;
            Vector2 drawPoint;

            if (simulationTime < MoveStats.TimeTillJumpApex) //ascending
                displacement = velocity * simulationTime + 0.5f * new Vector2(0, MoveStats.Gravity) * simulationTime * simulationTime;
            else if (simulationTime < MoveStats.TimeTillJumpApex + MoveStats.ApexHangTime) //apex hang time
            {
                float apexTime = simulationTime - MoveStats.TimeTillJumpApex;
                displacement = velocity * MoveStats.TimeTillJumpApex + 0.5f * new Vector2(0, MoveStats.Gravity) * MoveStats.TimeTillJumpApex * MoveStats.TimeTillJumpApex;
                displacement += new Vector2(speed, 0) * apexTime; //no vertical movement during hang time
            }
            else //descending
            {
                float descendTime = simulationTime - (MoveStats.TimeTillJumpApex + MoveStats.ApexHangTime);
				displacement = velocity * MoveStats.TimeTillJumpApex + 0.5f * new Vector2(0, MoveStats.Gravity) * MoveStats.TimeTillJumpApex * MoveStats.TimeTillJumpApex;
				displacement += new Vector2(speed, 0) * MoveStats.ApexHangTime; //horizontal movement during hang time
                displacement += new Vector2(speed, 0) * descendTime + 0.5f * new Vector2(0, MoveStats.Gravity) * descendTime * descendTime;
            }

            drawPoint = startPosition + displacement;

            if (MoveStats.StopOnCollision)
            {
                RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), MoveStats.GroundLayer);
                if (hit.collider != null)
                {
                    //if a hit is detected, stop drawing the arc at the hit point
                    Gizmos.DrawLine(previousPosition, hit.point);
                    break;
                }
            }

            Gizmos.DrawLine(previousPosition, drawPoint);
            previousPosition = drawPoint;
        }
    }

	#endregion
}
