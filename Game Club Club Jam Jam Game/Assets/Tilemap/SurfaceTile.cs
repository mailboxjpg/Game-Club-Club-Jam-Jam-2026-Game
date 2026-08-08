using UnityEngine;
using UnityEngine.Tilemaps;

public enum SurfaceType
{
    Normal,
    Poison,
    Rock,
    Sand,
    Breakable
}

[CreateAssetMenu(
    fileName = "SurfaceTile",
    menuName = "Tiles/Surface Tile"
)]
public class SurfaceTile : Tile
{
    [Header("Surface Tile")]
    public SurfaceType surfaceType = SurfaceType.Normal;
    public float damagePerSecond;
    public float movementMultiplier = 1f;
    public float bouncinessAddition = 0f;
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
}