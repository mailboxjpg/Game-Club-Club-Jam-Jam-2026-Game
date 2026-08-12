using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : CharacterController2D
{
    public static PlayerControl Instance { get; private set; }

    [Header("Player")]
    public ShellCollector shellCollector;
    public HealthSystem healthSystem;

    [Tooltip("When true, the Player is always running and pressing Sprint keybind (left shift) makes the player walk.")]
    [SerializeField] private bool runByDefault = false;
    [SerializeField] private bool autoJumpWithHold = true;
    [SerializeField] private bool allowJumpCanceling = true;
    [SerializeField] private Popup livesPopup;
    [Tooltip("Base number of lives to multiply by difficulty multiplier (normal difficulty number of lives).")]
    [SerializeField] private int baseLives = 8; // Easy mode has infinite lives
    [Tooltip("Time needed to hold reset button to reset.")]
    [SerializeField] private float resetHoldTime = 0.75f;
    [SerializeField] private Slider resetSlider;

    private bool _jumpPressedThisFrame;
    private bool _jumpReleasedThisFrame;
    private bool _jumpHeld;
    private bool _dashHeld;
    private bool _isRespawning;
    private Checkpoint _prevCheckpoint;
    private Vector3 _spawnPosition;
    private float _resetTimer;
    private int _numLives;
    private bool _startedReset;

    public InputSystem_Actions inputActions;
    public Action OnRespawn;

    protected override void Awake()
    {
        if (Instance != null)
        {
            Debug.Log($"[{name}: PlayerControl] An instance already exists. Setting original's position to {_spawnPosition} and destroying this instance's gameObject.");

            Destroy(gameObject);
            return;
        }
        base.Awake();
        if (SceneLoader.Instance.difficulty != Difficulty.Easy)
        {
            _numLives = Mathf.CeilToInt(baseLives / SceneLoader.Instance.DifficultyScale);
            livesPopup.SetText($"Lives Left: {_numLives}", Color.yellow);
            livesPopup.StartPopup();
        }
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
        _dashHeld = inputActions.Player.Dash.IsPressed();

        if (inputActions.Player.Reset.WasPressedThisFrame())
        {
            resetSlider.gameObject.SetActive(true);
            _startedReset = true;
        }

        if (inputActions.Player.Reset.IsPressed() && _startedReset)
        {
            _resetTimer += Time.deltaTime;
            resetSlider.value = _resetTimer / resetHoldTime;
            if (_resetTimer >= resetHoldTime)
            {
                _resetTimer = 0.1f;
                Respawn(0f);
                _startedReset = false;
            }
        }
        else if (_resetTimer > 0f)
        {
            resetSlider.gameObject.SetActive(false);
            _resetTimer = 0f;
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
        if (_isRespawning || Time.timeScale == 0f)
            return;
        StartCoroutine(RespawnRoutine(delay));
    }

    public void KillPlayer(float respawnDelay)
    {
        if (_isRespawning)
            return;
        _numLives--;
        Respawn(respawnDelay);
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        _isRespawning = true;
        Time.timeScale = 0.5f;
        SceneLoader.Instance.FadeScreen(1f);
        yield return new WaitForSecondsRealtime(delay);
        if (SceneLoader.Instance.difficulty != Difficulty.Easy && _numLives <= 0)
        {
            _numLives = 0;
            SceneLoader.Instance.LoadScene("TitleScreen");
            yield break;
        }
        SceneLoader.Instance.FadeScreen(0f);
        Time.timeScale = 1f;
        transform.position = _spawnPosition;
        _isRespawning = false;
        if (SceneLoader.Instance.difficulty != Difficulty.Easy)
        {
            livesPopup.SetText($"Lives Left: {_numLives}", Color.yellow);
            livesPopup.StartPopup();
        }
        healthSystem.SetHealth(healthSystem.GetMaxHealth());
        OnRespawn?.Invoke();
    }

    public void Delete()
    {
        Debug.Log($"[{name}: PlayerControl] Deleting Instance.");
        Instance = null;
        Destroy(gameObject);
    }

    public void SetCheckpoint(Checkpoint checkpoint)
    {
        if (_prevCheckpoint != null)
            _prevCheckpoint.Deactivate();
        _prevCheckpoint = checkpoint;
        _spawnPosition = checkpoint.transform.position;
    }
}