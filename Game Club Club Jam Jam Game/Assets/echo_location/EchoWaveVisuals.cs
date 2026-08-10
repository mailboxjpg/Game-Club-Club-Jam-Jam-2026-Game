using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EchoWaveVisuals : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject light;
    [SerializeField] float expansion_rate;
    [SerializeField] float max_radius;
    [SerializeField] int segments;
    [SerializeField] public Vector2 direction;
    [SerializeField] float radian_width;
    [SerializeField] public float delay;
    [Tooltip("Lerp from max (y) to min (x) with time to simulate wave dissipating.")]
    [SerializeField] private Vector2 intensityRange;
    float _hurt_radius;
    float _damage;
    bool already_hit = false;

    bool active_expanding = false;
    float current_radius;
    Vector2 current_origin;
    GameObject[] lights;
    Light2D[] light_components;
    float expandT;

    private void Update()
    {
        if (active_expanding && delay<0)
        {
            expandT += Time.deltaTime * expansion_rate;
            current_radius = expandT * max_radius;
            MakeWave(lineRenderer, current_radius, segments, current_origin, direction);
            if (current_radius > max_radius*0.95)
            {
                Destroy(gameObject);
            }
        }
        delay -= Time.deltaTime;
    }

    public void MakeWave(float hurt_radius, float damage)
    {
        _hurt_radius = hurt_radius;
        _damage = damage;
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
            light_components[i].intensity = Mathf.Lerp(intensityRange.y, intensityRange.x, expandT);

            //hitboxes
            if (!already_hit)
            {
                foreach (Collider2D c in Physics2D.OverlapCircleAll(point_position, _hurt_radius))
                {
                    if (c.tag == "Enemy")
                    {
                        already_hit = true;
                        c.gameObject.GetComponent<enemy_health>().deal_damage(_damage); break;
                    }
                }
            }
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(points);
    }
}
