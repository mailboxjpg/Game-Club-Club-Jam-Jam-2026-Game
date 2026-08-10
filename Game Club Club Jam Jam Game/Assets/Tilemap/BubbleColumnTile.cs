using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "BubbleColumnTile",
    menuName = "Tiles/Bubble Column Tile"
)]
public class BubbleColumnTile : SurfaceTile
{
    [Header("Bubble Column Tile")]
    public float pushForce;
}