using UnityEngine;

public class destory_particles : MonoBehaviour
{
    private void Start()
    {
        ParticleSystem parts;
        if (TryGetComponent<ParticleSystem>(out parts))
        {
            float totalDuration = parts.main.duration;
            Destroy(gameObject, totalDuration);
        }
    }
}
