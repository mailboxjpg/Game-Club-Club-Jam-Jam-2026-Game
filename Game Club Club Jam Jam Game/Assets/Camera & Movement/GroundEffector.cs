using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundEffector : MonoBehaviour
{
    [SerializeField] private CharacterController2D characterController;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private AudioSource walkAudioSource;
    [Tooltip("Lerp between min and max based on move speed to set walk audio volume.")]
    [SerializeField] private Vector2 walkVolumeRange;
    [Tooltip("Lerp between min and max based on move speed to set walk audio pitch.")]
    [SerializeField] private Vector2 walkPitchRange;
    [SerializeField] private float maxSpeed;

    private Vector3Int _currentCell;
    private SurfaceTile _currentTile;
    private float _originalBounciness;

    private void Start()
    {
        _originalBounciness = characterController.GetBounciness();
        _currentCell = TilemapManager.Instance.WorldToCollisionCell(characterController.transform.position);
        _currentCell.y -= 1;
        UpdateSurfaceTile();
    }

    private void FixedUpdate()
    {
        if (!characterController.IsGrounded)
            return;
        if (characterController.Velocity.sqrMagnitude > 0.001f)
        {
            Vector3Int newCell = TilemapManager.Instance.WorldToCollisionCell(characterController.transform.position);
            newCell.y -= 1;
            if (_currentCell != newCell)
            {
                _currentCell = newCell;
                UpdateSurfaceTile();
            }
        }

        if (_currentTile != null)
        {
            healthSystem.AddHealth(-_currentTile.damagePerSecond * Time.fixedDeltaTime);
            if (walkAudioSource != null && _currentTile.walkClip != null)
            {
                float t = Mathf.Clamp01(Mathf.Abs(characterController.Velocity.x) / maxSpeed);
                walkAudioSource.volume = Mathf.Lerp(walkVolumeRange.x, walkVolumeRange.y, t);
                walkAudioSource.pitch = Mathf.Lerp(walkPitchRange.x, walkPitchRange.y, t);
            }
        }
    }

    private void UpdateSurfaceTile()
    {
        _currentTile = TilemapManager.Instance.GetCollisionTileAt(_currentCell) as SurfaceTile;
        if (_currentTile == null)
            return;
        characterController.movementMultiplier = _currentTile.movementMultiplier;
        characterController.SetBounciness(_originalBounciness + _currentTile.bouncinessAddition);
        if (walkAudioSource != null)
        {
            walkAudioSource.Stop();
            if (_currentTile.walkClip != null)
            {
                walkAudioSource.clip = _currentTile.walkClip;
                walkAudioSource.Play();
            }
        }
    }
}
