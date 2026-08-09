using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Transforms to move between, in order. Position and rotation are both lerped.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [Tooltip("If true, start at waypoints[startIndex] instead of the platform's current transform.")]
    [SerializeField] private bool snapToFirstWaypointOnStart = true;
    [SerializeField] private int startIndex = 0;

    [Header("Movement")]
    [Tooltip("Units per second the platform travels toward the next waypoint (position lerp speed).")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("Degrees per second the platform rotates toward the next waypoint's rotation.")]
    [SerializeField] private float rotationSpeed = 180f;
    [Tooltip("If true, once the last waypoint is reached the platform teleports back to the first and continues forward. If false, it reverses direction and ping-pongs back through the list.")]
    [SerializeField] private bool wrapAround = false;
    [Tooltip("Distance from a waypoint's position considered 'arrived', so float imprecision doesn't stall movement.")]
    [SerializeField] private float arrivalThreshold = 0.02f;
    [Tooltip("Degrees from a waypoint's rotation considered 'arrived'.")]
    [SerializeField] private float rotationArrivalThreshold = 0.5f;
    [Tooltip("Optional pause at each waypoint before continuing to the next.")]
    [SerializeField] private float waitTimeAtWaypoint = 0f;

    [Header("Activation")]
    [Tooltip("If true, the platform starts moving automatically on Start(). If false, it stays paused until StartMoving() is called (e.g. from a trigger).")]
    [SerializeField] private bool startMoving = true;

    [Header("Riders")]
    [Tooltip("If true, Rigidbody2Ds resting on top of the platform are moved along with it (position delta + rotation delta applied each FixedUpdate).")]
    [SerializeField] private bool carryRiders = true;
    [Tooltip("Layer(s) eligible to be carried as riders. Leave as Everything if unsure.")]
    [SerializeField] private LayerMask riderLayer = ~0;
    [Tooltip("How a rider is detected as 'standing on' the platform.")]
    [SerializeField] private RiderDetectionMode riderDetectionMode = RiderDetectionMode.CollisionContacts;
    [Tooltip("Used only in OverlapBox mode: box placed just above the platform's collider to detect riders.")]
    [SerializeField] private Vector2 riderCheckSize = new Vector2(1f, 0.1f);
    [Tooltip("Used only in OverlapBox mode: local offset of the rider check box from the platform's position (should sit just above the platform surface).")]
    [SerializeField] private Vector2 riderCheckOffset = new Vector2(0f, 0.5f);

    public enum RiderDetectionMode { CollisionContacts, OverlapBox }

    private Rigidbody2D rb;
    public bool IsMoving { get; private set; }
    private int _currentIndex;
    private int _direction = 1; // +1 forward through the list, -1 backward (ping-pong mode only)
    private float _waitTimer;
    private Vector2 _lastPosition;

    // Riders currently in contact this physics step, used by CollisionContacts mode.
    private readonly HashSet<Rigidbody2D> _currentRiders = new HashSet<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // platforms should not be pushed around by physics
    }

    private void Start()
    {
        if (waypoints.Count == 0)
        {
            enabled = false;
            return;
        }

        _currentIndex = Mathf.Clamp(startIndex, 0, waypoints.Count - 1);

        if (snapToFirstWaypointOnStart)
        {
            Transform wp = waypoints[_currentIndex];
            rb.position = wp.position;
            rb.rotation = wp.eulerAngles.z;
        }

        IsMoving = startMoving;
    }

    private void FixedUpdate()
    {
        if (waypoints.Count < 2)
            return;

        Vector2 platformDelta = Vector2.zero;
        float rotationDelta = 0f;

        if (IsMoving)
        {
            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.fixedDeltaTime;
            }
            else
            {
                float oldRotation = rb.rotation;

                platformDelta = MoveTowardTarget();

                // Use the actual requested rotation change.
                rotationDelta = Mathf.DeltaAngle(
                    oldRotation,
                    rb.rotation
                );
            }
        }

        if (carryRiders)
        {
            if (riderDetectionMode == RiderDetectionMode.OverlapBox)
                DetectRidersOverlap();
            
            CarryRiders(platformDelta, rotationDelta);
        }
        _lastPosition = rb.position;
    }

    /// <summary>Resumes movement along the waypoint path from wherever it currently is.</summary>
    public void StartMoving()
    {
        IsMoving = true;
    }

    /// <summary>Pauses movement in place. Riders already on the platform are still carried if it's nudged, but the platform itself stops advancing toward its target waypoint.</summary>
    public void StopMoving()
    {
        IsMoving = false;
    }

    private Vector2 MoveTowardTarget()
    {
        int targetIndex = GetTargetIndex();
        Transform target = waypoints[targetIndex];

        Vector2 oldPosition = rb.position;

        Vector2 newPos = Vector2.MoveTowards(
            rb.position,
            target.position,
            moveSpeed * Time.fixedDeltaTime
        );

        float newRot = Mathf.MoveTowardsAngle(
            rb.rotation,
            target.eulerAngles.z,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);
        rb.MoveRotation(newRot);

        bool positionArrived = (newPos - (Vector2)target.position).sqrMagnitude <= arrivalThreshold * arrivalThreshold;

        bool rotationArrived = Mathf.Abs(Mathf.DeltaAngle(newRot, target.eulerAngles.z)) <= rotationArrivalThreshold;

        if (positionArrived && rotationArrived)
        {
            _currentIndex = targetIndex;
            AdvanceIndex();
            _waitTimer = waitTimeAtWaypoint;
        }

        return newPos - oldPosition;
    }

    /// <summary>Which waypoint we're currently lerping toward.</summary>
    private int GetTargetIndex()
    {
        if (wrapAround)
        {
            int next = _currentIndex + 1;
            return next >= waypoints.Count ? 0 : next;
        }
        else
        {
            int next = _currentIndex + _direction;
            if (next < 0 || next >= waypoints.Count)
            {
                // Shouldn't normally be hit since AdvanceIndex flips direction at the ends,
                // but guard against a single-waypoint-remaining edge case.
                return _currentIndex;
            }
            return next;
        }
    }

    /// <summary>Called once we've arrived at waypoints[_currentIndex]; decides where to head next.</summary>
    private void AdvanceIndex()
    {
        if (wrapAround)
        {
            if (_currentIndex >= waypoints.Count - 1)
            {
                // Snap to the first waypoint exactly (teleport) and continue forward from there.
                _currentIndex = 0;
                rb.position = waypoints[0].position;
                rb.rotation = waypoints[0].eulerAngles.z;
            }
            // else: _currentIndex already sits at the waypoint just reached; GetTargetIndex handles +1 next call.
        }
        else
        {
            if (_currentIndex >= waypoints.Count - 1)
            {
                _direction = -1;
            }
            else if (_currentIndex <= 0)
            {
                _direction = 1;
            }
        }
    }

    private void CarryRiders(Vector2 posDelta, float rotDelta)
    {
        if (posDelta.sqrMagnitude < 0.0000001f &&
            Mathf.Abs(rotDelta) < 0.0001f)
            return;

        foreach (Rigidbody2D rider in _currentRiders)
        {
            if (rider == null)
                continue;
            if (Mathf.Abs(rotDelta) > 0.0001f)
            {
                Vector2 offset = rider.position - rb.position;
                Vector2 rotatedOffset = RotateVector(offset, rotDelta);

                Vector2 rotationCorrection = rotatedOffset - offset;

                Vector2 targetVel = (posDelta + rotationCorrection) / Time.fixedDeltaTime;
                targetVel.y = Mathf.Min(0, targetVel.y);
                rider.linearVelocity += targetVel;

                rider.MoveRotation(rider.rotation + rotDelta);
            }
            else
            {
                // This shit is scuffed
                Vector2 targetVel = posDelta / Time.fixedDeltaTime;
                targetVel.y = Mathf.Min(0, targetVel.y);
                rider.linearVelocity += targetVel;
            }
        }
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void DetectRidersOverlap()
    {
        _currentRiders.Clear();
        Vector2 worldOffset = RotateVector(riderCheckOffset, rb.rotation);
        Vector2 checkCenter = rb.position + worldOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, riderCheckSize, rb.rotation, riderLayer);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D riderRb = hit.attachedRigidbody;
            if (riderRb != null && riderRb != rb)
            {
                _currentRiders.Add(riderRb);
            }
        }
    }

    // CollisionContacts mode: rely on actual physics contacts so only things genuinely
    // resting on the platform (not just nearby) get carried, and normal detach works for free.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (riderDetectionMode != RiderDetectionMode.CollisionContacts)
            return;
        if (!IsInRiderLayer(collision.gameObject.layer))
            return;
        if (IsStandingOnTop(collision))
        {
            _currentRiders.Add(collision.rigidbody);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (riderDetectionMode != RiderDetectionMode.CollisionContacts)
            return;
        if (!IsInRiderLayer(collision.gameObject.layer))
            return;

        if (IsStandingOnTop(collision))
        {
            _currentRiders.Add(collision.rigidbody);
        }
        else
        {
            _currentRiders.Remove(collision.rigidbody);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (riderDetectionMode != RiderDetectionMode.CollisionContacts)
            return;
        _currentRiders.Remove(collision.rigidbody);
    }

    /// <summary>True if any contact normal points roughly upward from the platform's perspective, meaning the other body is resting on top rather than hitting a side/underside.</summary>
    private bool IsStandingOnTop(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Contact normal points away from the platform surface; "up" relative to platform rotation.
            Vector2 up = RotateVector(Vector2.up, rb.rotation);
            if (-Vector2.Dot(contact.normal, up) > 0.5f)
                return true;
        }
        return false;
    }

    private bool IsInRiderLayer(int layer)
    {
        return (riderLayer.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null)
                continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
            int nextI = i + 1;
            if (nextI < waypoints.Count && waypoints[nextI] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextI].position);
            }
            else if (wrapAround && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }

        if (riderDetectionMode == RiderDetectionMode.OverlapBox)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + (Vector3)(Quaternion.Euler(0, 0, transform.eulerAngles.z) * riderCheckOffset);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, riderCheckSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}