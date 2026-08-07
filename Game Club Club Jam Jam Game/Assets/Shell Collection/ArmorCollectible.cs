using UnityEngine;

public class ArmorCollectible : Collectible
{
    [Header("Armor")]
    [Tooltip("Whether this collectable adds the armor amount or raises the collector's armor to the amount.")]
    [SerializeField] private bool addsArmor;
    [SerializeField] private float armorAmount;
    [Tooltip("Whether this collectable sets collector's armor durability.")]
    [SerializeField] private bool setDurability;
    [Tooltip("Sets collector's armor durability to this.")]
    [SerializeField] private float durability;
    [Tooltip("Will override the collector's armor regardless if the current armor's durability is better.")]
    [SerializeField] private bool overrideCurrentArmor;

    public override int Collect(ShellCollector collector)
    {
        if (collector.healthSystem == null)
            return -1;
        bool shouldCollect = false;
        if (addsArmor)
        {
            collector.healthSystem.AddArmor(armorAmount);
            shouldCollect = true;
        }
        else if (armorAmount > collector.healthSystem.GetArmor())
        {
            collector.healthSystem.SetArmor(armorAmount);
            shouldCollect = true;
        }

        if (setDurability && durability > collector.healthSystem.armorDurability)
        {
            collector.healthSystem.armorDurability = durability;
            shouldCollect = true;
        }
        if (!shouldCollect)
            return -1;
        return base.Collect(collector);
    }
}
