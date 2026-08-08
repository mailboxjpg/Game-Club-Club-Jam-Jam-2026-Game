using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    public static TilemapManager Instance {get; private set;}

    [SerializeField] private Tilemap[] tilemaps;

    public int collisionTileMapIndex = -1;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"[{name}: TilemapManager] A instance already exists. Destroying this instance's gameObject. There should only be one TilemapManager per");
            return;
        }
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i].TryGetComponent<TilemapCollider2D>(out _))
            {
                collisionTileMapIndex = i;
                break;
            }
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Vector3Int WorldToCell(Vector3 worldPos, int tilemapIndex)
    {
        return tilemaps[tilemapIndex].WorldToCell(worldPos);
    }

    public Vector3Int WorldToCollisionCell(Vector3 worldPos)
    {
        return tilemaps[collisionTileMapIndex].WorldToCell(worldPos);
    }

    public TileBase GetCollisionTileAt(Vector3Int cell)
    {
        if (collisionTileMapIndex == -1)
            return null;
        return tilemaps[collisionTileMapIndex].GetTile(cell);
    }
}
