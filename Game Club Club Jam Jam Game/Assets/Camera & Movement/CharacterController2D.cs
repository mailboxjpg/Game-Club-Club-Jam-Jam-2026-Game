using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class CharacterController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float walkSpeed = 5f;
    [SerializeField] protected float runSpeed = 8f;
    [SerializeField] protected float crouchSpeed = 2.5f;
    [SerializeField] protected float acceleration = 60f; // ground responsiveness
    [SerializeField] protected float deceleration = 60f; // stopping responsiveness
    [SerializeField] protected float airControlMultiplier = 0.6f; // how much control you keep in air

    [Header("Crouching")]
    [Tooltip("Multiplier applied to the original height while crouching (e.g. 0.5 = half height).")]
    [SerializeField] protected float crouchHeightMultiplier = 0.5f;
    [Tooltip("Applies multiplier to the original collider height if false. Resizes entire transform using scale if true.")]
    [SerializeField] protected bool crouchResizesScale = false;
    [Tooltip("Layer(s) checked above the character to decide whether it's safe to stand back up.")]
    [SerializeField] protected LayerMask ceilingLayer;
    [Tooltip("Small buffer added above the standing collider height when checking for ceiling clearance.")]
    [SerializeField] protected float standUpCheckBuffer = 0.05f;

    [Header("Jumping")]
    [SerializeField] protected float jumpForce = 14f;
    [SerializeField] protected float coyoteTime = 0.1f; // grace period after leaving a ledge
    [SerializeField] protected float jumpBufferTime = 0.1f; // grace period for early jump press
    [SerializeField] protected float fallGravityMultiplier = 2.2f; // snappier falls
    [SerializeField] protected float lowJumpGravityMultiplier = 2f; // short-hop when jump released early

    [Header("Air Jumps")]
    [Tooltip("Extra jumps allowed while airborne, on top of the normal ground jump. 0 = disabled, 1 = double jump, 2 = triple jump, etc.")]
    [SerializeField] protected int maxAirJumps = 0;
    [Tooltip("Force applied on an air jump. Defaults to the same as jumpForce if left at 0.")]
    [SerializeField] protected float airJumpForce = 0f;

    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] protected LayerMask groundLayer;

    [Header("Wall Detection")]
    [Tooltip("Prevents sticking to walls when airborne and holding input into them (outside of an active wall slide).")]
    [SerializeField] protected bool preventWallCling = true;
    [SerializeField] protected Transform wallCheckRight;
    [SerializeField] protected Transform wallCheckLeft;
    [SerializeField] protected Vector2 wallCheckSize = new Vector2(0.1f, 0.9f);
    [Tooltip("Layer(s) considered a wall for sliding/jumping. Usually the same as groundLayer.")]
    [SerializeField] protected LayerMask wallLayer;

    [Header("Wall Sliding")]
    [SerializeField] protected bool enableWallSlide = true;
    [Tooltip("Max downward speed while sliding on a wall.")]
    [SerializeField] protected float wallSlideSpeed = 2f;
    [Tooltip("How long the character can slide on a wall before detaching and free-falling.")]
    [SerializeField] protected float wallSlideTime = 1f;

    [Header("Wall Jumping")]
    [SerializeField] protected bool enableWallJump = true;
    [Tooltip("Horizontal force pushing away from the wall on a wall jump.")]
    [SerializeField] protected float wallJumpHorizontalForce = 10f;
    [Tooltip("Vertical force on a wall jump.")]
    [SerializeField] protected float wallJumpVerticalForce = 13f;
    [Tooltip("Seconds after a wall jump during which normal air-control input is suppressed, so the away-push isn't instantly cancelled by holding input back toward the wall.")]
    [SerializeField] protected float wallJumpLockoutTime = 0.15f;
    [SerializeField] protected int maxWallJumps = 2;

    protected Rigidbody2D rb;
    protected Collider2D col;

    // Runtime state; exposed as read-only properties so subclasses / other scripts can react to them
    public bool IsGrounded { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsTouchingWallRight { get; private set; }
    public bool IsTouchingWallLeft { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsJumping { get; private set; } // true from the moment we jump until we've left the ground and landed again
    public int AirJumpsRemaining { get; private set; }
    public int WallJumpsRemaining { get; private set; }
    public Vector2 Velocity => rb.linearVelocity;

    public event Action OnJumped;
    public event Action OnAirJumped;
    public event Action<Collider2D, float> OnLanded;
    public event Action<bool> OnFlipped;
    public event Action OnCrouchStarted;
    public event Action OnCrouchEnded;
    public event Action OnWallSlideStarted;
    public event Action OnWallSlideEnded;
    public event Action OnWallJumped;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _wasGroundedLastFrame;
    private float _wallSlideTimer;
    private float _wallJumpLockoutTimer;

    // Captured once in Awake so we always know the "standing" size to restore to / check clearance against
    private Vector2 _standingScale;
    private Vector2 _standingColliderSize;
    private Vector2 _standingColliderOffset;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        _standingScale = transform.localScale;
        _standingColliderSize = col.bounds.size;
        _standingColliderOffset = col.offset;
        AirJumpsRemaining = maxAirJumps;
    }

    protected virtual void Update()
    {
        // Timers tick every frame regardless of physics step
        _coyoteTimer -= Time.deltaTime;
        _jumpBufferTimer -= Time.deltaTime;

        if (GetJumpInput())
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        if (GetJumpReleasedInput() && rb.linearVelocityY > 0f)
        {
            // Short-hop: cut upward velocity if jump button released early
            rb.linearVelocityY *= 0.5f;
        }
    }

    protected virtual void FixedUpdate()
    {
        CheckGrounded();
        CheckWalls();
        UpdateCrouchState();
        UpdateWallSlideState();
        ApplyHorizontalMovement(GetMoveInput());
        ApplyBetterGravity();
        ApplyWallSlide();
        if (!TryWallJump() && !TryGroundOrCoyoteJump())
        {
            TryAirJump();
        }
        _wallJumpLockoutTimer -= Time.fixedDeltaTime;
    }

    /// <summary>Return -1 (left) to 1 (right). Override to supply input from any source.</summary>
    protected abstract float GetMoveInput();

    /// <summary>Return true on the frame jump was pressed.</summary>
    protected abstract bool GetJumpInput();

    /// <summary>Return true on the frame jump was released. Default: never (no short-hop). Override if needed.</summary>
    protected virtual bool GetJumpReleasedInput() => false;

    /// <summary>Return true while the crouch input is held. Default: never crouches. Override to enable crouching.</summary>
    protected virtual bool GetCrouchInput() => false;

    /// <summary>Return true while the run/sprint input is held. Default: never runs. Override to enable running.</summary>
    protected virtual bool GetRunInput() => false;

    protected virtual void OnJump()
    {
        OnJumped?.Invoke();
    }
    protected virtual void OnAirJump()
    {
        OnAirJumped?.Invoke();
    }
    protected virtual void OnLand(Collider2D ground, float speed)
    {
        OnLanded?.Invoke(ground, speed);
    }
    protected virtual void OnFlip(bool isFacingRight)
    {
        OnFlipped?.Invoke(isFacingRight);
    }
    protected virtual void OnCrouchStart()
    {
        OnCrouchStarted?.Invoke();
    }
    protected virtual void OnCrouchEnd()
    {
        OnCrouchEnded?.Invoke();
    }
    protected virtual void OnWallSlideStart()
    {
        OnWallSlideStarted?.Invoke();
    }
    protected virtual void OnWallSlideEnd()
    {
        OnWallSlideEnded?.Invoke();
    }
    protected virtual void OnWallJump()
    {
        OnWallJumped?.Invoke();
    }

    protected void ApplyHorizontalMovement(float input)
    {
        // Suppress normal air-control input briefly after a wall jump so the away-push isn't
        // instantly cancelled out by the player still holding input back toward the wall.
        if (_wallJumpLockoutTimer > 0f)
        {
            return;
        }

        if (preventWallCling && !IsGrounded)
        {
            if (input > 0f && IsTouchingWallRight)
            {
                rb.linearVelocityX = 0f;
                return;
            }
            else if (input < 0f && IsTouchingWallLeft)
            {
                rb.linearVelocityX = 0f;
                return;
            }
        }

        float currentSpeed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
        float targetSpeed = input * currentSpeed;
        float speedDiff = targetSpeed - rb.linearVelocityX;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        if (!IsGrounded)
            accelRate *= airControlMultiplier;

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.linearVelocityX += movement;

        if (Mathf.Abs(input) > 0.01f)
        {
            bool facingRight = input > 0f;
            if (facingRight != IsFacingRight)
            {
                IsFacingRight = facingRight;
                Flip();
            }
        }
    }

    /// <summary>
    /// Resolves crouch/run state each physics step. Crouch always wins over run (can't sprint while crouched).
    /// Crouching shrinks the collider; standing back up is blocked if something is directly overhead, exactly
    /// like the classic "can't uncrouch under a low ledge" platformer behavior.
    /// </summary>
    protected void UpdateCrouchState()
    {
        bool wantsToCrouch = GetCrouchInput();

        if (wantsToCrouch && !IsCrouching)
        {
            IsCrouching = true;
            ResizeCollider(crouchHeightMultiplier);
            OnCrouchStart();
        }
        else if (!wantsToCrouch && IsCrouching)
        {
            if (CanStandUp())
            {
                IsCrouching = false;
                ResizeCollider(1f);
                OnCrouchEnd();
            }
            // else: stay crouched since something is blocking overhead. Re-checked every FixedUpdate
            // so the character stands up automatically the moment the obstruction clears.
        }

        // Crouch overrides run so can't sprint while crouched, regardless of run input.
        IsRunning = !IsCrouching && GetRunInput();
    }

    private void ResizeScale(float heightMultiplier)
    {
        float newHeight = _standingScale.y * heightMultiplier;
        float heightDiff = transform.localScale.y - newHeight;

        transform.localScale = new Vector3(_standingScale.x, newHeight, transform.localScale.z);
        // Shift transform down by height difference so the character's feet stay on the ground
        transform.position -= new Vector3(0f, heightDiff, 0f);
    }

    private void ResizeCollider(float heightMultiplier)
    {
        if (crouchResizesScale)
        {
            ResizeScale(heightMultiplier);
        }
        else
        {
            float newHeight = _standingColliderSize.y * heightMultiplier;
            float heightDiff = _standingColliderSize.y - newHeight;

            switch (col.GetType().Name)
            {
                case nameof(BoxCollider2D):
                    col.GetComponent<BoxCollider2D>().size = new Vector2(_standingColliderSize.x, newHeight);
                    col.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDiff * 0.5f);
                    break;
                case nameof(CapsuleCollider2D):
                    col.GetComponent<CapsuleCollider2D>().size = new Vector2(_standingColliderSize.x, newHeight);
                    col.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDiff * 0.5f);
                    break;
                default:
                    // Cant resize height individually, just defer to scale
                    ResizeScale(heightMultiplier);
                    break;
            }
        }
    }

    /// <summary>Checks whether there's room directly above the character to return to standing height.</summary>
    private bool CanStandUp()
    {
        float heightDiff = _standingColliderSize.y - (_standingColliderSize.y * crouchHeightMultiplier);
        Vector2 checkSize = new Vector2(_standingColliderSize.x, heightDiff + standUpCheckBuffer);
        Vector2 checkCenter = (Vector2)transform.position + _standingColliderOffset + Vector2.up * (_standingColliderSize.y * 0.5f - heightDiff * 0.5f);

        // Overlap the strip of space between "top of crouch collider" and "top of standing collider"
        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, ceilingLayer);
        return hit == null;
    }

    /// <summary>Returns true if a normal ground/coyote-time jump fired this frame.</summary>
    protected bool TryGroundOrCoyoteJump()
    {
        bool canJump = !IsJumping && _coyoteTimer > 0f && _jumpBufferTimer > 0f;
        if (canJump)
        {
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            IsJumping = true; // block coyote time from re-arming a second jump until we land
            if (rb.linearVelocityY < jumpForce)
                rb.linearVelocityY = jumpForce;
            else
                rb.linearVelocityY += jumpForce;
            OnJump();
        }
        return canJump;
    }

    /// <summary>
    /// Fires an extra mid-air jump if any are remaining. Only reachable when a ground/coyote/wall
    /// jump did NOT fire this frame, so a single press never consumes more than one jump type.
    /// </summary>
    protected bool TryAirJump()
    {
        if (AirJumpsRemaining <= 0 || _jumpBufferTimer <= 0f)
        {
            return false;
        }

        AirJumpsRemaining--;
        _jumpBufferTimer = 0f;
        float force = airJumpForce > 0f ? airJumpForce : jumpForce;
        if (rb.linearVelocityY < force)
            rb.linearVelocityY = force;
        else
            rb.linearVelocityY += force;
        OnJump();
        OnAirJump();
        return true;
    }

    protected void ApplyBetterGravity()
    {
        // Snappier, more "game feel"-y jump arc than default uniform gravity
        if (rb.linearVelocityY < 0f)
        {
            rb.linearVelocityY += Physics2D.gravity.y * fallGravityMultiplier * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocityY > 0f && !GetJumpInputHeld())
        {
            rb.linearVelocityY += Physics2D.gravity.y * lowJumpGravityMultiplier * Time.fixedDeltaTime;
        }
    }

    /// <summary>Override if you want low-jump-cutoff behavior tied to a "held" state rather than release event.</summary>
    protected virtual bool GetJumpInputHeld() => true;

    protected void CheckGrounded()
    {
        _wasGroundedLastFrame = IsGrounded;

        Collider2D ground = null;
        if (groundCheck != null)
        {
            ground = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            IsGrounded = ground != null;
        }

        if (IsGrounded && !IsJumping)
        {
            _coyoteTimer = coyoteTime;
        }

        if (IsGrounded && !_wasGroundedLastFrame)
        {
            _wallSlideTimer = 0f; // reset wall slide timer once grounded
            IsJumping = false; // touched ground again; a new jump is now allowed
            AirJumpsRemaining = maxAirJumps; // refill air jumps on landing
            WallJumpsRemaining = maxWallJumps; // refill wall jumps on landing
            OnLand(ground, -rb.linearVelocityY); // speed going into ground (negative) should be the speed we use for checks
        }
    }

    protected void CheckWalls()
    {
        Vector2 scaledWallCheckSize = wallCheckSize;
        Vector3 wallCheckOffset = Vector3.zero;
        if (IsCrouching)
        {
            scaledWallCheckSize.y *= crouchHeightMultiplier;
            wallCheckOffset.y = -crouchHeightMultiplier * 0.5f * _standingColliderSize.y;
        }
        bool checkRightHit = wallCheckRight != null && Physics2D.OverlapBox(wallCheckRight.position + wallCheckOffset, scaledWallCheckSize, 0f, wallLayer);
        bool checkLeftHit = wallCheckLeft != null && Physics2D.OverlapBox(wallCheckLeft.position + wallCheckOffset, scaledWallCheckSize, 0f, wallLayer);

        if (IsFacingRight)
        {
            IsTouchingWallRight = checkRightHit;
            IsTouchingWallLeft = checkLeftHit;
        }
        else
        {
            IsTouchingWallRight = checkLeftHit;
            IsTouchingWallLeft = checkRightHit;
        }
    }

    /// <summary>
    /// Determines whether the character should be considered "wall sliding" this frame:
    /// airborne, falling, touching a wall, and holding input into that wall. Also runs the
    /// slide duration timer and detaches once wallSlideTime is exceeded.
    /// </summary>
    protected void UpdateWallSlideState()
    {
        bool wasWallSliding = IsWallSliding;

        if (!enableWallSlide || IsGrounded || _wallJumpLockoutTimer > 0f)
        {
            IsWallSliding = false;
        }
        else
        {
            float input = GetMoveInput();
            bool pressingIntoWall = (input > 0f && IsTouchingWallRight) || (input < 0f && IsTouchingWallLeft);
            bool falling = rb.linearVelocityY <= 0f;

            if (pressingIntoWall && falling && _wallSlideTimer < wallSlideTime)
            {
                IsWallSliding = true;
                _wallSlideTimer += Time.fixedDeltaTime;
            }
            else
            {
                IsWallSliding = false;
            }
        }

        if (IsWallSliding && !wasWallSliding)
            OnWallSlideStart();
        if (!IsWallSliding && wasWallSliding)
            OnWallSlideEnd();
    }

    /// <summary>Caps fall speed while wall sliding. Runs after ApplyBetterGravity so it clamps the final velocity.</summary>
    protected void ApplyWallSlide()
    {
        if (IsWallSliding && rb.linearVelocityY < -wallSlideSpeed)
        {
            rb.linearVelocityY = -wallSlideSpeed;
        }
    }

    /// <summary>
    /// Fires a wall jump if sliding and jump was pressed. Returns true if it fired, so FixedUpdate
    /// can skip normal ground/coyote jump handling this frame (they're mutually exclusive per-frame).
    /// </summary>
    protected bool TryWallJump()
    {
        if (!enableWallJump || !IsWallSliding || _jumpBufferTimer <= 0f || WallJumpsRemaining <= 0)
        {
            return false;
        }

        // Jump away from whichever wall we're sliding on
        int wallJumpDirection = IsTouchingWallRight ? -1 : 1;
        rb.linearVelocityX = wallJumpDirection * wallJumpHorizontalForce;
        if (rb.linearVelocityY < wallJumpVerticalForce)
            rb.linearVelocityY = wallJumpVerticalForce;
        else
            rb.linearVelocityY += wallJumpVerticalForce;

        _wallJumpLockoutTimer = wallJumpLockoutTime;
        IsWallSliding = false;
        _wallSlideTimer = 0f;
        IsJumping = true; // reuse the same guard as a normal jump so coyote/ground checks don't immediately re-trigger
        _jumpBufferTimer = 0f;
        AirJumpsRemaining = maxAirJumps; // wall contact refills air jumps, same as touching the ground
        WallJumpsRemaining--;

        OnJump();
        OnWallJump();
        return true;
    }

    protected void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        OnFlip(IsFacingRight);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        Gizmos.color = Color.magenta;
        
        Vector2 scaledWallCheckSize = wallCheckSize;
        Vector3 wallCheckOffset = Vector3.zero;
        if (IsCrouching)
        {
            scaledWallCheckSize.y *= crouchHeightMultiplier;
            wallCheckOffset.y = -crouchHeightMultiplier * 0.5f * _standingColliderSize.y;
        }
        if (wallCheckRight != null) Gizmos.DrawWireCube(wallCheckRight.position + wallCheckOffset, scaledWallCheckSize);
        if (wallCheckLeft != null) Gizmos.DrawWireCube(wallCheckLeft.position + wallCheckOffset, scaledWallCheckSize);
    }
}