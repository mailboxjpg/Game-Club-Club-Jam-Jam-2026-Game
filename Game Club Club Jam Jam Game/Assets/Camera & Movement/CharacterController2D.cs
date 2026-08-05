using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
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

    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] protected LayerMask groundLayer;

    protected Rigidbody2D rb;
    protected BoxCollider2D col;

    // Runtime state; exposed as read-only properties so subclasses / other scripts can react to them
    public bool IsGrounded { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }
    public Vector2 Velocity => rb.linearVelocity;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _wasGroundedLastFrame;
    private bool _isJumping; // true from the moment we jump until we've left the ground and landed again

    // Captured once in Awake so we always know the "standing" size to restore to / check clearance against
    private Vector2 _standingScale;
    private Vector2 _standingColliderSize;
    private Vector2 _standingColliderOffset;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rb.freezeRotation = true;
        _standingScale = transform.localScale;
        _standingColliderSize = col.size;
        _standingColliderOffset = col.offset;
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
            rb.linearVelocity = new Vector2(rb.linearVelocityX, rb.linearVelocityY * 0.5f);
        }
    }

    protected virtual void FixedUpdate()
    {
        CheckGrounded();
        UpdateCrouchState();
        ApplyHorizontalMovement(GetMoveInput());
        ApplyBetterGravity();
        HandleJumpBuffering();
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

    protected virtual void OnJump() { }
    protected virtual void OnLand() { }
    protected virtual void OnFlip(bool isFacingRight) { }
    protected virtual void OnCrouchStart() { }
    protected virtual void OnCrouchEnd() { }

    protected void ApplyHorizontalMovement(float input)
    {
        float currentSpeed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
        float targetSpeed = input * currentSpeed;
        float speedDiff = targetSpeed - rb.linearVelocityX;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        if (!IsGrounded) accelRate *= airControlMultiplier;

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocityX + movement, rb.linearVelocityY);

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

    private void ResizeCollider(float heightMultiplier)
    {
        if (crouchResizesScale)
        {
            float newHeight = _standingScale.y * heightMultiplier;
            float heightDiff = transform.localScale.y - newHeight;

            transform.localScale = new Vector3(_standingScale.x, newHeight, transform.localScale.z);
            // Shift transform down by height difference so the character's feet stay on the ground
            transform.position -= new Vector3(0f, heightDiff, 0f);
        }
        else
        {
            float newHeight = _standingColliderSize.y * heightMultiplier;
            float heightDiff = _standingColliderSize.y - newHeight;

            col.size = new Vector2(_standingColliderSize.x, newHeight);
            col.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDiff * 0.5f);
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

    protected void HandleJumpBuffering()
    {
        bool canJump = !_isJumping && _coyoteTimer > 0f && _jumpBufferTimer > 0f;
        if (canJump)
        {
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _isJumping = true; // block coyote time from re-arming a second jump until we land
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            OnJump();
        }
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

        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }

        if (IsGrounded && !_isJumping)
        {
            _coyoteTimer = coyoteTime;
        }

        if (IsGrounded && !_wasGroundedLastFrame)
        {
            _isJumping = false; // touched ground again; a new jump is now allowed
            OnLand();
        }
    }

    protected void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        OnFlip(IsFacingRight);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}