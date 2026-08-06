using UnityEngine;

public class echo_wave_visuals : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int expansion_rate;
    [SerializeField] float max_radius;
    [SerializeField] int segments;
    [SerializeField] public Vector2 direction;
    [SerializeField] float radian_width;

    bool active_expanding = false;
    float current_radius;
    Vector2 current_origin;
    public void _Makewave()
    {
        print("attempt create wave");
        current_radius = 0;
        active_expanding = true;
        current_origin = transform.position;
    }

    private void Update()
    {
        if (active_expanding)
        {
            current_radius = Mathf.Lerp(current_radius, max_radius, expansion_rate * Time.deltaTime);
            MakeWave(lineRenderer, current_radius, segments, current_origin, direction);
            if (current_radius > max_radius*0.95)
            {
                GameObject.Destroy(gameObject);
            }
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

            points[i] = new Vector3(x, y, 5);
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(points);
    }
}
