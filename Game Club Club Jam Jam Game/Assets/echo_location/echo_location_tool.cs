using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class echo_location_tool : MonoBehaviour
{
    [SerializeField] GameObject wave_prefab;
    Camera cam;
    [SerializeField] float cooldown;
    [SerializeField] float wave_count;
    [SerializeField] float interval;
    float current_cooldown = 0;

    private void Update()
    {
        current_cooldown -= Time.deltaTime;
    }
    private void Start()
    {
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }
    public void send_echo()
    {
        if (0 >= current_cooldown)
        {
            current_cooldown = cooldown;
            Vector2 mouse_pos = cam.ScreenToWorldPoint(Input.mousePosition);

            for (int i = 0; i < wave_count; i++)
            {
                GameObject wave = Instantiate(wave_prefab, transform.position, transform.rotation);

                echo_wave_visuals wave_visuals = wave.GetComponent<echo_wave_visuals>();
                wave_visuals.direction = -(new Vector2(transform.position.x, transform.position.y) - mouse_pos).normalized;
                wave_visuals.delay = (interval / wave_count) * i;
                wave_visuals._Makewave();
            }
        }
    }
}
