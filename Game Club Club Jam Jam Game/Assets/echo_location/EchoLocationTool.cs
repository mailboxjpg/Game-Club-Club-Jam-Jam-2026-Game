using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class EchoLocationTool : MonoBehaviour
{
    [SerializeField] private GameObject wave_prefab;
    [SerializeField] private float cooldown;
    [SerializeField] private float wave_count;
    [SerializeField] private float interval;
    [SerializeField] private float hold_radius = 0.5f;
    [SerializeField] private Light2D pulse_light;
    [SerializeField] private float pulse_speed = 1f;
    [SerializeField] private float min_pulse_intensity = 0.5f;
    [SerializeField] private float max_pulse_intensity = 1.5f;
    [SerializeField] private float hurt_radius;
    [SerializeField] private float damage;

    private float current_cooldown = 0;
    private float pulseT;
    private Vector3 mouse_dir;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        current_cooldown -= Time.deltaTime;
        
        float pulse = (Mathf.Sin(pulseT) + 1f) * 0.5f;
        pulse_light.intensity = Mathf.Lerp(min_pulse_intensity, max_pulse_intensity, pulse);
        pulseT += Time.deltaTime * pulse_speed;
        
        Vector2 mouse_pos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouse_dir = (mouse_pos - (Vector2)PlayerControl.Instance.transform.position).normalized;
        transform.SetPositionAndRotation(
            PlayerControl.Instance.transform.position + mouse_dir * hold_radius,
            Quaternion.LookRotation(Vector3.forward, mouse_dir));

        if (PlayerControl.Instance.inputActions.Player.Interact.WasPressedThisFrame())
        {
            SendEcho();
        }
    }

    public void SendEcho()
    {
        if (current_cooldown <= 0f)
        {
            current_cooldown = cooldown;
            for (int i = 0; i < wave_count; i++)
            {
                GameObject wave = Instantiate(wave_prefab, transform.position, transform.rotation);

                EchoWaveVisuals wave_visuals = wave.GetComponent<EchoWaveVisuals>();
                wave_visuals.direction = mouse_dir;
                wave_visuals.delay = (interval / wave_count) * i;
                wave_visuals.MakeWave(hurt_radius,damage);
            }
        }
    }
}
