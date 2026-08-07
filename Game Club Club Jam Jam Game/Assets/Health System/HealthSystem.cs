using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private CharacterController2D characterController2D;
    [SerializeField] private bool shakeCameraOnDamage;
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;
    [SerializeField] private SliderIndicator healthIndicator;
    [SerializeField] private ScreenTint screenTint;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private float damageTintFadeSpeed = 0.25f;
    [SerializeField] private float healTintFadeSpeed = 0.35f;
    [SerializeField] private float armor;
    [SerializeField] private float maxArmor;
    [SerializeField] private SliderIndicator armorIndicator;
    [Tooltip("Each armor point reduces health damage by this factor.")]
    [SerializeField] private float armorDamageReductionFactor = 0.01f;
    [Tooltip("Determines how many armor points to remove with damage. 1=>100% of damage, 2=>50% of damage, 0.5=>200% of damage, etc")]
    public float armorDurability = 1f;
    [SerializeField] private FallDamageSetting[] fallDamageSettings;
    [SerializeField] private float defaultFallDamageScale = 0.25f;
    [SerializeField] private float defaultMinFallSpeed = 10f;
    [Tooltip("Multiplier with damage amount for shake magnitude.")]
    [SerializeField] private float damageShakeIntensity = 0.1f;
    [SerializeField] private float damageShakeSpeed = 1.5f;
    [SerializeField] private float damageShakeDuration = 0.1f;

    [System.Serializable]
    private struct FallDamageSetting
    {
        [Tooltip("Ground tag to use this fall damage scale with.")]
        public string tag;
        [Tooltip("Fall speed is multiplied by this to apply fall damage.")]
        public float scale;
        [Tooltip("Fall damage is only applied when fall speed is above this threshold.")]
        public float minSpeed;
    }
    private Dictionary<string, FallDamageSetting> _fallDamageSettings = new Dictionary<string, FallDamageSetting>();
    private CameraControl _mainCameraControl;

    public UnityEvent OnDeath;
    public UnityEvent OnArmorBreak;

    private void Start()
    {
        if (characterController2D != null)
            characterController2D.OnLanded += CheckFallDamage;
        foreach (FallDamageSetting fallDamageSetting in fallDamageSettings)
        {
            _fallDamageSettings.Add(fallDamageSetting.tag, fallDamageSetting);
        }
        _mainCameraControl = Camera.main.GetComponent<CameraControl>();
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }

    private void OnDestroy()
    {
        if (characterController2D != null)
            characterController2D.OnLanded -= CheckFallDamage;
    }

    private void Update()
    {
        
    }

    private void CheckFallDamage(Collider2D ground, float speed)
    {
        float minSpeed = defaultMinFallSpeed;
        float scale = defaultFallDamageScale;
        if (_fallDamageSettings.ContainsKey(ground.tag))
        {
            minSpeed = _fallDamageSettings[ground.tag].minSpeed;
            scale = _fallDamageSettings[ground.tag].scale;
        }

        if (speed > minSpeed)
        {
            AddHealth(-speed * scale);
        }
    }

    public float GetHealth()
    {
        return health;
    }

    public void SetHealth(float health)
    {
        if (health <= 0f && this.health > 0f)
            OnDeath?.Invoke();
        this.health = Mathf.Clamp(health, 0f, maxHealth);
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public void AddHealth(float amount)
    {
        if (amount < 0f)
        {
            float newAmount = amount * Mathf.Max(1f - armorDamageReductionFactor * armor, 0f);
            AddArmor(amount); // Apply damage to armor
            amount = newAmount;

            if (amount >= 0f)
                return;
            if (shakeCameraOnDamage && _mainCameraControl != null)
            {
                _mainCameraControl.StartCameraShake(-amount * damageShakeIntensity, damageShakeSpeed, damageShakeDuration);
            }
            if (screenTint != null)
                screenTint.StartTint(damageColor, damageTintFadeSpeed, 0f);
        }
        else if (screenTint != null)
        {
            screenTint.StartTint(healColor, healTintFadeSpeed, 0f);
        }
        float newHealth = health + amount;
        if (newHealth > maxHealth)
        {
            health = maxHealth;
        }
        else if (newHealth <= 0f)
        {
            health = 0f;
            OnDeath?.Invoke();
        }
        else
        {
            health = newHealth;
        }
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public void AddMaxHealth(float amount)
    {
        float newMaxHealth = maxHealth + amount;
        if (newMaxHealth <= 0f)
        {
            maxHealth = 0f;
        }
        else
        {
            maxHealth = newMaxHealth;
        }
        if (healthIndicator != null)
            healthIndicator.UpdateUI(this.health, maxHealth);
    }

    public float GetArmor()
    {
        return armor;
    }

    public void SetArmor(float armor)
    {
        this.armor = Mathf.Clamp(armor, 0f, maxArmor);
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }

    public void AddArmor(float amount)
    {
        if (amount < 0f)
        {
            amount *= 1f / armorDurability;
        }
        float newArmor = armor + amount;
        armor += amount;
        if (newArmor > maxArmor)
        {
            armor = maxArmor;
        }
        else if (newArmor <= 0f)
        {
            armor = 0f;
            OnArmorBreak?.Invoke();
        }
        else
        {
            armor = newArmor;
        }
        if (armorIndicator != null)
            armorIndicator.UpdateUI(this.armor, maxArmor);
    }
}
