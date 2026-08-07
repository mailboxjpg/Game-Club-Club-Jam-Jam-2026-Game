using UnityEngine;
using UnityEngine.Rendering.Universal;

public class echo_wave_visuals : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject light;
    [SerializeField] float expansion_rate;
    [SerializeField] float max_radius;
    [SerializeField] int segments;
    [SerializeField] public Vector2 direction;
    [SerializeField] float radian_width;
    [SerializeField] public float delay;

    bool active_expanding = false;
    float current_radius;
    Vector2 current_origin;
    GameObject[] lights;
    Light2D[] light_components;
    public void _Makewave()
    {
        print("attempt create wave");
        current_radius = 0;
        active_expanding = true;
        current_origin = transform.position;
        //create lights
        lights = new GameObject[segments];
        light_components = new Light2D[segments];
        for (int i = 0; i < segments; i++)
        {
            lights[i] = Instantiate(light,transform);
            light_components[i] = lights[i].GetComponent<Light2D>();
            light_components[i].enabled = false;
        }
    }

    private void Update()
    {
        if (active_expanding && delay<0)
        {
            current_radius = Mathf.Lerp(current_radius, max_radius, expansion_rate * Time.deltaTime);
            MakeWave(lineRenderer, current_radius, segments, current_origin, direction);
            if (current_radius > max_radius*0.95)
            {
                GameObject.Destroy(gameObject);
            }
        }
        delay -= Time.deltaTime;
    }

    public void MakeWave(LineRenderer lineRenderer, float radius, int segments, Vector2 origin, Vector2 direction)
    {
        Vector3[] points = new Vector3[segments];
        float focus_radian = Mathf.Atan2(direction.y, direction.x);

        for (int i = 0; i < segments; i++)
        {
            float angle = (i * radian_width*2) / segments + (focus_radian - radian_width);

            Vector2 point_vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float x = origin.x + point_vector.x * radius;
            float y = origin.y + point_vector.y * radius;

            Vector3 point_position = new Vector3(x, y, 5);

            points[i] = point_position;
            light_components[i].enabled = true;
            lights[i].transform.position = point_position;
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(points);
    }
}
