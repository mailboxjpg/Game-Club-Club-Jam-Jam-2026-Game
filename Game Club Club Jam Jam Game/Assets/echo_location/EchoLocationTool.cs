using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class EchoLocationTool : MonoBehaviour
{
    [SerializeField] private EchoWaveVisuals wavePrefab;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private int waveCount = 3;
    [Tooltip("Total time in seconds over which waveCount waves are staggered.")]
    [SerializeField] private float interval = 0.3f;
    [SerializeField] private float hitRadius = 0.25f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float holdRadius = 0.5f;
    [SerializeField] private Light2D pulseLight;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float minPulseIntensity = 0.5f;
    [SerializeField] private float maxPulseIntensity = 1.5f;

    private float _currentCooldown;
    private float _pulseT;
    private Vector2 _mouseDir = Vector2.right;
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        _currentCooldown -= Time.deltaTime;

        UpdatePulseLight();
        UpdateAimAndHoldPosition();

        if (PlayerControl.Instance.inputActions.Player.Interact.WasPressedThisFrame())
        {
            SendEcho();
        }
    }

    private void UpdatePulseLight()
    {
        if (pulseLight == null)
            return;

        float pulse = (Mathf.Sin(_pulseT) + 1f) * 0.5f;
        pulseLight.intensity = Mathf.Lerp(minPulseIntensity, maxPulseIntensity, pulse);
        _pulseT += Time.deltaTime * pulseSpeed;
    }

    private void UpdateAimAndHoldPosition()
    {
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 playerPos = PlayerControl.Instance.transform.position;

        Vector2 toMouse = mousePos - playerPos;
        if (toMouse.sqrMagnitude > 0.0001f)
        {
            _mouseDir = toMouse.normalized;
        }

        transform.SetPositionAndRotation(
            (Vector3)(playerPos + _mouseDir * holdRadius),
            Quaternion.LookRotation(Vector3.forward, _mouseDir));
    }

    public void SendEcho()
    {
        if (_currentCooldown > 0f)
            return;

        _currentCooldown = cooldown;

        for (int i = 0; i < waveCount; i++)
        {
            EchoWaveVisuals wave = Instantiate(wavePrefab, transform.position, transform.rotation);
            float delay = waveCount > 0 ? (interval / waveCount) * i : 0f;
            wave.Fire(_mouseDir, delay, hitRadius, damage);
        }
    }
}