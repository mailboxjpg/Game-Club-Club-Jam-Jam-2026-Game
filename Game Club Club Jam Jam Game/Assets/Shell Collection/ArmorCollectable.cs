using UnityEngine;

public class ArmorCollectable : Collectable
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
        bool reducesDurability = setDurability && durability < collector.healthSystem.armorDurability;
        bool reducesArmor = !addsArmor && armorAmount <= collector.healthSystem.GetArmor();
        if (!overrideCurrentArmor && (reducesDurability || reducesArmor))
            return -1;
        if (addsArmor)
            collector.healthSystem.AddArmor(armorAmount);
        else
            collector.healthSystem.SetArmor(armorAmount);
        if (setDurability)
            collector.healthSystem.armorDurability = durability;
        return base.Collect(collector);
    }
}
