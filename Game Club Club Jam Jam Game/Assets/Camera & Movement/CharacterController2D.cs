using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
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
    [Tooltip("Rate at which to slow down if already moving in desired direction.")]
    [SerializeField] protected float friction = 0.1f;
    [SerializeField] protected SpriteRenderer sprite;
    [Tooltip("Final multiplier on top of movement speed.")]
    public float movementMultiplier = 1f;

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
    public int maxAirJumps = 0;
    [Tooltip("Force applied on an air jump. Defaults to the same as jumpForce if left at 0.")]
    [SerializeField] protected float airJumpForce = 0f;

    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] protected LayerMask groundLayer;
    [Tooltip("Time needed to be on the ground to allow things to reset.")]
    [SerializeField] protected float groundedLandTime = 0.1f;

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
    [Tooltip("Horizontal force pushing away from the wall on a wall jump.")]
    [SerializeField] protected float wallJumpHorizontalForce = 10f;
    [Tooltip("Vertical force on a wall jump.")]
    [SerializeField] protected float wallJumpVerticalForce = 13f;
    [Tooltip("Seconds after a wall jump during which normal air-control input is suppressed, so the away-push isn't instantly cancelled by holding input back toward the wall.")]
    [SerializeField] protected float wallJumpLockoutTime = 0.15f;
    public int maxWallJumps = 2;

    [Header("Dashing")]
    [SerializeField] protected float dashSpeed = 20f;
    [SerializeField] protected float dashDuration = 0.15f;
    [SerializeField] protected float dashCooldown = 0.15f;
    public int maxDashes = 1;
    [SerializeField] protected bool preserveMomentum = false;
    [SerializeField] protected bool waveDashEnabled = true;

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
    public bool IsDashing { get; private set; }
    public int DashesRemaining { get; private set; }

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
    private float _wallSlideTimer;
    private float _wallJumpLockoutTimer;
    private bool _isWaveDashing;

    // Captured once in Awake so we always know the "standing" size to restore to / check clearance against
    private Vector2 _standingScale;
    private Vector2 _standingColliderSize;
    private Vector2 _standingColliderOffset;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector2 _dashDirection;
    private float _originalGravityScale;
    private float _groundedTimer;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        _standingScale = transform.localScale;
        _standingColliderSize = col.bounds.size;
        _standingColliderSize.x /= _standingScale.x;
        _standingColliderSize.y /= _standingScale.y;
        _standingColliderOffset = col.offset;
        AirJumpsRemaining = maxAirJumps;
        DashesRemaining = maxDashes;
        _originalGravityScale = rb.gravityScale;
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
        ApplyHorizontalMovement(GetMoveInput().x);
        UpdateCrouchState();
        UpdateWallSlideState();
        ApplyBetterGravity();
        ApplyWallSlide();
        if (!TryWallJump() && !TryGroundOrCoyoteJump())
        {
            TryAirJump();
        }
        UpdateDash();
        TryDash();
        _wallJumpLockoutTimer -= Time.fixedDeltaTime;
    }

    /// <summary>Return x -1 (left) to 1 (right) y -1 (down) to 1 (up). Override to supply input from any source.</summary>
    protected abstract Vector2 GetMoveInput();

    /// <summary>Return true on the frame jump was pressed.</summary>
    protected abstract bool GetJumpInput();

    /// <summary>Return true on the frame jump was released. Default: never (no short-hop). Override if needed.</summary>
    protected virtual bool GetJumpReleasedInput() => false;

    /// <summary>Return true while the crouch input is held. Default: never crouches. Override to enable crouching.</summary>
    protected virtual bool GetCrouchInput() => false;

    /// <summary>Return true while the run/sprint input is held. Default: never runs. Override to enable running.</summary>
    protected virtual bool GetRunInput() => false;

    protected abstract bool GetDashInput();

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
        AirJumpsRemaining = maxAirJumps; // refill air jumps
        DashesRemaining = maxDashes; // refill dashes
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
        if (IsDashing || _isWaveDashing)
            return;
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
        float targetSpeed = input * currentSpeed * movementMultiplier;
        float speedDiff = targetSpeed - rb.linearVelocityX;
        if ((targetSpeed > 0f && speedDiff < 0f) || (targetSpeed < 0f && speedDiff > 0f)) // already going in target direction past targetSpeed
        {
            targetSpeed = rb.linearVelocityX * (1f-friction);
            speedDiff *= friction;
        }

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
            wallCheckLeft.localScale = Vector2.one * heightMultiplier;
            wallCheckRight.localScale = Vector2.one * heightMultiplier;
            // wallCheckLeft.localPosition = new Vector2(wallCheckLeft.localPosition.x, _standingWallCheckY - heightDiff * 0.5f * _standingScale.y);
            // wallCheckRight.localPosition = new Vector2(wallCheckRight.localPosition.x, _standingWallCheckY - heightDiff * 0.5f * _standingScale.y);

            switch (col.GetType().Name)
            {
                case nameof(BoxCollider2D):
                    col.GetComponent<BoxCollider2D>().size = new Vector2(_standingColliderSize.x, newHeight);
                    col.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDiff * 0.5f);
                    break;
                case nameof(CapsuleCollider2D):
                    Vector2 newSize = new Vector2(_standingColliderSize.x, newHeight);
                    if (newHeight < _standingColliderSize.x)
                        newSize.x = newHeight;
                    col.GetComponent<CapsuleCollider2D>().size = newSize;
                    col.offset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDiff * 0.5f);
                    break;
                case nameof(CircleCollider2D):
                    col.GetComponent<CircleCollider2D>().radius = newHeight;
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
        // This code is a fucking mess just to undo all the scaling and transformations applied on the collider
        float heightDiff = _standingColliderSize.y - (_standingColliderSize.y * crouchHeightMultiplier);
        Vector2 checkSize = new Vector2(_standingColliderSize.x, heightDiff + (standUpCheckBuffer / _standingScale.y));
        Vector2 offset = _standingColliderOffset + Vector2.up * (_standingColliderSize.y * 0.5f - heightDiff * 0.5f);
        offset.y *= _standingScale.y;
        checkSize.x *= _standingScale.x;
        checkSize.y *= _standingScale.y;
        Vector2 checkCenter = (Vector2)transform.position + offset;

        // Overlap the strip of space between "top of crouch collider" and "top of standing collider"
        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, ceilingLayer);
        return hit == null;
    }

    /// <summary>Returns true if a normal ground/coyote-time jump fired this frame.</summary>
    protected bool TryGroundOrCoyoteJump()
    {
        if (IsDashing || IsJumping || _coyoteTimer <= 0f || _jumpBufferTimer <= 0f)
            return false;
        _coyoteTimer = 0f;
        _jumpBufferTimer = 0f;
        _groundedTimer = 0f;
        IsJumping = true; // block coyote time from re-arming a second jump until we land
        if (rb.linearVelocityY < jumpForce)
            rb.linearVelocityY = jumpForce;
        else
            rb.linearVelocityY += jumpForce;
        OnJump();
        return true;
    }

    /// <summary>
    /// Fires an extra mid-air jump if any are remaining. Only reachable when a ground/coyote/wall
    /// jump did NOT fire this frame, so a single press never consumes more than one jump type.
    /// </summary>
    protected bool TryAirJump()
    {
        if (IsGrounded || IsDashing || AirJumpsRemaining <= 0 || _jumpBufferTimer <= 0f)
            return false;

        AirJumpsRemaining--;
        _jumpBufferTimer = 0f;
        _groundedTimer = 0f;
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
        if (IsDashing)
            return;
        // Snappier, more "game feel"-y jump arc than default uniform gravity
        if (rb.linearVelocityY < 0f)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (rb.linearVelocityY > 0f && !GetJumpInputHeld())
        {
            rb.gravityScale = lowJumpGravityMultiplier;
        }
        else
        {
            rb.gravityScale = _originalGravityScale;
        }
    }

    /// <summary>Override if you want low-jump-cutoff behavior tied to a "held" state rather than release event.</summary>
    protected virtual bool GetJumpInputHeld() => true;

    protected void CheckGrounded()
    {
        Collider2D ground = null;
        if (groundCheck != null)
        {
            ground = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            IsGrounded = ground != null;
        }

        if (IsGrounded)
        {
            if (!IsJumping)
            {
                _coyoteTimer = coyoteTime;
            }

            if (_groundedTimer <= 0f) // landed this frame
            {
                if (IsDashing)
                {
                    OnLand(ground, 0f); // Dash negates fall speed
                    _dashTimer = 0f; // cancel dash
                    if (waveDashEnabled)
                    {
                        TryWaveDash();
                    }
                }
                else
                {
                    OnLand(ground, -rb.linearVelocityY); // fall speed going into ground (negative) should be the speed we use for checks
                }
            }

            if (_groundedTimer > groundedLandTime)
            {
                _wallSlideTimer = 0f; // reset wall slide timer once grounded
                IsJumping = false; // touched ground again; a new jump is now allowed
                AirJumpsRemaining = maxAirJumps; // refill air jumps on landing
                WallJumpsRemaining = maxWallJumps; // refill wall jumps on landing
                DashesRemaining = maxDashes; // refill dashes
            }
            _groundedTimer += Time.deltaTime;
        }
        else
        {
            _groundedTimer = 0f;
        }
    }

    protected void CheckWalls()
    {
        Vector2 scaledWallCheckSize = wallCheckSize;
        Vector3 wallCheckOffset = Vector3.zero;
        if (IsCrouching)
        {
            scaledWallCheckSize.y *= crouchHeightMultiplier;
            wallCheckOffset.y = -crouchHeightMultiplier * 0.5f * _standingColliderSize.y * _standingScale.y;
        }
        bool checkRightHit = wallCheckRight != null && Physics2D.OverlapBox(wallCheckRight.position + wallCheckOffset, scaledWallCheckSize, 0f, wallLayer);
        bool checkLeftHit = wallCheckLeft != null && Physics2D.OverlapBox(wallCheckLeft.position + wallCheckOffset, scaledWallCheckSize, 0f, wallLayer);

        IsTouchingWallRight = checkRightHit;
        IsTouchingWallLeft = checkLeftHit;
    }

    /// <summary>
    /// Determines whether the character should be considered "wall sliding" this frame:
    /// airborne, falling, touching a wall, and holding input into that wall. Also runs the
    /// slide duration timer and detaches once wallSlideTime is exceeded.
    /// </summary>
    protected void UpdateWallSlideState()
    {
        bool wasWallSliding = IsWallSliding;

        if (!enableWallSlide || _wallJumpLockoutTimer > 0f)
        {
            IsWallSliding = false;
        }
        else
        {
            float inputX = GetMoveInput().x;
            bool pressingIntoWall = (inputX > 0f && IsTouchingWallRight) || (inputX < 0f && IsTouchingWallLeft);
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
        if (IsDashing || maxWallJumps <= 0 || !IsWallSliding || _jumpBufferTimer <= 0f || WallJumpsRemaining <= 0)
            return false;

        _groundedTimer = 0f;

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
        WallJumpsRemaining--;

        OnJump();
        OnWallJump();
        return true;
    }

    protected virtual bool TryWaveDash()
    {
        if (!GetDashInput() || !waveDashEnabled)
            return false;

        Vector2 dir = GetMoveInput();
        if (dir.x == 0f || dir.y >= 0f)
            return false;
        _isWaveDashing = true;
        DashesRemaining--;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        dir.y = 0f;
        _dashDirection = dir.normalized;
        rb.linearVelocityX = _dashDirection.x * dashSpeed;
        rb.linearVelocityY = jumpForce;
        rb.gravityScale = _originalGravityScale;

        return true;
    }

    protected virtual bool TryDash()
    {
        if (_dashCooldownTimer > 0f || !GetDashInput() || DashesRemaining <= 0)
            return false;

        Vector2 dir = GetMoveInput();
        if (dir == Vector2.zero)
            return false;
        IsDashing = true;
        DashesRemaining--;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _dashDirection = dir.normalized;
        rb.linearVelocity = _dashDirection * dashSpeed;
        if ((dir.x > 0f && IsTouchingWallRight) || (dir.x < 0f && IsTouchingWallLeft))
            rb.linearVelocityX = 0f; // Prevent dash from clipping through wall
        rb.gravityScale = 0f;

        return true;
    }

    protected virtual void UpdateDash()
    {
        if (!IsDashing)
        {
            _dashCooldownTimer -= Time.fixedDeltaTime;
        }
        else
        {
            _dashTimer -= Time.fixedDeltaTime;

            if (_isWaveDashing)
                rb.linearVelocityX = _dashDirection.x * dashSpeed;
            else
                rb.linearVelocity = _dashDirection * dashSpeed;

            if (_dashTimer <= 0f)
            {
                if (!preserveMomentum && !_isWaveDashing) // wave dash preserves momentum
                {
                    rb.linearVelocity = Vector2.zero;
                }

                rb.gravityScale = _originalGravityScale;
                IsDashing = false;
                _isWaveDashing = false;
            }
        }
    }

    protected void Flip()
    {
        if (sprite != null)
            sprite.flipX = !IsFacingRight;
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
            wallCheckOffset.y = -crouchHeightMultiplier * 0.5f * _standingColliderSize.y * _standingScale.y;
        }
        if (wallCheckRight != null)
            Gizmos.DrawWireCube(wallCheckRight.position + wallCheckOffset, scaledWallCheckSize);
        if (wallCheckLeft != null)
            Gizmos.DrawWireCube(wallCheckLeft.position + wallCheckOffset, scaledWallCheckSize);

        // Ceiling check
        float heightDiff = _standingColliderSize.y - (_standingColliderSize.y * crouchHeightMultiplier);
        Vector2 checkSize = new Vector2(_standingColliderSize.x, heightDiff + (standUpCheckBuffer / _standingScale.y));
        Vector2 offset = _standingColliderOffset + Vector2.up * (_standingColliderSize.y * 0.5f - heightDiff * 0.5f);
        offset.y *= _standingScale.y;
        checkSize.x *= _standingScale.x;
        checkSize.y *= _standingScale.y;
        Vector2 checkCenter = (Vector2)transform.position + offset;
        Gizmos.DrawWireCube(checkCenter, checkSize);
    }

    public Vector3 GetGroundCheckPosition()
    {
        return groundCheck.position;
    }

    public float GetBounciness()
    {
        if (rb.sharedMaterial == null)
            return 0;
        return rb.sharedMaterial.bounciness;
    }

    public void SetBounciness(float bounciness)
    {
        if (rb.sharedMaterial == null)
            return;
        rb.sharedMaterial.bounciness = bounciness;
    }
}