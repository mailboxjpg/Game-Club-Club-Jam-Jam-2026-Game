using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Collectible : MonoBehaviour
{
    [Header("Collectable")]
    [Tooltip("Amount of shells to award when collected.")]
    [SerializeField] protected int shellAmount = 1;
    [SerializeField] protected bool destroyOnCollect = true;
    [Tooltip("Audio clip to play when collected.")]
    [SerializeField] protected AudioClip collectAudioClip;
    [Tooltip("Particle system to instantiate when collected.")]
    [SerializeField] protected ParticleSystem collectParticlesPrefab;
    [SerializeField] protected bool overrideParticleColor;
    [SerializeField] protected Color particleColor;

    [Header("Pulse")]
    [SerializeField] private Light2D pulseLight;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minPulseIntensity = 0.5f;
    [SerializeField] private float maxPulseIntensity = 1f;

    [Header("Popup")]    
    [Tooltip("Popup to instantiate when the player collects this.")]
    [SerializeField] private Popup popupPrefab;
    [SerializeField] private float popupFadeTime = 0.15f;
    [Tooltip("Seconds the popup is active before getting destroyed.")]
    [SerializeField] private float popupActiveTime = 2f;
    [Tooltip("Alpha of the popup box lerps between min (x) and max (y).")]
    [SerializeField] private Vector2 popupFadeAlphaRange = new Vector2(0f, 1f);
    [SerializeField] private string popupMessage;
    [SerializeField] private Color popupTextColor = Color.white;
    [Tooltip("Sets the popup's world space target to this transform.")]
    [SerializeField] private bool popupIsWorldSpace;
    [Tooltip("Popup drifts with this velocity (world space only).")]
    public Vector3 popupDriftVelocity;

    public UnityEvent OnCollect;

    private float pulseT;

    private void Update()
    {
        if (pulseLight == null)
            return;

        float pulse = (Mathf.Sin(pulseT) + 1f) * 0.5f;
        pulseLight.intensity = Mathf.Lerp(minPulseIntensity, maxPulseIntensity, pulse);
        pulseT += Time.deltaTime * pulseSpeed;
    }

    public virtual int Collect(ShellCollector collector)
    {
        if (collectAudioClip != null)
            AudioSource.PlayClipAtPoint(collectAudioClip, transform.position);
        if (collectParticlesPrefab != null)
        {
            ParticleSystem particles = Instantiate(collectParticlesPrefab, transform.position, transform.rotation);
            if (overrideParticleColor)
            {
                ParticleSystem.MainModule particleMain = particles.main;
                particleMain.startColor = particleColor;
            }
        }
        if (popupPrefab != null)
        {
            Popup newPopup = Instantiate(popupPrefab);
            if (popupIsWorldSpace)
                newPopup.worldSpaceTarget = transform;
            newPopup.fadeTime = popupFadeTime;
            newPopup.activeTime = popupActiveTime;
            newPopup.fadeAlphaRange = popupFadeAlphaRange;
            newPopup.driftVelocity = popupDriftVelocity;
            if (!string.IsNullOrEmpty(popupMessage))
            {
                newPopup.SetText(popupMessage, popupTextColor);
            }
            newPopup.StartPopup();
        }
        OnCollect?.Invoke();
        if (destroyOnCollect)
            Destroy(gameObject);
        return shellAmount;
    }

    // Functions that can be used for events
    public void SpawnPrefab(GameObject target)
    {
        Instantiate(target, transform.position, transform.rotation);
    }
}
