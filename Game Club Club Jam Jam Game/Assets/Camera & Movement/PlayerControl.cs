using UnityEngine;

public class PlayerControl : CharacterController2D
{
    public static PlayerControl instance;
    public InputSystem_Actions inputActions;

    private bool _jumpPressedThisFrame;
    private bool _jumpReleasedThisFrame;
    private bool _jumpHeld;

    [Header("Player")]
    [SerializeField] private bool jumpWithHold = true;
    [SerializeField] private bool allowJumpCanceling = true;

    protected override void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning($"[{name}: PlayerControl] A PlayerControl instance already exists. Deleting this instance's gameObject.");
            Destroy(gameObject);
            return;
        }
        base.Awake();
        inputActions = new InputSystem_Actions();
        instance = this;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
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
 
    protected override bool GetJumpInput() => _jumpPressedThisFrame;
 
    protected override bool GetJumpReleasedInput() => _jumpReleasedThisFrame;
 
    protected override bool GetJumpInputHeld() => _jumpHeld;
 
    protected override bool GetCrouchInput() => inputActions.Player.Crouch.IsPressed();
 
    protected override bool GetRunInput() => inputActions.Player.Sprint.IsPressed();
 
    protected override void OnJump()
    {
        // Play sound or something
    }
 
    protected override void OnLand()
    {
        // Play sound or something
    }

    protected override void OnCrouchStart()
    {
        
    }

    protected override void OnCrouchEnd()
    {
        
    }
}