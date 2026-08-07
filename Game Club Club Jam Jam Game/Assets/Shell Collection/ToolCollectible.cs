using UnityEngine;

public class ToolCollectible : Collectible
{
    [Header("Tool")]
    [Tooltip("Tool to spawn on the player when collected.")]
    [SerializeField] protected GameObject toolPrefab;
    
    public override int Collect(ShellCollector collector)
    {
        Transform newTool = Instantiate(toolPrefab, PlayerControl.Instance.transform).transform;
        newTool.SetPositionAndRotation(PlayerControl.Instance.transform.position, PlayerControl.Instance.transform.rotation);
        return base.Collect(collector);
    }
}
