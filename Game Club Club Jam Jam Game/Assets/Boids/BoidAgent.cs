using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BoidAgent : MonoBehaviour {
    #region Fields
    
    [Header("Movement")]
    [SerializeField, Min(0.1f)] float maxSpeed = 7f;
    [SerializeField, Min(0.1f)] float accelerationForce = 18f;
    [SerializeField, Min(0.1f)] float rotationSharpness = 10f;
    
    [Header("Behavior Distances")]
    [SerializeField, Min(0.1f)] float separationDistance = 1.5f;
    [SerializeField, Min(0.1f)] float alignmentDistance = 3.5f;
    [SerializeField, Min(0.1f)] float cohesionDistance = 4.5f;
    
    [Header("Behavior Weights")]
    [SerializeField, Min(0f)] float separationWeight = 1.6f;
    [SerializeField, Min(0f)] float alignmentWeight = 1f;
    [SerializeField, Min(0f)] float cohesionWeight = 1.2f;
    [SerializeField, Min(0f)] float boundsWeight = 2.5f;
    [SerializeField, Min(0f)] float obstacleWeight = 3f;
    
    [Header("Neighbor Query")]
    [SerializeField] LayerMask neighborMask = ~0;

    [Header("Obstacle Avoidance")]
    [SerializeField] bool avoidObstacles = true;
    [SerializeField] LayerMask obstacleMask = ~0;
    [SerializeField, Min(0.1f)] float obstacleProbeRadius = 0.45f;
    [SerializeField, Min(0.5f)] float obstacleLookAhead = 3.5f;
    [SerializeField, Min(0.1f)] float floorClearance = 0.8f;
    
    [Header("Depth (Z) Isolation")]
    [Tooltip("All boids are placed on this fixed Z so they render/sort together and never physically interact with anything outside their own Z-layer (2D physics ignores Z, but this keeps them visually and spatially grouped and separable from the rest of the scene).")]
    [SerializeField] float boidZ = -1f;

    [Header("Consumption")]
    [Tooltip("Health granted to player when this boid is consumed.")]
    [SerializeField] float easyHealth = 1f;
    [SerializeField] float normalHealth = 0.5f; // Dont allow player to eat on hard mode
    [Tooltip("Tag of colliders that can eat this boid.")]
    [SerializeField] string eatTag = "Player";
    [SerializeField] GameObject eatParticles;

    Rigidbody2D rb;
    readonly List<BoidAgent> neighbors = new(32);
    
    Vector2 boundsCenter;
    Vector2 boundsSize = new(50f, 50f);
    bool useBounds = true;
    
    float separationDistanceSqr;
    float alignmentDistanceSqr;
    float cohesionDistanceSqr;
    float neighborScanRadius;
    Material materialInstance;

    bool offsetPulse;

    #endregion
    
    public Vector2 Velocity => rb ? rb.linearVelocity : Vector2.zero;
    [HideInInspector] public float pulseOffset;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearDamping = 0.2f;
                
        separationDistanceSqr = separationDistance * separationDistance;
        alignmentDistanceSqr = alignmentDistance * alignmentDistance;
        cohesionDistanceSqr = cohesionDistance * cohesionDistance;
        neighborScanRadius = Mathf.Max(separationDistance, Mathf.Max(alignmentDistance, cohesionDistance));
        materialInstance = GetComponent<SpriteRenderer>().material;
        pulseOffset = Random.Range(-100f, 100f);
        offsetPulse = true;
        materialInstance.SetFloat("_PulseOffset", pulseOffset);
    }

    protected void Start()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.01f)
            rb.linearVelocity = Random.insideUnitCircle.normalized * maxSpeed;

        // Snap onto the shared boid Z-plane so this boid only ever finds/interacts with other
        // boids via the 2D neighbor scan, and stays visually grouped/sortable as its own layer.
        var position = transform.position;
        position.z = boidZ;
        transform.position = position;
    }

    /// <summary>Configures a rectangular bounding area (center + full width/height) the boid will steer back inside of.</summary>
    public void ConfigureBounds(Vector2 center, Vector2 size, bool enabled = true)
    {
        boundsCenter = center;
        boundsSize = Vector2.Max(size, Vector2.one * 1f);
        useBounds = enabled;
    }

    public void ConfigureSpeed(float speed) => maxSpeed = Mathf.Max(0.1f, speed);

    public void ConfigureDepth(float z) => boidZ = z;

    void FindNeighbors()
    {
        neighbors.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll
        (
            transform.position,
            neighborScanRadius,
            neighborMask
        );

        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (!hit)
                continue;
            
            var other = hit.attachedRigidbody ? hit.attachedRigidbody.GetComponent<BoidAgent>() : hit.GetComponent<BoidAgent>();
            if (!other || other == this)
                continue;
            
            neighbors.Add(other);
        }
    }

    Vector2 ComputeSeparation()
    {
        Vector2 force = Vector2.zero;
        int count = 0;
        Vector2 position = transform.position;

        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector2 toOther = position - (Vector2)neighbors[i].transform.position;
            float sqrDistance = toOther.sqrMagnitude;
            if (sqrDistance > separationDistanceSqr || sqrDistance < 0.0001f)
                continue;
            
            force += toOther / sqrDistance;
            count++;
        }
        
        return count > 0 ? force / count : Vector2.zero;
    }

    Vector2 ComputeAlignment()
    {
        Vector2 averageVelocity = Vector2.zero;
        int count = 0;
        Vector2 position = transform.position;
        float totalOffset = 0f;

        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector2 offset = (Vector2)neighbors[i].transform.position - position;
            if (offset.sqrMagnitude > alignmentDistanceSqr)
                continue;
            totalOffset += neighbors[i].pulseOffset;

            averageVelocity += neighbors[i].Velocity;
            count++;
        }
        if (count > 3)
        {
            offsetPulse = false;
            float avgOffset = totalOffset / count;
            pulseOffset = avgOffset;
            materialInstance.SetFloat("_PulseOffset", pulseOffset);
        }
        else if (!offsetPulse)
        {
            offsetPulse = true;
            pulseOffset += Random.Range(-1f, 1f);
            materialInstance.SetFloat("_PulseOffset", pulseOffset);
        }
        
        return count > 0 ? (averageVelocity / count).normalized : (Vector2)transform.up;
    }

    Vector2 ComputeCohesion() {
        var center = Vector2.zero;
        var count = 0;
        Vector2 position = transform.position;

        for (var i = 0; i < neighbors.Count; i++)
        {
            Vector2 otherPosition = neighbors[i].transform.position;
            if ((otherPosition - position).sqrMagnitude > cohesionDistanceSqr)
                continue;

            center += otherPosition;
            count++;
        }
        
        return count > 0 ? (center / count - position).normalized : Vector2.zero;
    }

    /// <summary>
    /// Steers back toward the bounds center once the boid crosses an inner "soft" rectangle
    /// (85% of boundsSize), with strength ramping from 0 at the soft edge to 1 at the hard edge.
    /// Each axis is evaluated independently so a boid near a corner gets pushed diagonally back in.
    /// </summary>
    Vector2 ComputeBoundsSteer()
    {
        if (!useBounds)
            return Vector2.zero;

        Vector2 position = transform.position;
        Vector2 halfSize = boundsSize * 0.5f;
        Vector2 innerHalfSize = halfSize * 0.85f;
        Vector2 offset = position - boundsCenter;

        float strengthX = 0f;
        float pushX = 0f;
        float absX = Mathf.Abs(offset.x);
        if (absX > innerHalfSize.x)
        {
            strengthX = Mathf.InverseLerp(innerHalfSize.x, halfSize.x, absX);
            pushX = offset.x > 0f ? -1f : 1f;
        }

        float strengthY = 0f;
        float pushY = 0f;
        float absY = Mathf.Abs(offset.y);
        if (absY > innerHalfSize.y)
        {
            strengthY = Mathf.InverseLerp(innerHalfSize.y, halfSize.y, absY);
            pushY = offset.y > 0f ? -1f : 1f;
        }

        if (strengthX <= 0f && strengthY <= 0f)
            return Vector2.zero;

        Vector2 steer = new Vector2(pushX * strengthX, pushY * strengthY);
        return steer.sqrMagnitude > 1f ? steer.normalized * Mathf.Max(strengthX, strengthY) : steer;
    }

    Vector2 ComputeObstacleAvoidance()
    {
        if (!avoidObstacles)
            return Vector2.zero;
        
        var velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude < 0.0001f)
            return Vector2.zero;
        
        var direction = velocity.normalized;
        Vector2 position = transform.position;
        var avoidance = Vector2.zero;

        var forwardHit = Physics2D.CircleCast(position, obstacleProbeRadius, direction, obstacleLookAhead, obstacleMask);
        if (forwardHit.collider)
        {
            var awayFromHit = Vector2.Reflect(direction, forwardHit.normal).normalized;
            avoidance += awayFromHit;
        }

        var floorHit = Physics2D.Raycast(position, Vector2.down, floorClearance, obstacleMask);
        if (floorHit.collider)
        {
            var floorStrength = Mathf.InverseLerp(floorClearance, 0f, floorHit.distance);
            avoidance += Vector2.up * floorStrength;
        }
        
        return avoidance.normalized;
    }

    protected void FixedUpdate()
    {
        FindNeighbors();
        
        // This one block is the boids thesis: blend local influences and let emergence do the rest.
        var steering =
            ComputeSeparation() * separationWeight +
            ComputeAlignment() * alignmentWeight +
            ComputeCohesion() * cohesionWeight+
            ComputeBoundsSteer() * boundsWeight+
            ComputeObstacleAvoidance() * obstacleWeight;
        
        if (steering.sqrMagnitude < 0.0001f) steering = transform.up;
        var acceleration = steering.normalized * accelerationForce;
        var nextVelocity = rb.linearVelocity + acceleration * Time.fixedDeltaTime;
        rb.linearVelocity = Vector2.ClampMagnitude(nextVelocity, maxSpeed);

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            var targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            var newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSharpness * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }

        // Re-pin Z every step in case anything (parenting, external forces) nudges depth.
        if (!Mathf.Approximately(transform.position.z, boidZ))
        {
            var position = transform.position;
            position.z = boidZ;
            transform.position = position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.isTrigger || !collision.transform.CompareTag(eatTag) || SceneLoader.Instance.difficulty == Difficulty.Hard)
            return;
        if (collision.TryGetComponent<HealthSystem>(out var healthSystem) && healthSystem.GetHealth() < healthSystem.GetMaxHealth())
        {
            if (SceneLoader.Instance.difficulty == Difficulty.Easy)
                healthSystem.AddHealth(easyHealth);
            else
                healthSystem.AddHealth(normalHealth);
            Instantiate(eatParticles, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds)
            return;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}