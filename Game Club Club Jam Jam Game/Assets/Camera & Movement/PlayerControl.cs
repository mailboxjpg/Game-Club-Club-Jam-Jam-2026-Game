using System.Collections;
using TMPro;
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
    public int numLives = 3;

    private bool _jumpPressedThisFrame;
    private bool _jumpReleasedThisFrame;
    private bool _jumpHeld;
    private bool _dashHeld;
    private bool _isRespawning;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    public InputSystem_Actions inputActions;

    protected override void Awake()
    {
        if (Instance != null)
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            Debug.Log($"[{name}: PlayerControl] An instance already exists. Setting original's position to {_spawnPosition} and rotation to {_spawnRotation} and destroying this instance's gameObject.");

            Instance.Respawn(0f);
            Destroy(gameObject);
            return;
        }
        base.Awake();
        inputActions = new InputSystem_Actions();
        Instance = this;
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
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
        _dashHeld = inputActions.Player.Dash.IsPressed();

        if (inputActions.Player.Reset.WasPressedThisFrame())
        {
            Respawn(0f);
        }

        base.Update();
    }

    protected override Vector2 GetMoveInput() => inputActions.Player.Move.ReadValue<Vector2>();
 
    protected override bool GetJumpInput() => _jumpPressedThisFrame || (autoJumpWithHold && _jumpHeld && IsGrounded && !IsJumping);
 
    protected override bool GetJumpReleasedInput() => allowJumpCanceling && _jumpReleasedThisFrame; // false defaults to never
 
    protected override bool GetJumpInputHeld() => _jumpHeld || !allowJumpCanceling; // true defaults to always use unity gravity jumping up
 
    protected override bool GetCrouchInput() => inputActions.Player.Crouch.IsPressed();
 
    protected override bool GetRunInput() => runByDefault ^ inputActions.Player.Sprint.IsPressed();

    protected override bool GetDashInput() => _dashHeld;
 
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

    public void Respawn(float delay)
    {
        if (_isRespawning)
            return;
        StartCoroutine(RespawnRoutine(delay));
    }

    public bool KillPlayer(float respawnDelay)
    {
        if (_isRespawning)
            return false;
        numLives--;
        if (numLives <= 0)
        {
            numLives = 0;
            SceneLoader.Instance.LoadScene("MenuScene"); // TODO: CHANGE TO ACTUAL NAME LATER
            return false;
        }
        Respawn(respawnDelay);
        return true;
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        _isRespawning = true;
        Time.timeScale = 0.5f;
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        _isRespawning = false;
    }
}