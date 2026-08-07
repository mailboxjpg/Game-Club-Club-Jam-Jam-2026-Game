using UnityEngine;

public class DashCollectible : Collectible
{
    [Header("Dash")]
    [Tooltip("Whether this collectible adds the dash amount or raises the collector's dash limit to the amount.")]
    [SerializeField] private bool addsDashes;
    [SerializeField] private int dashAmount;

    public override int Collect(ShellCollector collector)
    {
        if (collector.characterController2D == null)
            return -1;
        if (addsDashes)
        {
            collector.characterController2D.maxDashes += dashAmount;
        }
        else
        {
            if (dashAmount > collector.characterController2D.maxDashes)
                collector.characterController2D.maxDashes = dashAmount;
            else
                return -1;
        }
        return base.Collect(collector);
    }
}
