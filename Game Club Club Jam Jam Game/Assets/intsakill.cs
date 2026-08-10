using UnityEngine;

public class intsakill : MonoBehaviour
{
    HealthSystem healthSystem;
    public void Kill()
    {
        print("killbox entered");
        healthSystem = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthSystem>();
        healthSystem.AddHealth(-healthSystem.GetHealth());
    }
}
