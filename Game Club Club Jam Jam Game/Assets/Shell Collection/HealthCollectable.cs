using UnityEngine;

public class HealthCollectable : Collectable
{
    [Header("Health")]
    [Tooltip("Whether this collectable adds the health amount or raises the collector's health to the amount.")]
    [SerializeField] private bool addsHealth;
    [SerializeField] private float healthAmount;
    [Tooltip("Whether this collectable adds the max health amount or raises the collector's max health to the amount.")]
    [SerializeField] private bool addsMaxHealth;
    [Tooltip("Sets collector's max health to this.")]
    [SerializeField] private float maxHealthAmount;

    public override int Collect(ShellCollector collector)
    {
        if (collector.healthSystem == null)
            return -1;
        float collectorMaxHealth = collector.healthSystem.GetMaxHealth();
        bool doesNotIncreaseMaxHealth = !addsMaxHealth && maxHealthAmount <= collectorMaxHealth;
        float collectorHealth = collector.healthSystem.GetHealth();
        bool doesNotIncreaseHealth = collectorHealth >= collectorMaxHealth || (!addsHealth && healthAmount <= collectorHealth);
        if (doesNotIncreaseMaxHealth || doesNotIncreaseHealth)
            return -1;
        if (addsHealth)
            collector.healthSystem.AddHealth(healthAmount);
        else
            collector.healthSystem.SetHealth(healthAmount);
        if (addsMaxHealth)
            collector.healthSystem.AddMaxHealth(maxHealthAmount);
        else
            collector.healthSystem.SetMaxHealth(maxHealthAmount);
        return base.Collect(collector);
    }
}
