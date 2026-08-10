using UnityEngine;

public class EchoWaveVisuals : MonoBehaviour
{
    [Tooltip("Must have an EchoPoint component (and Light2D, per EchoPoint's requirement).")]
    [SerializeField] private EchoWavePoint pointPrefab;
    [Tooltip("Number of light points spread across the arc.")]
    [SerializeField] private int pointCount = 12;
    [Tooltip("Half-width of the arc in radians, centered on the fire direction (matches the old radian_width behavior).")]
    [SerializeField] private float radianWidth = 0.6f;
    [SerializeField] private float expansionSpeed = 8f;
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("If false, this wave never checks for or deals damage at all.")]
    [SerializeField] private bool damageEnabled = true;

    private EchoWavePoint[] _points;
    private float _delay;
    private bool _fired;
    private bool _waveHasHit;
    private Vector2 _pendingDirection;
    private float _pendingHitRadius;
    private float _pendingDamage;

    /// <summary>Fires the wave: spawns points across the arc facing `direction`, each moving outward at expansionSpeed. `delay` staggers the actual spawn (used by EchoLocationTool to fire multiple waves in sequence).</summary>
    public void Fire(Vector2 direction, float delay, float hitRadius, float damage)
    {
        _delay = delay;
        if (_delay <= 0f)
        {
            SpawnPoints(direction, hitRadius, damage);
        }
        else
        {
            // Stash for use once the delay elapses in Update.
            _pendingDirection = direction;
            _pendingHitRadius = hitRadius;
            _pendingDamage = damage;
        }
    }

    private void Update()
    {
        if (!_fired)
        {
            _delay -= Time.deltaTime;
            if (_delay <= 0f)
            {
                SpawnPoints(_pendingDirection, _pendingHitRadius, _pendingDamage);
            }
            return;
        }

        UpdateLine();

        if (AllPointsGone())
        {
            Destroy(gameObject);
        }
    }

    private void SpawnPoints(Vector2 direction, float hitRadius, float damage)
    {
        _fired = true;
        _points = new EchoWavePoint[pointCount];

        float focusRadian = Mathf.Atan2(direction.y, direction.x);

        for (int i = 0; i < pointCount; i++)
        {
            // Evenly spread across [-radianWidth, +radianWidth] around the aim direction.
            // Matches the old EchoWaveVisuals angle formula (pointCount == 1 handled below to
            // avoid a divide-by-zero when pointCount - 1 would be 0).
            float t = pointCount > 1 ? (float)i / (pointCount - 1) : 0.5f;
            float angle = focusRadian - radianWidth + t * radianWidth * 2f;

            Vector2 pointDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 spawnPos = transform.position;

            EchoWavePoint point = Instantiate(pointPrefab, spawnPos, Quaternion.identity, transform);
            point.Velocity = pointDir * expansionSpeed;
            point.DamageEnabled = damageEnabled;
            point.HasWaveAlreadyHit = () => _waveHasHit;
            point.OnWaveHit = OnAnyPointHit;

            _points[i] = point;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = pointCount;
        }
    }

    /// <summary>Shared "already hit" flag across the whole wave, matching the old EchoWaveVisuals behavior where only the first enemy touched by any segment takes damage.</summary>
    private void OnAnyPointHit()
    {
        _waveHasHit = true;
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || _points == null)
            return;

        // Only draw through points that are still alive; a reflected/expired point leaves a gap
        // rather than snapping the line back to the origin.
        int aliveCount = 0;
        for (int i = 0; i < _points.Length; i++)
        {
            if (_points[i] != null)
                aliveCount++;
        }

        if (aliveCount == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Vector3[] positions = new Vector3[aliveCount];
        int w = 0;
        for (int i = 0; i < _points.Length; i++)
        {
            if (_points[i] == null)
                continue;
            positions[w] = _points[i].transform.position;
            w++;
        }

        lineRenderer.positionCount = aliveCount;
        lineRenderer.SetPositions(positions);
    }

    private bool AllPointsGone()
    {
        if (_points == null)
            return false;

        for (int i = 0; i < _points.Length; i++)
        {
            if (_points[i] != null)
                return false;
        }
        return true;
    }
}