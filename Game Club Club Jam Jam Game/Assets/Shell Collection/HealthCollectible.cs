using UnityEngine;

public class HealthCollectible : Collectible
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
        bool shouldCollect = false;
        if (addsMaxHealth)
        {
            collector.healthSystem.AddMaxHealth(maxHealthAmount);
            shouldCollect = true;
        }
        else if (maxHealthAmount > collector.healthSystem.GetMaxHealth())
        {
            collector.healthSystem.SetMaxHealth(maxHealthAmount);
            shouldCollect = true;
        }

        if (addsHealth)
        {
            collector.healthSystem.AddHealth(healthAmount);
            shouldCollect = true;

        }
        else if (healthAmount > collector.healthSystem.GetHealth())
        {
            collector.healthSystem.SetHealth(healthAmount);
            shouldCollect = true;
        }
        if (!shouldCollect)
            return -1;
        return base.Collect(collector);
    }
}
