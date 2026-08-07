using UnityEngine;

public class PlayerControl : CharacterController2D
{
    public static PlayerControl Instance { get; private set; }

    [Header("Player")]
    public ShellCollector shellCollector;

    [Tooltip("When true, the Player is always running and pressing Sprint keybind (left shift) makes the player walk.")]
    [SerializeField] private bool runByDefault = false;
    [SerializeField] private bool autoJumpWithHold = true;
    [SerializeField] private bool allowJumpCanceling = true;

    public InputSystem_Actions inputActions;

    private bool _jumpPressedThisFrame;
    private bool _jumpReleasedThisFrame;
    private bool _jumpHeld;

    protected override void Awake()
    {
        if (Instance != null)
        {
            Debug.Log($"[{name}: PlayerControl] A PlayerControl instance already exists. Setting original's position and rotation here and destroying this instance's gameObject.");
            Instance.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Destroy(gameObject);
            return;
        }
        base.Awake();
        inputActions = new InputSystem_Actions();
        Instance = this;
        inputActions.Enable();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            inputActions.Disable();
    }

    protected override void Update()
    {
        // Read raw input once per frame before the base class consumes it
        _jumpPressedThisFrame = inputActions.Player.Jump.WasPressedThisFrame();
        _jumpReleasedThisFrame = inputActions.Player.Jump.WasReleasedThisFrame();
        _jumpHeld = inputActions.Player.Jump.IsPressed();

        base.Update(); // let base handle timers / short-hop using the values above
    }

    protected override float GetMoveInput()
    {
        return inputActions.Player.Move.ReadValue<Vector2>().x;
    }
 
    protected override bool GetJumpInput() => _jumpPressedThisFrame || (autoJumpWithHold && _jumpHeld && IsGrounded && !IsJumping);
 
    protected override bool GetJumpReleasedInput() => allowJumpCanceling && _jumpReleasedThisFrame; // false defaults to never
 
    protected override bool GetJumpInputHeld() => _jumpHeld || !allowJumpCanceling; // true defaults to always use unity gravity jumping up
 
    protected override bool GetCrouchInput() => inputActions.Player.Crouch.IsPressed();
 
    protected override bool GetRunInput() => runByDefault ^ inputActions.Player.Sprint.IsPressed();
 
    protected override void OnJump()
    {
        base.OnJump();
        // Play sound or something
    }
 
    protected override void OnLand(Collider2D ground, float speed)
    {
        base.OnLand(ground, speed);
    }

    protected override void OnCrouchStart()
    {
        base.OnCrouchStart();
    }

    protected override void OnCrouchEnd()
    {
        base.OnCrouchEnd();
    }
}