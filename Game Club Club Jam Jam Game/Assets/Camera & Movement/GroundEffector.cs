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
    [SerializeField] private float defaultMinFallHeight = 6f;
    [SerializeField] private Vector2 oneShotPitchRange;

    [System.Serializable]
    private struct FallDamageSetting
    {
        [Tooltip("Surface type to use this fall damage scale with.")]
        public SurfaceType surfaceType;
        [Tooltip("Fall height is multiplied by this to apply fall damage.")]
        public float scale;
        [Tooltip("Fall damage is only applied when fall height is above this threshold.")]
        public float minHeight;
    }
    private Dictionary<SurfaceType, FallDamageSetting> _fallDamageSettings = new Dictionary<SurfaceType, FallDamageSetting>();
    private Vector3Int _currentCell;
    private SurfaceTile _currentTile;
    private float _originalBounciness;
    private float _maxAirY = -999999f;

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
        characterController.OnDashed += Dash;
    }

    private void OnDestroy()
    {
        characterController.OnJumped -= Jump;
        characterController.OnLanded -= Land;
        characterController.OnDashed -= Dash;
    }

    private void FixedUpdate()
    {
        if (!characterController.IsGrounded)
        {
            if (walkAudioSource != null)
            {
                walkAudioSource.Stop();
            }
            _maxAirY = Mathf.Max(_maxAirY, transform.position.y);
            return;
        }

        if (characterController.Velocity.sqrMagnitude > 0.001f)
        {
            TryGetSurfaceTile();
        }

        if (_currentTile != null && _currentTile.damagePerSecond != 0f)
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
        float fallHeight = Mathf.Max(0f, _maxAirY - transform.position.y);
        _maxAirY = -999999f;
        TryGetSurfaceTile();

        if (walkAudioSource != null)
        {
            if (_currentTile != null && _currentTile.landClip != null)
            {
                PlayClipAtPointWithPitch(_currentTile.landClip, transform.position, 1f, Random.Range(oneShotPitchRange.x, oneShotPitchRange.y));
            }
            else if (defaultLandClip != null)
            {
                PlayClipAtPointWithPitch(defaultLandClip, transform.position, 1f, Random.Range(oneShotPitchRange.x, oneShotPitchRange.y));
            }
        }

        if (_currentTile != null && _fallDamageSettings.TryGetValue(_currentTile.surfaceType, out var fallDamageSetting))
        {
            if (fallHeight > fallDamageSetting.minHeight)
                healthSystem.AddHealth(-fallHeight * fallDamageSetting.scale);
            return;
        }
        if (fallHeight > defaultMinFallHeight)
            healthSystem.AddHealth(-fallHeight * defaultFallDamageScale);
    }

    private void Jump()
    {
        _maxAirY = transform.position.y;
        if (walkAudioSource == null)
            return;

        if (_currentTile != null && _currentTile.jumpClip != null)
        {
            PlayClipAtPointWithPitch(_currentTile.jumpClip, transform.position, 0.35f, Random.Range(oneShotPitchRange.x, oneShotPitchRange.y));
        }
        else if (defaultJumpClip != null)
        {
            PlayClipAtPointWithPitch(defaultJumpClip, transform.position, 0.35f, Random.Range(oneShotPitchRange.x, oneShotPitchRange.y));
        }
    }

    private void Dash()
    {
        _maxAirY = transform.position.y;
    }

    private void PlayClipAtPointWithPitch(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        
        audioSource.Play();
        Destroy(tempGO, clip.length / Mathf.Abs(pitch));
    }
}