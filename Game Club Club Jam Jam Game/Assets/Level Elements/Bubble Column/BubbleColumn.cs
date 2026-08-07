using System.Collections;
using UnityEngine;

public class BubbleColumn : MonoBehaviour
{
    private Coroutine _burstRoutine;
    private bool _isEmitting;
    private Vector2 _originalSize;
    private Vector2 _originalOffset;
    private ParticleSystem.MainModule _bubbleParticlesMain;

    [SerializeField] private BoxCollider2D triggerCollider;
    [SerializeField] private float pushForce = 16f;
    [Tooltip("Time in seconds the bubble column is active.")]
    [SerializeField] private float burstTime = 2f;
    [Tooltip("Time between bursts.")]
    [SerializeField] private float burstCooldown = 4f;
    [Tooltip("Percentage of original height to lerp towards during burst.")]
    [SerializeField, Range(0, 1)] private float burstMinHeight = 1f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private ParticleSystem bubbleParticles;
    [Tooltip("Particle speed is set to pushForce*particleSpeedScale.")]
    [SerializeField] private float particleSpeedScale = 0.25f;
    [Tooltip("Particle lifetime is set to height*particleLifetimeScale.")]
    [SerializeField] private float particleLifetimeScale = 0.1f;
    [SerializeField] private AudioSource bubbleAudioSource;

    private void Awake()
    {
        _originalSize = triggerCollider.size;
        _originalOffset = triggerCollider.offset;
        if (bubbleParticles != null)
            _bubbleParticlesMain = bubbleParticles.main;
        _burstRoutine = StartCoroutine(BurstLoop());
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    public void Play()
    {
        if (_burstRoutine != null)
            return;
        _burstRoutine = StartCoroutine(BurstLoop());
    }

    public void Stop()
    {
        if (_burstRoutine == null)
            return;
        StopCoroutine(_burstRoutine);
        bubbleAudioSource.Stop();
        bubbleParticles.Stop();
        _burstRoutine = null;
    }

    private IEnumerator BurstLoop()
    {
        while (true)
        {
            _isEmitting = true;
            _bubbleParticlesMain.startSpeed = pushForce * particleSpeedScale;
            bubbleAudioSource.Play();
            bubbleParticles.Play();
            float time = burstTime;
            while (time > 0)
            {
                float t = time / burstTime;
                bubbleAudioSource.volume = t;
                float heightPercent = Mathf.Lerp(burstMinHeight, 1f, t);
                float targetHeight = _originalSize.y * heightPercent;
                float heightDiff = _originalSize.y - targetHeight;
                float targetOffsetY = _originalOffset.y - heightDiff * 0.5f;
                triggerCollider.size = new Vector2(_originalSize.x, targetHeight);
                triggerCollider.offset = new Vector2(_originalOffset.x, targetOffsetY);
                _bubbleParticlesMain.startLifetime = targetHeight * particleLifetimeScale;

                time -= Time.deltaTime;
                yield return null;
            }
            _isEmitting = false;
            bubbleAudioSource.Stop();
            bubbleParticles.Stop();

            yield return new WaitForSeconds(burstCooldown);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!_isEmitting || collision.isTrigger || collision.attachedRigidbody == null)
            return;

        collision.attachedRigidbody.AddForce(transform.up * pushForce, ForceMode2D.Force);
    }
}
