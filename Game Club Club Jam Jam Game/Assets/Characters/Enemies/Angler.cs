using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
public class AnglerFish : MonoBehaviour
{
    private enum State { Idle, SwoopIn, Biting, SwoopOut }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("The light that acts as the lure.")]
    [SerializeField] private Light2D lureLight;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobFrequency = 0.8f;
    [Tooltip("Small horizontal drift amplitude, layered on top of the vertical bob for a lazier sway.")]
    [SerializeField] private float swayAmplitude = 0.1f;
    [SerializeField] private float swayFrequency = 0.4f;

    [Header("Lure Light")]
    [SerializeField] private float lureNormalRadius = 4f;
    [SerializeField] private float lureMinIntensity = 0.15f;
    [SerializeField] private float lureMaxIntensity = 0.5f;
    [SerializeField] private float lurePulseSpeed = 1.5f;
    [SerializeField] private float lureAttackRadius = 6f;
    [Tooltip("Intensity the lure snaps/lerps to while attacking.")]
    [SerializeField] private float lureAttackIntensity = 1f;
    [SerializeField] private float lureAttackLerpSpeed = 6f;

    [Header("Attack")]
    [SerializeField] private float swoopInSpeed = 14f;
    [SerializeField] private float swoopOutSpeed = 10f;
    [SerializeField] private float biteRange = 0.6f;
    [SerializeField] private float biteOffset = 0.5f;
    [Tooltip("Damage dealt on a successful bite.")]
    [SerializeField] private int biteDamage = 10;
    [Tooltip("Safety timeout: if the fish hasn't reached target this many seconds into a state, it gives up and swoops away.")]
    [SerializeField] private float swoopTimeout = 2.5f;
    [Tooltip("How long the bite 'lunge stop' lasts before swooping away.")]
    [SerializeField] private float biteHoldTime = 0.15f;
    [Tooltip("Cooldown after fully returning to idle before the attack trigger can fire again.")]
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("Layers to listen for in attack trigger.")]
    [SerializeField] private LayerMask attackLayers = -1;
    [Tooltip("Tags to listen for in attack trigger.")]
    [SerializeField] private string[] attackTags;

    [Header("Flee (post-bite)")]
    [Tooltip("After biting, the fish swoops toward a random point within this radius of its current position.")]
    [SerializeField] private float fleeMinRadius = 4f;
    [Tooltip("After biting, the fish swoops toward a random point within this radius of its current position.")]
    [SerializeField] private float fleeMaxRadius = 8f;
    [Tooltip("How close (world units) to the flee target counts as 'arrived', at which point it settles into idle bobbing from there.")]
    [SerializeField] private float fleeArrivalThreshold = 0.15f;

    private State _state = State.Idle;
    private Vector3 _origin;
    private float _bobSeed;
    private Transform _target;
    private HealthSystem _targetHealth;
    private bool _hasBitten;
    private float _cooldownTimer;
    private HashSet<string> _attackTags = new HashSet<string>();
    private Vector2 _fleeTarget;
    private Vector2 _bobFaceDir = Vector2.left;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        _origin = transform.position;
        // Randomize per-instance so a group of anglers doesn't bob in lockstep.
        _bobSeed = Random.Range(0f, 100f);
        foreach (string tag in attackTags)
        {
            _attackTags.Add(tag);
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        UpdateLureLight();
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case State.Idle:
                DoIdleBob();
                break;
            case State.SwoopIn:
                DoSwoopIn();
                break;
            case State.Biting:
                // Held in place briefly by the coroutine driving the bite; no movement here.
                rb.linearVelocity = Vector2.zero;
                break;
            case State.SwoopOut:
                DoSwoopOut();
                break;
        }
    }

    private void DoIdleBob()
    {
        float t = Time.time + _bobSeed;
        float y = _origin.y + Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        float x = _origin.x + Mathf.Sin(t * swayFrequency * Mathf.PI * 2f) * swayAmplitude;
        Vector2 targetPos = new Vector2(x, y);

        Vector2 toTarget = targetPos - rb.position;
        // Move via velocity rather than snapping the transform so it still respects physics/collisions.
        rb.linearVelocity = toTarget / Time.fixedDeltaTime;
        
        FaceDirection(_bobFaceDir);
    }

    private void DoSwoopIn()
    {
        if (_target == null)
        {
            BeginSwoopOut();
            return;
        }

        Vector2 toTarget = _target.position - (transform.position + transform.right * biteOffset); // mouth area
        float sqrDst = toTarget.sqrMagnitude;

        FaceDirection(toTarget);

        if (sqrDst <= biteRange * biteRange)
        {
            Bite();
            return;
        }

        rb.linearVelocity = toTarget.normalized * swoopInSpeed;
    }

    private void DoSwoopOut()
    {
        Vector2 toFleeTarget = _fleeTarget - rb.position;
        float sqrDst = toFleeTarget.sqrMagnitude;

        if (sqrDst <= fleeArrivalThreshold * fleeArrivalThreshold)
        {
            rb.linearVelocity = Vector2.zero;
            // Fish now idles/bobs around wherever it ended up rather than its original spawn point.
            _origin = rb.position;
            _state = State.Idle;
            return;
        }

        FaceDirection(toFleeTarget);
        rb.linearVelocity = toFleeTarget.normalized * swoopOutSpeed;
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return;
        transform.right = dir.normalized;
    }

    private void UpdateLureLight()
    {
        if (lureLight == null)
            return;

        if (_state == State.SwoopIn)
        {
            // Attacking: lerp up to a bright, steadier attack intensity.
            lureLight.intensity = Mathf.Lerp(lureLight.intensity, lureAttackIntensity, lureAttackLerpSpeed * Time.deltaTime);
            lureLight.pointLightOuterRadius = lureAttackRadius;
        }
        else
        {
            // Dim pulse: sine wave mapped from [-1,1] to [lureMinIntensity, lureMaxIntensity].
            float pulse = (Mathf.Sin(Time.time * lurePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float target = Mathf.Lerp(lureMinIntensity, lureMaxIntensity, pulse);
            lureLight.intensity = target;
            lureLight.pointLightOuterRadius = lureNormalRadius;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != State.Idle || _cooldownTimer > 0f || !_attackTags.Contains(other.tag) || (attackLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        HealthSystem health = other.GetComponentInParent<HealthSystem>();
        if (health == null)
            return;

        BeginAttack(other.transform, health);
    }

    private void BeginAttack(Transform target, HealthSystem targetHealth)
    {
        _target = target;
        _targetHealth = targetHealth;
        _hasBitten = false;
        _state = State.SwoopIn;
        StopAllCoroutines();
        StartCoroutine(SwoopTimeoutRoutine());
    }

    private IEnumerator SwoopTimeoutRoutine()
    {
        yield return new WaitForSeconds(swoopTimeout);
        if (_state == State.SwoopIn || _state == State.SwoopOut)
        {
            BeginSwoopOut();
        }
    }

    private void Bite()
    {
        if (_hasBitten)
            return;
        _hasBitten = true;
        _state = State.Biting;
        rb.linearVelocity = Vector2.zero;

        if (_targetHealth != null)
        {
            _targetHealth.AddHealth(-biteDamage);
        }

        StopAllCoroutines();
        StartCoroutine(BiteHoldRoutine());
    }

    private IEnumerator BiteHoldRoutine()
    {
        yield return new WaitForSeconds(biteHoldTime);
        BeginSwoopOut();
    }

    private void BeginSwoopOut()
    {
        _state = State.SwoopOut;
        _target = null;
        _targetHealth = null;
        _cooldownTimer = attackCooldown;

        // Pick a random point within fleeRadius of the fish's current position to swoop toward,
        // instead of returning to its original spawn point.
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(fleeMinRadius, fleeMaxRadius);
        _fleeTarget = rb.position + randomOffset;
        if (Random.value < 0.5f)
            _bobFaceDir = Vector2.left;
        else
            _bobFaceDir = Vector2.right;
        StartCoroutine(SwoopTimeoutRoutine());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? _origin : transform.position, bobAmplitude);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.right * biteOffset, biteRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fleeMinRadius);
        Gizmos.DrawWireSphere(transform.position, fleeMaxRadius);
    }
}