using UnityEngine;

public class echo_location_tool : MonoBehaviour
{
    [SerializeField] GameObject wave_prefab;
    Camera cam;
    private void Start()
    {
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }
    public void send_echo()
    {
        Vector2 mouse_pos = cam.ScreenToWorldPoint( Input.mousePosition );
        GameObject wave = Instantiate( wave_prefab, transform.position, transform.rotation);

        echo_wave_visuals wave_visuals = wave.GetComponent<echo_wave_visuals>();
        wave_visuals.direction = -(new Vector2(transform.position.x, transform.position.y) - mouse_pos).normalized;
        wave_visuals._Makewave();

    }
}
