using System;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Tooltip("Amount of shells to award when collected.")]
    [SerializeField] private int shellAmount = 1;
    [SerializeField] private bool destroyOnCollect = true;
    [Tooltip("Audio clip to play when collected.")]
    [SerializeField] private AudioClip collectAudioClip;
    [Tooltip("Particle system to instantiate when collected.")]
    [SerializeField] private ParticleSystem collectParticlesPrefab;

    public Action OnCollect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int Collect()
    {
        if (collectAudioClip != null)
            AudioSource.PlayClipAtPoint(collectAudioClip, transform.position);
        if (collectParticlesPrefab != null)
            Instantiate(collectParticlesPrefab, transform.position, transform.rotation);
        OnCollect?.Invoke();
        if (destroyOnCollect)
            Destroy(gameObject);
        return shellAmount;
    }
}
