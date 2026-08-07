using UnityEngine;

public class echo_ring_visual : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int expansion_rate;
    [SerializeField] float max_radius;
    [SerializeField] int segments;
    [SerializeField] Vector2 direction;
    [SerializeField] float dot_product_multiplier;

    bool active_expanding = false;
    float current_radius;
    Vector2 current_origin;
    public void _Makering()
    {
        print("attempt create ring");
        current_radius = 0;
        active_expanding=true;
        current_origin = transform.position;
    }

    private void Update()
    {
        if(active_expanding)
        {
            current_radius = Mathf.Lerp(current_radius,max_radius,expansion_rate*Time.deltaTime);
            MakeRing(lineRenderer, current_radius, segments, current_origin, direction);
            if(current_radius > max_radius)
            {
                active_expanding = false;
            }
        }
    }

    public void MakeRing(LineRenderer lineRenderer, float radius, int segments, Vector2 origin, Vector2 direction)
    {
        Vector3[] points = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i * Mathf.PI * 2) / segments;

            Vector2 point_vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float dot_product = Vector2.Dot(direction, point_vector) * dot_product_multiplier;

            float x = origin.x + point_vector.x * dot_product * radius;
            float y = origin.y + point_vector.y * dot_product * radius;

            points[i] = new Vector3(x, y, 5);
        }

        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(points);
    }
}
