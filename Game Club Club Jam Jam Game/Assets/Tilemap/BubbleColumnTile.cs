using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "BubbleColumnTile",
    menuName = "Tiles/Bubble Column Tile"
)]
public class BubbleColumnTile : SurfaceTile
{
    [Header("Bubble Column Collider")]
    public Vector2 size = new Vector2(0.75f, 8f);
    [Tooltip("Total number of tiles to the right of this tile (big has 1 small has 0).")]
    public int totalTilesRight = 0;

    [Header("Bubble Column Script Settings")]
    public float pushForce = 16f;
    [Tooltip("Time in seconds the bubble column is active.")]
    public float burstTime = 2f;
    [Tooltip("Time between bursts.")]
    public float burstCooldown = 4f;
    [Tooltip("Percentage of original height to lerp towards during burst.")]
    [Range(0, 1)]
    public float burstMinHeight = 0.75f;
    [Tooltip("Speed at which to expand the trigger collider's height.")]
    public float burstHeightSpeed = 2f;
    [Tooltip("Particle speed is set to pushForce*particleSpeedScale.")]
    public float particleSpeedScale = 0.25f;
    [Tooltip("Particle lifetime is set to height*particleLifetimeScale.")]
    public float particleLifetimeScale = 0.1f;

    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        BubbleColumn bubbleColumn = go.GetComponent<BubbleColumn>();
        bubbleColumn.triggerCollider.size = size;
        Vector2 offset = new Vector2(totalTilesRight * 0.5f, size.y * 0.5f);
        bubbleColumn.triggerCollider.offset = offset;
        bubbleColumn.pushForce = pushForce;
        bubbleColumn.burstTime = burstTime;
        bubbleColumn.burstCooldown = burstCooldown;
        bubbleColumn.burstMinHeight = burstMinHeight;
        bubbleColumn.burstHeightSpeed = burstHeightSpeed;
        bubbleColumn.particleSpeedScale = particleSpeedScale;
        bubbleColumn.particleLifetimeScale = particleLifetimeScale;
        
        return base.StartUp(position, tilemap, go);
    }
}