using UnityEngine;
using UnityEngine.Events;

public class enemy_health : MonoBehaviour
{
    [SerializeField] float hp;
    [SerializeField] ParticleSystem blood_particles;

    public UnityEvent OnDeath;
    public UnityEvent OnHurt;

    public void deal_damage(float damage)
    {
        OnHurt.Invoke();
        if (blood_particles != null)
        {
            ParticleSystem particles = Instantiate(blood_particles, transform.position, transform.rotation);
        }
        if (hp < 0)
        {
            OnDeath.Invoke();
            Destroy(gameObject);
        }
    }
}
