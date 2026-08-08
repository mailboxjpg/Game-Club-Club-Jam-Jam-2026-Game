using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraControl : MonoBehaviour
{
    public static CameraControl Instance {get; private set;}

    [Tooltip("Thing that the camera will track (like the player).")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float lerpSpeed = 1f;
    [Tooltip("Mouse sensitivity when moving the focus position around.")]
    [SerializeField] private float focusSensitivity = 0.02f;

    private Camera _camera;

    // For cutscenes/events that require the camera to focus on something
    private bool _locked = false;
    private Vector2 _lockedPosition;

    // The point that the player wants to focus on by clicking
    private Vector2 _focusPosition;

    private bool _isShaking = false;
    private Vector3 _shakeOffset;

    // The bounds in world space that define the area the camera can move around in
    // null means the camera is unbounded (can move freely anywhere)
    private CameraBounds _cameraBounds;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _camera = GetComponent<Camera>();
        PlayerControl.Instance.inputActions.Player.CameraFocus.performed += SetFocusPoint;
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private void OnDestroy()
    {
        PlayerControl.Instance.inputActions.Player.CameraFocus.performed -= SetFocusPoint;
    }

    private void FixedUpdate()
    {
        Vector3 newPosition;

        if (_locked)
        {
            newPosition = _lockedPosition;
        }
        else
        {
            if (PlayerControl.Instance.inputActions.Player.CameraFocus.IsPressed())
            {
                // Camera moves to wherever the player's mouse started clicking + mouse's delta
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                // Vector2 targetDelta = target.position - targetPreviousPosition;
                _focusPosition += mouseDelta * focusSensitivity;
                newPosition = _focusPosition;
            }
            else
            {
                // Camera tracks the target (player)
                newPosition = followTarget.position;
            }
        }
        newPosition = ClampWithinBounds(newPosition);
        newPosition.z = -1; // Keep the camera above everything so we can see
        transform.position = Vector3.Lerp(transform.position, newPosition, lerpSpeed * Time.deltaTime) + _shakeOffset;
    }

    private void SetFocusPoint(InputAction.CallbackContext context)
    {
        _focusPosition = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    public CameraBounds GetBounds()
    {
        return _cameraBounds;
    }

    public void SetBounds(CameraBounds cameraBounds)
    {
        _cameraBounds = cameraBounds;
    }

    private Vector2 ClampWithinBounds(Vector2 position)
    {
        Vector2 clampedPosition = position;
        if (_cameraBounds != null)
        {
            float height = _camera.orthographicSize;
            float width = height * _camera.aspect;

            float minY = _cameraBounds.min.y + height;
            float maxY = _cameraBounds.max.y - height;
            clampedPosition.y = Mathf.Clamp(position.y, minY, maxY);

            float minX = _cameraBounds.min.x + width;
            float maxX = _cameraBounds.max.x - width;
            clampedPosition.x = Mathf.Clamp(position.x, minX, maxX);
        }
        return clampedPosition;
    }

    public void StartCameraShake(float magnitude, float speed, float duration)
    {
        if(!_isShaking)
            StartCoroutine(CameraShake(magnitude, speed, duration));
    }

    private IEnumerator CameraShake(float magnitude, float speed, float duration)
    {
        _isShaking = true;
        float timer = 0;
        float x = Random.Range(-999f, 999f);
        float y = Random.Range(-999f, 999f);
        while (timer < duration)
        {
            _shakeOffset = new Vector3((Mathf.PerlinNoise1D(x) - 0.5f) * 2, (Mathf.PerlinNoise1D(y) - 0.5f) * 2, 0) * magnitude;
            timer += Time.deltaTime;
            x += Time.deltaTime * speed;
            y += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
        _shakeOffset = Vector3.zero;
        _isShaking = false;
    }
}
