using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class GroundEffector : MonoBehaviour
{
    [SerializeField] private CharacterController2D characterController;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioClip defaultWalkClip;
    [SerializeField] private AudioClip defaultJumpClip;
    [SerializeField] private AudioClip defaultLandClip;
    [Tooltip("Lerp between min and max based on move speed to set walk audio volume.")]
    [SerializeField] private Vector2 walkVolumeRange;
    [Tooltip("Lerp between min and max based on move speed to set walk audio pitch.")]
    [SerializeField] private Vector2 walkPitchRange;
    [SerializeField] private float maxSpeed;
    [SerializeField] private FallDamageSetting[] fallDamageSettings;
    [SerializeField] private float defaultFallDamageScale = 0.25f;
    [SerializeField] private float defaultMinFallSpeed = 15f;

    [System.Serializable]
    private struct FallDamageSetting
    {
        [Tooltip("Surface type to use this fall damage scale with.")]
        public SurfaceType surfaceType;
        [Tooltip("Fall speed is multiplied by this to apply fall damage.")]
        public float scale;
        [Tooltip("Fall damage is only applied when fall speed is above this threshold.")]
        public float minSpeed;
    }
    private Dictionary<SurfaceType, FallDamageSetting> _fallDamageSettings = new Dictionary<SurfaceType, FallDamageSetting>();
    private Vector3Int _currentCell;
    private SurfaceTile _currentTile;
    private float _originalBounciness;

    private void Start()
    {
        foreach (FallDamageSetting fallDamageSetting in fallDamageSettings)
        {
            _fallDamageSettings.Add(fallDamageSetting.surfaceType, fallDamageSetting);
        }
        _originalBounciness = characterController.GetBounciness();
        _currentCell = TilemapManager.Instance.WorldToCollisionCell(characterController.transform.position);
        _currentCell.y -= 1;
        TryGetSurfaceTile();
        characterController.OnJumped += Jump;
        characterController.OnLanded += Land;
    }

    private void OnDestroy()
    {
        characterController.OnJumped -= Jump;
        characterController.OnLanded -= Land;
    }

    private void FixedUpdate()
    {
        // if (walkAudioSource != null)
        //     Debug.Log(walkAudioSource.isPlaying);
        if (!characterController.IsGrounded)
        {
            if (walkAudioSource != null)
            {
                walkAudioSource.Stop();
            }
            return;
        }

        if (characterController.Velocity.sqrMagnitude > 0.001f)
        {
            TryGetSurfaceTile();
        }

        if (_currentTile != null)
        {
            healthSystem.AddHealth(-_currentTile.damagePerSecond * Time.fixedDeltaTime);
        }
        if (walkAudioSource != null)
        {
            if (!walkAudioSource.isPlaying)
            {
                walkAudioSource.Play();
            }
            float t = Mathf.Clamp01(Mathf.Abs(characterController.Velocity.x) / maxSpeed);
            walkAudioSource.volume = Mathf.Lerp(walkVolumeRange.x, walkVolumeRange.y, t);
            walkAudioSource.pitch = Mathf.Lerp(walkPitchRange.x, walkPitchRange.y, t);
        }
    }

    private void TryGetSurfaceTile()
    {
        Vector3Int newCell = TilemapManager.Instance.WorldToCollisionCell(characterController.transform.position);
        newCell.y -= 1;
        if (_currentCell == newCell)
            return;
        _currentCell = newCell;
        _currentTile = TilemapManager.Instance.GetCollisionTileAt(_currentCell) as SurfaceTile;
        if (_currentTile == null)
        {
            if (walkAudioSource != null)
            {
                walkAudioSource.Stop();
                walkAudioSource.clip = defaultWalkClip;
                walkAudioSource.Play();
            }
            return;
        }
        characterController.movementMultiplier = _currentTile.movementMultiplier;
        characterController.SetBounciness(_originalBounciness + _currentTile.bouncinessAddition);
        if (walkAudioSource != null && _currentTile.walkClip != null && _currentTile.walkClip != walkAudioSource.clip)
        {
            walkAudioSource.Stop();
            walkAudioSource.clip = _currentTile.walkClip;
            walkAudioSource.Play();
        }
    }

    private void Land(Collider2D ground, float speed)
    {
        if (walkAudioSource == null)
            return;
        Debug.Log("LAND");
        TryGetSurfaceTile();
        if (_currentTile != null && _currentTile.landClip != null)
        {
            Debug.Log($"PLAYONESHOT LAND AUDIO: {_currentTile.surfaceType} {_currentTile.landClip}");
            AudioSource.PlayClipAtPoint(_currentTile.landClip, transform.position);
        }
        else if (defaultLandClip != null)
        {
            AudioSource.PlayClipAtPoint(defaultLandClip, transform.position);
        }

        if (_currentTile != null && _fallDamageSettings.TryGetValue(_currentTile.surfaceType, out var fallDamageSetting))
        {
            if (speed > fallDamageSetting.minSpeed)
                healthSystem.AddHealth(-speed * fallDamageSetting.scale);
            return;
        }
        if (speed > defaultMinFallSpeed)
            healthSystem.AddHealth(-speed * defaultFallDamageScale);
    }

    private void Jump()
    {
        if (walkAudioSource == null)
            return;
        Debug.Log("JUMP");

        if (_currentTile != null && _currentTile.jumpClip != null)
        {
            Debug.Log($"PLAYONESHOT JUMP AUDIO: {_currentTile.surfaceType} {_currentTile.landClip}");
            AudioSource.PlayClipAtPoint(_currentTile.jumpClip, transform.position);
        }
        else if (defaultJumpClip != null)
        {
            AudioSource.PlayClipAtPoint(defaultJumpClip, transform.position);
        }
    }
}
