using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class EchoWavePoint : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Max number of times this point can reflect before it's destroyed outright.")]
    [SerializeField] private int maxReflections = 3;
    [Tooltip("Seconds this point stays alive before self-destructing, independent of distance traveled.")]
    [SerializeField] private float lifetime = 1.5f;
    [Tooltip("Light intensity at the start of life (x) and end of life (y); lerped over lifetime.")]
    [SerializeField] private Vector2 intensityOverLife = new Vector2(1.5f, 0f);
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float hitRadius = 0.25f;

    public Vector2 Velocity { get; set; }
    public bool DamageEnabled { get; set; } = true;

    private Light2D _light;
    private float _age;
    private int _reflectionsUsed;

    public System.Func<bool> HasWaveAlreadyHit;
    public System.Action OnWaveHit;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(Velocity * Time.deltaTime);

        float lifeT = Mathf.Clamp01(_age / lifetime);
        _light.intensity = Mathf.Lerp(intensityOverLife.x, intensityOverLife.y, lifeT);

        CheckForEnemyHit();
    }

    private void CheckForEnemyHit()
    {
        if (!DamageEnabled)
            return;
        // Mirrors the old "already_hit" flag shared across the whole wave: once anything in the
        // wave has hit an enemy, every point stops checking for further hits.
        if (HasWaveAlreadyHit != null && HasWaveAlreadyHit())
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                OnWaveHit?.Invoke();
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayer.value) == 0)
            return;

        _reflectionsUsed++;
        if (_reflectionsUsed > maxReflections)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 normal = GetSurfaceNormal(other);
        Velocity = Vector2.Reflect(Velocity, normal);
    }

    private Vector2 GetSurfaceNormal(Collider2D other)
    {
        Vector2 closest = other.ClosestPoint(transform.position);
        Vector2 fromSurface = (Vector2)transform.position - closest;

        if (fromSurface.sqrMagnitude < 0.0001f)
        {
            // Point is exactly on/inside the surface (rare); fall back to reversing velocity
            // rather than dividing by a near-zero vector.
            return -Velocity.normalized;
        }

        return fromSurface.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}