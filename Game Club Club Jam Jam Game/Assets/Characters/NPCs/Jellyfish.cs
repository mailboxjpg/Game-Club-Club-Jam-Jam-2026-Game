using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Jellyfish : MonoBehaviour
{
    [Tooltip("Speed applied at the moment of each thrust pulse.")]
    [SerializeField] private float thrustSpeed = 4f;
    [Tooltip("How quickly velocity decays back toward zero between pulses (acts as drag; higher = stops faster).")]
    [SerializeField] private float driftSpeed = 0.5f;
    [Tooltip("Random range (min, max) in seconds between thrust pulses.")]
    [SerializeField] private Vector2 thrustIntervalRange = new Vector2(1.5f, 3f);
    [Tooltip("Base direction the jellyfish pulses toward, e.g. (0, 1) for mostly upward. Doesn't need to be normalized.")]
    [SerializeField] private Vector2 thrustDirection = Vector2.up;
    [Tooltip("How much random deviation (in radians) is added to thrustDirection each pulse. 0 = always exactly thrustDirection.")]
    [SerializeField] private float directionRandomness = 0.1f;
    [Tooltip("Degrees per second the sprite rotates to face its current thrust direction.")]
    [SerializeField] private float rotateSpeed = 1f;

    private Rigidbody2D _rigidbody;
    private float _thrustTimer;
    private Vector2 _lastThrustDirection = Vector2.up;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.freezeRotation = true;
        _lastThrustDirection = thrustDirection.normalized;
        _thrustTimer = Random.Range(thrustIntervalRange.x, thrustIntervalRange.y);
    }

    // Update is called once per frame
    void Update()
    {
        _thrustTimer -= Time.deltaTime;
        if (_thrustTimer <= 0f)
        {
            Thrust();
            _thrustTimer = Random.Range(thrustIntervalRange.x, thrustIntervalRange.y);
        }

        // Drag: exponential decay toward zero velocity between pulses, so each thrust feels
        // like a pulse that gradually slows rather than an abrupt stop.
        _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, Vector2.zero, driftSpeed * Time.deltaTime);

        RotateTowardThrustDirection();
    }

    /// <summary>Fires a single pulse: picks a direction near thrustDirection (jittered by directionRandomness) and snaps velocity to thrustSpeed along it.</summary>
    private void Thrust()
    {
        Vector2 baseDir = thrustDirection.sqrMagnitude > 0.0001f ? thrustDirection.normalized : Vector2.up;

        // Rotate baseDir by a random angle within +-directionRandomness radians for organic variance.
        float angleJitter = Random.Range(-directionRandomness, directionRandomness);
        Vector2 jitteredDir = RotateVector(baseDir, angleJitter * Mathf.Rad2Deg);

        _lastThrustDirection = jitteredDir;
        _rigidbody.linearVelocity = transform.up * thrustSpeed;
    }

    /// <summary>Smoothly rotates the jellyfish to face its most recent thrust direction (assumes "up" is the sprite's forward).</summary>
    private void RotateTowardThrustDirection()
    {
        if (_lastThrustDirection.sqrMagnitude < 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(_lastThrustDirection.y, _lastThrustDirection.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = _rigidbody.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotateSpeed * Time.deltaTime);
        _rigidbody.MoveRotation(newAngle);
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 dir = thrustDirection.sqrMagnitude > 0.0001f ? thrustDirection.normalized : Vector2.up;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)dir * 1.5f);
    }
}