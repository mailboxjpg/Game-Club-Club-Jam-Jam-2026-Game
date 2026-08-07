using System;
using UnityEngine;
using UnityEngine.Events;

public class Collectable : MonoBehaviour
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

    public UnityEvent OnCollect;

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
