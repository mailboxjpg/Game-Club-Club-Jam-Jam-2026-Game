using UnityEngine;

public class AirJumpCollectable : Collectible
{
    [Header("Air Jump")]
    [Tooltip("Whether this collectible adds the jump amount or raises the collector's air jump limit to the amount.")]
    [SerializeField] private bool addsJumps;
    [SerializeField] private int jumpAmount;

    public override int Collect(ShellCollector collector)
    {
        if (collector.characterController2D == null)
            return -1;
        if (addsJumps)
        {
            collector.characterController2D.maxAirJumps += jumpAmount;
        }
        else
        {
            if (jumpAmount > collector.characterController2D.maxAirJumps)
                collector.characterController2D.maxAirJumps = jumpAmount;
            else
                return -1;
        }
        return base.Collect(collector);
    }
}
