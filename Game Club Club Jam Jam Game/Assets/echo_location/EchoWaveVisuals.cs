using UnityEngine;

public class EchoWaveVisuals : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int expansion_rate;
    [SerializeField] private float max_radius;
    [SerializeField] private int segments;
    [SerializeField] private float radian_width;

    private bool active_expanding = false;
    private float current_radius;
    private Vector2 current_origin;

    public Vector2 direction;
    public float delay;

    private void Update()
    {
        if (active_expanding && delay<0)
        {
            current_radius = Mathf.Lerp(current_radius, max_radius, expansion_rate * Time.deltaTime);
            MakeWave(lineRenderer, current_radius, segments, current_origin, direction);
            if (current_radius > max_radius*0.95)
            {
                Destroy(gameObject);
            }
        }
        delay -= Time.deltaTime;
    }

    public void MakeWave()
    {
        print("attempt create wave");
        current_radius = 0;
        active_expanding = true;
        current_origin = transform.position;
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
